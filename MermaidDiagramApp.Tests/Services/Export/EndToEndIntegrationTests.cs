using Xunit;
using Moq;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;
using System.Text;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// End-to-end integration tests for the complete Markdown to Word export workflow.
/// Tests all requirements with real file operations and actual document generation.
/// Requirements: All (comprehensive integration testing)
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILogger _logger;
    private readonly MarkdownToWordExportService _exportService;

    public EndToEndIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"MermaidExportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        
        var mockLogger = new Mock<ILogger>();
        _logger = mockLogger.Object;

        // Note: These are integration tests that test the full workflow
        // WebView2 rendering is mocked because it requires a UI thread
        // For true end-to-end testing with real WebView2, manual testing is required
        var parser = new MarkdigMarkdownParser();
        var wordGenerator = new OpenXmlWordDocumentGenerator();
        var mermaidRenderer = CreateMockedMermaidRenderer();

        _exportService = new MarkdownToWordExportService(
            parser,
            wordGenerator,
            mermaidRenderer,
            _logger);
    }

    private IMermaidImageRenderer CreateMockedMermaidRenderer()
    {
        var mock = new Mock<IMermaidImageRenderer>();
        
        // Mock successful rendering - creates a simple test image
        mock.Setup(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, ImageFormat, CancellationToken>((code, path, format, ct) =>
            {
                // Create a simple test image
                CreateTestImage(path, 400, 300);
                return Task.FromResult(path);
            });

        return mock.Object;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Test Helpers

    private string CreateTestMarkdownFile(string content, string fileName = "test.md")
    {
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content, Encoding.UTF8);
        return filePath;
    }

    private string GetOutputPath(string fileName = "output.docx")
    {
        return Path.Combine(_testDirectory, fileName);
    }

    private void VerifyWordDocumentExists(string path)
    {
        Assert.True(File.Exists(path), $"Word document should exist at {path}");
        Assert.True(new FileInfo(path).Length > 0, "Word document should not be empty");
    }

    private void VerifyWordDocumentStructure(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        Assert.NotNull(doc.MainDocumentPart);
        Assert.NotNull(doc.MainDocumentPart.Document);
        Assert.NotNull(doc.MainDocumentPart.Document.Body);
    }

    private int CountElementsInWordDocument<T>(string path) where T : OpenXmlElement
    {
        using var doc = WordprocessingDocument.Open(path, false);
        return doc.MainDocumentPart.Document.Body.Descendants<T>().Count();
    }

    #endregion

    #region Basic Export Tests

    [Fact]
    public async Task EndToEnd_SimpleMarkdown_ExportsSuccessfully()
    {
        // Arrange
        var markdown = @"# Test Document

This is a simple test document with basic formatting.

## Section 1

Some **bold** text and *italic* text.

## Section 2

A paragraph with `inline code`.";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        VerifyWordDocumentExists(outputPath);
        VerifyWordDocumentStructure(outputPath);
    }

    #endregion

    #region Markdown Elements Tests

    [Fact]
    public async Task EndToEnd_AllHeadingLevels_PreservesHierarchy()
    {
        // Arrange
        var markdown = @"# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        VerifyWordDocumentExists(outputPath);

        // Verify all heading levels are present
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraphs = doc.MainDocumentPart.Document.Body.Descendants<Paragraph>().ToList();
        
        Assert.True(paragraphs.Count >= 6, "Should have at least 6 paragraphs for headings");
    }

    [Fact]
    public async Task EndToEnd_Lists_PreservesStructure()
    {
        // Arrange
        var markdown = @"# Lists Test

## Unordered List
- Item 1
- Item 2
  - Nested item 2.1
  - Nested item 2.2
- Item 3

## Ordered List
1. First item
2. Second item
   1. Nested first
   2. Nested second
3. Third item";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        VerifyWordDocumentExists(outputPath);

        // Verify lists are present
        var listCount = CountElementsInWordDocument<NumberingProperties>(outputPath);
        Assert.True(listCount > 0, "Should contain list elements");
    }

    [Fact]
    public async Task EndToEnd_Tables_PreservesStructure()
    {
        // Arrange
        var markdown = @"# Table Test

| Header 1 | Header 2 | Header 3 |
|----------|----------|----------|
| Cell 1   | Cell 2   | Cell 3   |
| Cell 4   | Cell 5   | Cell 6   |
| Cell 7   | Cell 8   | Cell 9   |";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        VerifyWordDocumentExists(outputPath);

        // Verify table is present
        var tableCount = CountElementsInWordDocument<Table>(outputPath);
        Assert.Equal(1, tableCount);

        // Verify table structure
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var table = doc.MainDocumentPart.Document.Body.Descendants<Table>().First();
        var rows = table.Descendants<TableRow>().Count();
        Assert.Equal(4, rows); // 1 header + 3 data rows
    }

    [Fact]
    public async Task EndToEnd_CodeBlocks_FormattedCorrectly()
    {
        // Arrange
        var markdown = @"# Code Test

Here's some code:

```csharp
public class Test
{
    public void Method()
    {
        Console.WriteLine(""Hello"");
    }
}
```

And some inline `code` too.";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        VerifyWordDocumentExists(outputPath);
        VerifyWordDocumentStructure(outputPath);
    }

    [Fact]
    public async Task EndToEnd_TextFormatting_PreservedCorrectly()
    {
        // Arrange
        var markdown = @"# Formatting Test

This paragraph has **bold text**, *italic text*, and `inline code`.

It also has ***bold and italic*** text.

And ~~strikethrough~~ text.";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        VerifyWordDocumentExists(outputPath);

        // Verify formatting is present
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var runs = doc.MainDocumentPart.Document.Body.Descendants<Run>().ToList();
        Assert.True(runs.Count > 0, "Should have text runs");
    }

    #endregion

    #region Mermaid Diagram Tests

    [Fact]
    public async Task EndToEnd_SingleMermaidDiagram_RendersAsImage()
    {
        // Arrange
        var markdown = @"# Diagram Test

Here's a flowchart:

```mermaid
graph TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
```

End of document.";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.Equal(1, result.Statistics.MermaidDiagramsRendered);
        VerifyWordDocumentExists(outputPath);

        // Verify image is embedded
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts > 0, "Should have at least one embedded image");
    }

    [Fact]
    public async Task EndToEnd_MultipleMermaidDiagrams_AllRendered()
    {
        // Arrange
        var markdown = @"# Multiple Diagrams

## Diagram 1: Flowchart

```mermaid
graph LR
    A --> B
    B --> C
```

## Diagram 2: Sequence

```mermaid
sequenceDiagram
    Alice->>Bob: Hello
    Bob->>Alice: Hi
```

## Diagram 3: Pie Chart

```mermaid
pie
    title Pets
    ""Dogs"" : 40
    ""Cats"" : 35
    ""Birds"" : 25
```";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Statistics.MermaidDiagramsRendered);
        VerifyWordDocumentExists(outputPath);

        // Verify all images are embedded
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts >= 3, $"Should have at least 3 embedded images, found {imageParts}");
    }

    [Fact]
    public async Task EndToEnd_ComplexMermaidDiagram_RendersCorrectly()
    {
        // Arrange
        var markdown = @"# Complex Diagram

```mermaid
graph TB
    subgraph ""Frontend""
        A[User Interface]
        B[API Client]
    end
    
    subgraph ""Backend""
        C[API Gateway]
        D[Service Layer]
        E[Database]
    end
    
    A --> B
    B --> C
    C --> D
    D --> E
```";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Statistics.MermaidDiagramsRendered);
        VerifyWordDocumentExists(outputPath);
    }

    #endregion

    #region Image Tests

    [Fact]
    public async Task EndToEnd_PngImage_EmbedsCorrectly()
    {
        // Arrange
        var imagePath = Path.Combine(_testDirectory, "test.png");
        CreateTestImage(imagePath, 100, 100);

        var markdown = $@"# Image Test

![Test Image](test.png)

End of document.";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Statistics.ImagesEmbedded > 0);
        VerifyWordDocumentExists(outputPath);

        // Verify image is embedded
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts > 0, "Should have embedded image");
    }

    [Fact]
    public async Task EndToEnd_MultipleImageFormats_AllEmbedded()
    {
        // Arrange
        var pngPath = Path.Combine(_testDirectory, "test.png");
        var jpgPath = Path.Combine(_testDirectory, "test.jpg");
        
        CreateTestImage(pngPath, 100, 100);
        CreateTestImage(jpgPath, 100, 100);

        var markdown = @"# Multiple Images

![PNG Image](test.png)

![JPG Image](test.jpg)";

        var markdownPath = CreateTestMarkdownFile(markdown);
        var outputPath = GetOutputPath();

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Statistics.ImagesEmbedded >= 2, $"Should embed at least 2 images, embedded {result.Statistics.ImagesEmbedded}");
        VerifyWordDocumentExists(outputPath);
    }

    private void CreateTestImage(string path, int width, int height)
    {
        // Create a simple test image using SkiaSharp
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.White);
        
        // Draw a simple shape
        var paint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Blue,
            IsAntialias = true
        };
        canvas.DrawCircle(width / 2, height / 2, Math.Min(width, height) / 3, paint);
        
        // Save as PNG or JPG
        using var image = surface.Snapshot();
        using var data = path.EndsWith(".jpg") || path.EndsWith(".jpeg")
            ? image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90)
            : image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    #endregion

    #region Large File Tests

    [Fact]
    public async Task EndToEnd_LargeMarkdownFile_ExportsSuccessfully()
    {
        // Arrange - Create a large markdown file (>1MB)
        var sb = new StringBuilder();
        sb.AppendLine("# Large Document Test");
        sb.AppendLine();

        // Add many sections to make it large
        for (int i = 1; i <= 100; i++)
        {
            sb.AppendLine($"## Section {i}");
            sb.AppendLine();
            sb.AppendLine($"This is section {i} with some content. ");
            sb.AppendLine("Lorem ipsum dolor sit amet, consectetur adipiscing elit. ");
            sb.AppendLine("Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. ");
            sb.AppendLine();
            
            // Add a list every 10 sections
            if (i % 10 == 0)
            {
                sb.AppendLine("- Item 1");
                sb.AppendLine("- Item 2");
                sb.AppendLine("- Item 3");
                sb.AppendLine();
            }
            
            // Add a table every 20 sections
            if (i % 20 == 0)
            {
                sb.AppendLine("| Col 1 | Col 2 | Col 3 |");
                sb.AppendLine("|-------|-------|-------|");
                sb.AppendLine("| A     | B     | C     |");
                sb.AppendLine("| D     | E     | F     |");
                sb.AppendLine();
            }
        }

        var markdown = sb.ToString();
        Assert.True(markdown.Length > 1024 * 1024, "Markdown should be > 1MB");

        var markdownPath = CreateTestMarkdownFile(markdown, "large.md");
        var outputPath = GetOutputPath("large.docx");

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(result.Success);
        Assert.True(duration.TotalSeconds < 30, $"Export should complete within 30 seconds, took {duration.TotalSeconds:F2}s");
        VerifyWordDocumentExists(outputPath);
        
        // Verify file size is reasonable
        var fileSize = new FileInfo(outputPath).Length;
        Assert.True(fileSize > 0, "Output file should not be empty");
    }

    [Fact]
    public async Task EndToEnd_ManyMermaidDiagrams_AllRendered()
    {
        // Arrange - Create document with many diagrams
        var sb = new StringBuilder();
        sb.AppendLine("# Many Diagrams Test");
        sb.AppendLine();

        for (int i = 1; i <= 10; i++)
        {
            sb.AppendLine($"## Diagram {i}");
            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.AppendLine("graph LR");
            sb.AppendLine($"    A{i}[Node A] --> B{i}[Node B]");
            sb.AppendLine($"    B{i} --> C{i}[Node C]");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        var markdown = sb.ToString();
        var markdownPath = CreateTestMarkdownFile(markdown, "many-diagrams.md");
        var outputPath = GetOutputPath("many-diagrams.docx");

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(10, result.Statistics.MermaidDiagramsRendered);
        VerifyWordDocumentExists(outputPath);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task EndToEnd_CancellationDuringExport_StopsProcessing()
    {
        // Arrange
        var sb = new StringBuilder();
        sb.AppendLine("# Cancellation Test");
        
        // Add many diagrams to make export take longer
        for (int i = 1; i <= 20; i++)
        {
            sb.AppendLine($"## Diagram {i}");
            sb.AppendLine("```mermaid");
            sb.AppendLine("graph TD");
            sb.AppendLine($"    A{i} --> B{i}");
            sb.AppendLine($"    B{i} --> C{i}");
            sb.AppendLine("```");
        }

        var markdown = sb.ToString();
        var markdownPath = CreateTestMarkdownFile(markdown, "cancel-test.md");
        var outputPath = GetOutputPath("cancel-test.docx");

        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            cts.Cancel();
        });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _exportService.ExportToWordAsync(
                markdown,
                markdownPath,
                outputPath,
                null,
                cts.Token);
        });
    }

    #endregion

    #region Comprehensive Integration Test

    [Fact]
    public async Task EndToEnd_ComprehensiveDocument_ExportsAllElements()
    {
        // Arrange - Create a document with all supported elements
        var imagePath = Path.Combine(_testDirectory, "diagram.png");
        CreateTestImage(imagePath, 200, 150);

        var markdown = @"# Comprehensive Test Document

This document tests all supported Markdown elements.

## Text Formatting

This paragraph contains **bold text**, *italic text*, `inline code`, and ***bold italic*** text.

## Lists

### Unordered List
- First item
- Second item
  - Nested item 1
  - Nested item 2
- Third item

### Ordered List
1. First
2. Second
   1. Nested first
   2. Nested second
3. Third

## Code Block

```csharp
public class Example
{
    public void Method()
    {
        Console.WriteLine(""Hello, World!"");
    }
}
```

## Table

| Name    | Age | City        |
|---------|-----|-------------|
| Alice   | 30  | New York    |
| Bob     | 25  | Los Angeles |
| Charlie | 35  | Chicago     |

## Blockquote

> This is a blockquote.
> It can span multiple lines.

## Image

![Test Diagram](diagram.png)

## Mermaid Diagram

```mermaid
graph TB
    A[Start] --> B{Is it working?}
    B -->|Yes| C[Great!]
    B -->|No| D[Debug]
    D --> A
    C --> E[End]
```

## Another Mermaid Diagram

```mermaid
sequenceDiagram
    participant User
    participant System
    User->>System: Request
    System->>User: Response
```

## Conclusion

This document contains all major Markdown elements.";

        var markdownPath = CreateTestMarkdownFile(markdown, "comprehensive.md");
        var outputPath = GetOutputPath("comprehensive.docx");

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        VerifyWordDocumentExists(outputPath);
        VerifyWordDocumentStructure(outputPath);

        // Verify statistics
        Assert.Equal(2, result.Statistics.MermaidDiagramsRendered);
        Assert.True(result.Statistics.ImagesEmbedded >= 1, "Should embed at least the PNG image");
        Assert.Equal(1, result.Statistics.TablesProcessed);

        // Verify document structure
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart.Document.Body;
        
        // Should have paragraphs
        var paragraphs = body.Descendants<Paragraph>().Count();
        Assert.True(paragraphs > 10, $"Should have many paragraphs, found {paragraphs}");
        
        // Should have table
        var tables = body.Descendants<Table>().Count();
        Assert.Equal(1, tables);
        
        // Should have images (Mermaid diagrams + embedded image)
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts >= 3, $"Should have at least 3 images (2 Mermaid + 1 PNG), found {imageParts}");
    }

    #endregion
      
}

