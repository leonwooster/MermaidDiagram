using Markdig.Syntax;
using MermaidDiagramApp.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Service that orchestrates the export of Markdown files to Word documents.
/// </summary>
public class MarkdownToWordExportService
{
    private readonly IMarkdownParser _markdownParser;
    private readonly IWordDocumentGenerator _wordGenerator;
    private readonly IMermaidImageRenderer _mermaidRenderer;
    private readonly ILogger _logger;
    private readonly List<string> _temporaryFiles = new();

    public MarkdownToWordExportService(
        IMarkdownParser markdownParser,
        IWordDocumentGenerator wordGenerator,
        IMermaidImageRenderer mermaidRenderer,
        ILogger logger)
    {
        _markdownParser = markdownParser ?? throw new ArgumentNullException(nameof(markdownParser));
        _wordGenerator = wordGenerator ?? throw new ArgumentNullException(nameof(wordGenerator));
        _mermaidRenderer = mermaidRenderer ?? throw new ArgumentNullException(nameof(mermaidRenderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports Markdown content to a Word document.
    /// </summary>
    /// <param name="markdownContent">The Markdown content to export.</param>
    /// <param name="markdownFilePath">The path to the source Markdown file (for resolving relative paths).</param>
    /// <param name="outputPath">The path where the Word document will be created.</param>
    /// <param name="progress">Progress reporter for tracking export progress.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling the operation.</param>
    /// <returns>An ExportResult containing the outcome of the operation.</returns>
    public async Task<ExportResult> ExportToWordAsync(
        string markdownContent,
        string markdownFilePath,
        string outputPath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ExportResult();

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                throw new ArgumentException("Markdown content cannot be empty.", nameof(markdownContent));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));
            }

            // Validate output directory exists and is writable
            try
            {
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                {
                    _logger.LogWarning($"Output directory does not exist, creating: {outputDirectory}");
                    Directory.CreateDirectory(outputDirectory);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError("Access denied to output directory", ex);
                throw new InvalidOperationException($"Access denied: Cannot write to directory '{Path.GetDirectoryName(outputPath)}'. Please check permissions.", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError("I/O error accessing output directory", ex);
                throw new InvalidOperationException($"I/O error: Cannot access directory '{Path.GetDirectoryName(outputPath)}'. {ex.Message}", ex);
            }

            _logger.LogInformation($"Starting export to {outputPath}");

            // Stage 1: Parse Markdown
            ReportProgress(progress, 10, "Parsing Markdown...", ExportStage.Parsing);
            cancellationToken.ThrowIfCancellationRequested();

            MarkdownDocument document;
            try
            {
                document = _markdownParser.Parse(markdownContent);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to parse Markdown content", ex);
                throw new InvalidOperationException("Failed to parse Markdown content. The file may contain invalid syntax.", ex);
            }

            var mermaidBlocks = _markdownParser.ExtractMermaidBlocks(document).ToList();
            var imageReferences = _markdownParser.ExtractImageReferences(document).ToList();

            _logger.LogInformation(
                $"Parsed document: {mermaidBlocks.Count} Mermaid blocks, {imageReferences.Count} images");

            // Stage 2: Render Mermaid diagrams
            ReportProgress(progress, 30, "Rendering Mermaid diagrams...", ExportStage.RenderingDiagrams);
            cancellationToken.ThrowIfCancellationRequested();

            await RenderMermaidDiagramsAsync(mermaidBlocks, cancellationToken);
            result.Statistics.MermaidDiagramsRendered = mermaidBlocks.Count(mb => mb.RenderedImagePath != null);
            
            // Log mermaid block details for debugging
            _logger.LogInformation($"Mermaid blocks after rendering: {mermaidBlocks.Count}");
            foreach (var mb in mermaidBlocks)
            {
                _logger.LogInformation($"  Block at line {mb.LineNumber}: RenderedImagePath={mb.RenderedImagePath ?? "null"}, ErrorMessage={mb.ErrorMessage ?? "null"}");
            }

            // Stage 3: Resolve image paths
            ReportProgress(progress, 50, "Resolving image paths...", ExportStage.ResolvingImages);
            cancellationToken.ThrowIfCancellationRequested();

            ResolveImagePaths(imageReferences, markdownFilePath);
            result.Statistics.ImagesEmbedded = imageReferences.Count(ir => ir.ResolvedPath != null);

            // Stage 4: Generate Word document
            ReportProgress(progress, 70, "Generating Word document...", ExportStage.GeneratingDocument);
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("=== STAGE 4: About to enter Word document generation try block ===");
            
            try
            {
                _logger.LogInformation($"About to generate Word document with {mermaidBlocks.Count} Mermaid blocks");
                GenerateWordDocument(document, mermaidBlocks, imageReferences, outputPath);
                _logger.LogInformation("Word document generation completed successfully");
                
                // IMPORTANT: Clean up temporary files AFTER document generation completes
                // This ensures all images are embedded before we delete the temp files
                _logger.LogInformation($"About to clean up {_temporaryFiles.Count} temporary files");
                CleanupTemporaryFiles();
                _logger.LogInformation("Temporary files cleanup completed");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError("Access denied when creating Word document", ex);
                throw new InvalidOperationException($"Access denied: Cannot write to file '{outputPath}'. The file may be open in another program or you may not have write permissions.", ex);
            }
            catch (IOException ex) when (ex.Message.Contains("disk") || ex.Message.Contains("space"))
            {
                _logger.LogError("Insufficient disk space", ex);
                throw new InvalidOperationException("Insufficient disk space to create the Word document. Please free up disk space and try again.", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError("I/O error creating Word document", ex);
                throw new InvalidOperationException($"I/O error: Cannot create file '{outputPath}'. {ex.Message}", ex);
            }

            // Calculate output file size
            try
            {
                if (File.Exists(outputPath))
                {
                    result.Statistics.OutputFileSize = new FileInfo(outputPath).Length;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to get output file size", ex);
                // Non-critical error, continue
            }

            ReportProgress(progress, 100, "Export complete", ExportStage.Complete);

            result.Success = true;
            result.OutputPath = outputPath;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                $"Export completed successfully in {result.Duration.TotalSeconds:F2} seconds");

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Export cancelled by user");
            result.Success = false;
            result.ErrorMessage = "Export was cancelled.";
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("Invalid argument", ex);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Operation failed", ex);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error during export", ex);
            result.Success = false;
            result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }
        finally
        {
            // Clean up temporary files only if they weren't already cleaned up
            // (they are cleaned up after successful document generation)
            if (_temporaryFiles.Count > 0)
            {
                CleanupTemporaryFiles();
            }
        }
    }

    private async Task RenderMermaidDiagramsAsync(
        List<MermaidBlock> mermaidBlocks,
        CancellationToken cancellationToken)
    {
        foreach (var block in mermaidBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"mermaid_{Guid.NewGuid()}.png");
                _temporaryFiles.Add(tempPath);

                var renderedPath = await _mermaidRenderer.RenderToImageAsync(
                    block.Code,
                    tempPath,
                    ImageFormat.PNG,
                    cancellationToken);

                block.RenderedImagePath = renderedPath;
                _logger.LogDebug($"Rendered Mermaid diagram to {renderedPath}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("syntax") || ex.Message.Contains("render"))
            {
                // Mermaid syntax error - store error message for document generation
                _logger.LogWarning($"Mermaid syntax error at line {block.LineNumber}: {ex.Message}");
                block.ErrorMessage = $"Mermaid syntax error: {ex.Message}";
                // Continue with other diagrams
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to render Mermaid diagram at line {block.LineNumber}", ex);
                block.ErrorMessage = $"Failed to render diagram: {ex.Message}";
                // Continue with other diagrams - error will be handled in document generation
            }
        }
    }

    private void ResolveImagePaths(List<ImageReference> imageReferences, string markdownFilePath)
    {
        var markdownDirectory = Path.GetDirectoryName(markdownFilePath) ?? string.Empty;

        foreach (var imageRef in imageReferences)
        {
            try
            {
                // Skip URLs
                if (imageRef.OriginalPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    imageRef.OriginalPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Log(LogLevel.Debug, $"Skipping URL image: {imageRef.OriginalPath}");
                    continue;
                }

                // Resolve relative paths
                string resolvedPath;
                if (Path.IsPathRooted(imageRef.OriginalPath))
                {
                    resolvedPath = imageRef.OriginalPath;
                }
                else
                {
                    resolvedPath = Path.Combine(markdownDirectory, imageRef.OriginalPath);
                    resolvedPath = Path.GetFullPath(resolvedPath);
                }

                // Validate file exists
                if (File.Exists(resolvedPath))
                {
                    imageRef.ResolvedPath = resolvedPath;
                    _logger.Log(LogLevel.Debug, $"Resolved image path: {resolvedPath}");
                }
                else
                {
                    _logger.Log(LogLevel.Warning, $"Image file not found: {resolvedPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, $"Failed to resolve image path: {imageRef.OriginalPath}", ex);
            }
        }
    }

    private void GenerateWordDocument(
        MarkdownDocument document,
        List<MermaidBlock> mermaidBlocks,
        List<ImageReference> imageReferences,
        string outputPath)
    {
        _wordGenerator.CreateDocument(outputPath);

        try
        {
            // Walk through the Markdown AST and generate Word content
            ProcessMarkdownBlocks(document, mermaidBlocks, imageReferences);

            _wordGenerator.Save();
        }
        finally
        {
            // Always dispose to release file locks
            _wordGenerator.Dispose();
        }
    }

    private void ProcessMarkdownBlocks(
        MarkdownDocument document,
        List<MermaidBlock> mermaidBlocks,
        List<ImageReference> imageReferences)
    {
        foreach (var block in document)
        {
            ProcessBlock(block, mermaidBlocks, imageReferences);
        }
    }

    private void ProcessBlock(
        Block block,
        List<MermaidBlock> mermaidBlocks,
        List<ImageReference> imageReferences)
    {
        switch (block)
        {
            case HeadingBlock heading:
                ProcessHeading(heading);
                break;

            case ParagraphBlock paragraph:
                ProcessParagraph(paragraph, imageReferences);
                break;

            case FencedCodeBlock codeBlock:
                ProcessCodeBlock(codeBlock, mermaidBlocks);
                break;

            case ListBlock list:
                ProcessList(list);
                break;

            case Markdig.Extensions.Tables.Table table:
                ProcessTable(table);
                break;

            case QuoteBlock quote:
                ProcessQuote(quote);
                break;

            case ContainerBlock container:
                // Process nested blocks
                foreach (var child in container)
                {
                    ProcessBlock(child, mermaidBlocks, imageReferences);
                }
                break;
        }
    }

    private void ProcessHeading(HeadingBlock heading)
    {
        var text = ExtractText(heading);
        _wordGenerator.AddHeading(text, heading.Level);
    }

    private void ProcessParagraph(ParagraphBlock paragraph, List<ImageReference> imageReferences)
    {
        // Check if paragraph contains images
        var hasImages = false;
        foreach (var inline in paragraph.Inline)
        {
            if (inline is Markdig.Syntax.Inlines.LinkInline link && link.IsImage)
            {
                hasImages = true;
                ProcessImage(link, imageReferences);
            }
        }

        // If no images, process as text
        if (!hasImages)
        {
            var text = ExtractText(paragraph);
            if (!string.IsNullOrWhiteSpace(text))
            {
                _wordGenerator.AddParagraph(text, new ParagraphStyle());
            }
        }
    }

    private void ProcessImage(Markdig.Syntax.Inlines.LinkInline link, List<ImageReference> imageReferences)
    {
        var imageRef = imageReferences.FirstOrDefault(ir => ir.OriginalPath == link.Url);

        if (imageRef?.ResolvedPath != null && File.Exists(imageRef.ResolvedPath))
        {
            try
            {
                _wordGenerator.AddImage(imageRef.ResolvedPath, new ImageOptions());
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning($"Image file not found: {imageRef.ResolvedPath}", ex);
                _wordGenerator.AddParagraph($"[Image not found: {imageRef.OriginalPath}]", new ParagraphStyle());
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Access denied to image file: {imageRef.ResolvedPath}", ex);
                _wordGenerator.AddParagraph($"[Image access denied: {imageRef.OriginalPath}]", new ParagraphStyle());
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to embed image: {imageRef.ResolvedPath}", ex);
                _wordGenerator.AddParagraph($"[Image error: {imageRef.OriginalPath} - {ex.Message}]", new ParagraphStyle());
            }
        }
        else
        {
            var altText = !string.IsNullOrEmpty(imageRef?.AltText) ? $" (alt: {imageRef.AltText})" : "";
            _wordGenerator.AddParagraph($"[Image not found: {link.Url}{altText}]", new ParagraphStyle());
            _logger.LogWarning($"Image file not found or could not be resolved: {link.Url}");
        }
    }

    private void ProcessCodeBlock(FencedCodeBlock codeBlock, List<MermaidBlock> mermaidBlocks)
    {
        var language = codeBlock.Info ?? string.Empty;

        if (language.Equals("mermaid", StringComparison.OrdinalIgnoreCase))
        {
            var codeBlockLineNumber = codeBlock.Line + 1; // Convert to 1-based
            _logger.LogInformation($"Processing Mermaid code block at line {codeBlockLineNumber}");
            
            // Find the corresponding rendered Mermaid block by matching line numbers
            // Both are now 1-based line numbers
            var mermaidBlock = mermaidBlocks.FirstOrDefault(mb => mb.LineNumber == codeBlockLineNumber);
            
            _logger.LogInformation($"Found mermaid block: {mermaidBlock != null}, RenderedImagePath: {mermaidBlock?.RenderedImagePath ?? "null"}");
            _logger.LogInformation($"Available mermaid blocks: {string.Join(", ", mermaidBlocks.Select(mb => $"Line {mb.LineNumber}"))}");
            
            if (mermaidBlock?.RenderedImagePath != null)
            {
                var fileExists = File.Exists(mermaidBlock.RenderedImagePath);
                _logger.LogInformation($"File exists check: {fileExists} for path: {mermaidBlock.RenderedImagePath}");
            }

            if (mermaidBlock?.RenderedImagePath != null && File.Exists(mermaidBlock.RenderedImagePath))
            {
                try
                {
                    _logger.LogInformation($"Attempting to add image: {mermaidBlock.RenderedImagePath}");
                    _wordGenerator.AddImage(mermaidBlock.RenderedImagePath, new ImageOptions());
                    _logger.LogInformation("Image added successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to embed Mermaid diagram: {ex.Message}", ex);
                    _wordGenerator.AddParagraph($"[Mermaid diagram error: {ex.Message}]", new ParagraphStyle());
                    // Include the original code for reference
                    _wordGenerator.AddCodeBlock(mermaidBlock.Code, "mermaid");
                }
            }
            else if (mermaidBlock?.ErrorMessage != null)
            {
                _logger.LogWarning($"Mermaid block has error message: {mermaidBlock.ErrorMessage}");
                // Display the specific error message
                _wordGenerator.AddParagraph($"[{mermaidBlock.ErrorMessage}]", new ParagraphStyle());
                // Include the original code for reference
                _wordGenerator.AddCodeBlock(mermaidBlock.Code, "mermaid");
            }
            else
            {
                _logger.LogWarning($"Mermaid diagram rendering failed - no rendered path or file doesn't exist");
                _wordGenerator.AddParagraph("[Mermaid diagram rendering failed]", new ParagraphStyle());
                // Try to include the original code if available
                if (mermaidBlock != null)
                {
                    _wordGenerator.AddCodeBlock(mermaidBlock.Code, "mermaid");
                }
            }
        }
        else
        {
            // Regular code block
            var code = ExtractCodeBlockText(codeBlock);
            _wordGenerator.AddCodeBlock(code, language);
        }
    }

    private void ProcessList(ListBlock list)
    {
        var listData = new ListData();
        ExtractListItems(list, listData.Items, 0);
        _wordGenerator.AddList(listData, list.IsOrdered);
    }

    private void ExtractListItems(ListBlock list, List<ListItem> items, int level)
    {
        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                var text = ExtractText(listItem);
                items.Add(new ListItem { Text = text, Level = level });

                // Check for nested lists
                foreach (var child in listItem)
                {
                    if (child is ListBlock nestedList)
                    {
                        ExtractListItems(nestedList, items, level + 1);
                    }
                }
            }
        }
    }

    private void ProcessTable(Markdig.Extensions.Tables.Table table)
    {
        var tableData = new TableData();

        foreach (var row in table)
        {
            if (row is Markdig.Extensions.Tables.TableRow tableRow)
            {
                var cells = new List<string>();
                foreach (var cell in tableRow)
                {
                    if (cell is Markdig.Extensions.Tables.TableCell tableCell)
                    {
                        cells.Add(ExtractText(tableCell));
                    }
                }

                if (tableRow.IsHeader)
                {
                    tableData.Headers = cells;
                }
                else
                {
                    tableData.Rows.Add(cells);
                }
            }
        }

        _wordGenerator.AddTable(tableData);
    }

    private void ProcessQuote(QuoteBlock quote)
    {
        var text = ExtractText(quote);
        _wordGenerator.AddBlockquote(text);
    }

    private string ExtractText(Block block)
    {
        if (block is LeafBlock leafBlock && leafBlock.Inline != null)
        {
            return ExtractInlineText(leafBlock.Inline);
        }

        if (block is ContainerBlock container)
        {
            var texts = new List<string>();
            foreach (var child in container)
            {
                texts.Add(ExtractText(child));
            }
            return string.Join(" ", texts.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        return string.Empty;
    }

    private string ExtractInlineText(Markdig.Syntax.Inlines.ContainerInline inline)
    {
        var texts = new List<string>();
        foreach (var child in inline)
        {
            if (child is Markdig.Syntax.Inlines.LiteralInline literal)
            {
                texts.Add(literal.Content.ToString());
            }
            else if (child is Markdig.Syntax.Inlines.ContainerInline container)
            {
                texts.Add(ExtractInlineText(container));
            }
        }
        return string.Join("", texts);
    }

    private string ExtractCodeBlockText(FencedCodeBlock codeBlock)
    {
        return codeBlock.Lines.ToString();
    }

    private void ReportProgress(
        IProgress<ExportProgress>? progress,
        int percentComplete,
        string operation,
        ExportStage stage)
    {
        progress?.Report(new ExportProgress
        {
            PercentComplete = percentComplete,
            CurrentOperation = operation,
            Stage = stage
        });
    }

    private void CleanupTemporaryFiles()
    {
        foreach (var tempFile in _temporaryFiles)
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                    _logger.Log(LogLevel.Debug, $"Deleted temporary file: {tempFile}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, $"Failed to delete temporary file: {tempFile}", ex);
            }
        }

        _temporaryFiles.Clear();
    }
}
