using Xunit;
using Moq;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Performance tests for Markdown to Word export functionality.
/// Tests export performance with various file sizes, memory usage, and optimization.
/// Requirements: 7.1 (Performance requirements)
/// </summary>
public class PerformanceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILogger _logger;
    private readonly MarkdownToWordExportService _exportService;

    public PerformanceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"MermaidPerfTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        
        var mockLogger = new Mock<ILogger>();
        _logger = mockLogger.Object;

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
        
        // Mock rendering with realistic delay to simulate actual rendering
        mock.Setup(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, ImageFormat, CancellationToken>(async (code, path, format, ct) =>
            {
                // Simulate rendering time (50-100ms per diagram)
                await Task.Delay(Random.Shared.Next(50, 100), ct);
                CreateTestImage(path, 800, 600);
                return path;
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

    private void CreateTestImage(string path, int width, int height)
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.White);
        
        var paint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Blue,
            IsAntialias = true
        };
        canvas.DrawCircle(width / 2, height / 2, Math.Min(width, height) / 3, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    private string GenerateMarkdownWithSize(int targetSizeKB, bool includeDiagrams = false, int diagramCount = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Performance Test Document");
        sb.AppendLine();

        int currentSize = 0;
        int sectionCount = 0;

        while (currentSize < targetSizeKB * 1024)
        {
            sectionCount++;
            sb.AppendLine($"## Section {sectionCount}");
            sb.AppendLine();
            sb.AppendLine("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. ");
            sb.AppendLine("Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. ");
            sb.AppendLine("Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. ");
            sb.AppendLine();

            // Add variety
            if (sectionCount % 3 == 0)
            {
                sb.AppendLine("**Bold text** and *italic text* with `inline code`.");
                sb.AppendLine();
            }

            if (sectionCount % 5 == 0)
            {
                sb.AppendLine("- List item 1");
                sb.AppendLine("- List item 2");
                sb.AppendLine("- List item 3");
                sb.AppendLine();
            }

            if (sectionCount % 10 == 0)
            {
                sb.AppendLine("| Column 1 | Column 2 | Column 3 |");
                sb.AppendLine("|----------|----------|----------|");
                sb.AppendLine("| Data 1   | Data 2   | Data 3   |");
                sb.AppendLine("| Data 4   | Data 5   | Data 6   |");
                sb.AppendLine();
            }

            currentSize = Encoding.UTF8.GetByteCount(sb.ToString());
        }

        // Add diagrams if requested
        if (includeDiagrams && diagramCount > 0)
        {
            for (int i = 1; i <= diagramCount; i++)
            {
                sb.AppendLine($"## Diagram {i}");
                sb.AppendLine();
                sb.AppendLine("```mermaid");
                sb.AppendLine("graph TD");
                sb.AppendLine($"    A{i}[Start {i}] --> B{i}[Process {i}]");
                sb.AppendLine($"    B{i} --> C{i}[Decision {i}]");
                sb.AppendLine($"    C{i} -->|Yes| D{i}[Action {i}]");
                sb.AppendLine($"    C{i} -->|No| E{i}[Alternative {i}]");
                sb.AppendLine($"    D{i} --> F{i}[End {i}]");
                sb.AppendLine($"    E{i} --> F{i}");
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    #endregion

    #region File Size Performance Tests

    [Fact]
    public async Task Performance_SmallFile_ExportsQuickly()
    {
        // Arrange - Small file (<100KB)
        var markdown = GenerateMarkdownWithSize(50);
        var markdownPath = CreateTestMarkdownFile(markdown, "small.md");
        var outputPath = GetOutputPath("small.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.True(stopwatch.Elapsed.TotalSeconds < 2, 
            $"Small file export should complete in <2 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        Assert.True(File.Exists(outputPath), "Output file should exist");
    }

    [Fact]
    public async Task Performance_MediumFile_ExportsWithinTimeLimit()
    {
        // Arrange - Medium file (100KB-1MB)
        var markdown = GenerateMarkdownWithSize(500);
        var markdownPath = CreateTestMarkdownFile(markdown, "medium.md");
        var outputPath = GetOutputPath("medium.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.True(stopwatch.Elapsed.TotalSeconds < 10, 
            $"Medium file export should complete in <10 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        Assert.True(File.Exists(outputPath), "Output file should exist");
    }

    [Fact]
    public async Task Performance_LargeFile_ExportsWithinTimeLimit()
    {
        // Arrange - Large file (>1MB)
        var markdown = GenerateMarkdownWithSize(1500);
        var markdownPath = CreateTestMarkdownFile(markdown, "large.md");
        var outputPath = GetOutputPath("large.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert - Requirement 7.1: Files >1MB should complete within 30 seconds
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.True(stopwatch.Elapsed.TotalSeconds < 30, 
            $"Large file (>1MB) export should complete in <30 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        Assert.True(File.Exists(outputPath), "Output file should exist");
        
        var fileInfo = new FileInfo(markdownPath);
        Assert.True(fileInfo.Length > 1024 * 1024, 
            $"Test file should be >1MB, actual size: {fileInfo.Length / 1024}KB");
    }

    #endregion

    #region Mermaid Diagram Performance Tests

    [Fact]
    public async Task Performance_10MermaidDiagrams_ExportsEfficiently()
    {
        // Arrange
        var markdown = GenerateMarkdownWithSize(100, includeDiagrams: true, diagramCount: 10);
        var markdownPath = CreateTestMarkdownFile(markdown, "10-diagrams.md");
        var outputPath = GetOutputPath("10-diagrams.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.Equal(10, result.Statistics.MermaidDiagramsRendered);
        
        // Should complete reasonably quickly (allowing ~100ms per diagram + overhead)
        Assert.True(stopwatch.Elapsed.TotalSeconds < 5, 
            $"10 diagrams should export in <5 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    [Fact]
    public async Task Performance_50MermaidDiagrams_ExportsWithinTimeLimit()
    {
        // Arrange - Test with 50+ diagrams as per requirements
        var markdown = GenerateMarkdownWithSize(200, includeDiagrams: true, diagramCount: 50);
        var markdownPath = CreateTestMarkdownFile(markdown, "50-diagrams.md");
        var outputPath = GetOutputPath("50-diagrams.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.Equal(50, result.Statistics.MermaidDiagramsRendered);
        
        // Should complete within reasonable time (allowing ~100ms per diagram + overhead)
        Assert.True(stopwatch.Elapsed.TotalSeconds < 20, 
            $"50 diagrams should export in <20 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        // Verify all diagrams are embedded
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts >= 50, $"Should have at least 50 embedded images, found {imageParts}");
    }

    [Fact]
    public async Task Performance_100MermaidDiagrams_HandlesLargeScale()
    {
        // Arrange - Stress test with 100 diagrams
        var markdown = GenerateMarkdownWithSize(300, includeDiagrams: true, diagramCount: 100);
        var markdownPath = CreateTestMarkdownFile(markdown, "100-diagrams.md");
        var outputPath = GetOutputPath("100-diagrams.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.Equal(100, result.Statistics.MermaidDiagramsRendered);
        
        // Should complete within reasonable time for large scale
        Assert.True(stopwatch.Elapsed.TotalSeconds < 40, 
            $"100 diagrams should export in <40 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        // Verify document is valid
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts >= 100, $"Should have at least 100 embedded images, found {imageParts}");
    }

    #endregion

    #region Image Performance Tests

    [Fact]
    public async Task Performance_100Images_ExportsEfficiently()
    {
        // Arrange - Create 100 test images
        var sb = new StringBuilder();
        sb.AppendLine("# 100 Images Test");
        sb.AppendLine();

        for (int i = 1; i <= 100; i++)
        {
            var imagePath = Path.Combine(_testDirectory, $"image{i}.png");
            CreateTestImage(imagePath, 200, 150);
            
            sb.AppendLine($"## Image {i}");
            sb.AppendLine($"![Image {i}](image{i}.png)");
            sb.AppendLine();
        }

        var markdown = sb.ToString();
        var markdownPath = CreateTestMarkdownFile(markdown, "100-images.md");
        var outputPath = GetOutputPath("100-images.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.True(result.Statistics.ImagesEmbedded >= 100, 
            $"Should embed at least 100 images, embedded {result.Statistics.ImagesEmbedded}");
        
        // Should complete within reasonable time
        Assert.True(stopwatch.Elapsed.TotalSeconds < 15, 
            $"100 images should export in <15 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        // Verify all images are embedded
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts >= 100, $"Should have at least 100 embedded images, found {imageParts}");
    }

    [Fact]
    public async Task Performance_MixedContent_50Diagrams_50Images_ExportsSuccessfully()
    {
        // Arrange - Mix of diagrams and images
        var sb = new StringBuilder();
        sb.AppendLine("# Mixed Content Test");
        sb.AppendLine();

        // Add 50 images
        for (int i = 1; i <= 50; i++)
        {
            var imagePath = Path.Combine(_testDirectory, $"photo{i}.png");
            CreateTestImage(imagePath, 300, 200);
            
            sb.AppendLine($"## Image {i}");
            sb.AppendLine($"![Photo {i}](photo{i}.png)");
            sb.AppendLine();
        }

        // Add 50 Mermaid diagrams
        for (int i = 1; i <= 50; i++)
        {
            sb.AppendLine($"## Diagram {i}");
            sb.AppendLine("```mermaid");
            sb.AppendLine("graph LR");
            sb.AppendLine($"    A{i}[Node A] --> B{i}[Node B]");
            sb.AppendLine($"    B{i} --> C{i}[Node C]");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        var markdown = sb.ToString();
        var markdownPath = CreateTestMarkdownFile(markdown, "mixed-content.md");
        var outputPath = GetOutputPath("mixed-content.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.Equal(50, result.Statistics.MermaidDiagramsRendered);
        Assert.True(result.Statistics.ImagesEmbedded >= 50, 
            $"Should embed at least 50 images, embedded {result.Statistics.ImagesEmbedded}");
        
        // Should complete within reasonable time
        Assert.True(stopwatch.Elapsed.TotalSeconds < 25, 
            $"Mixed content should export in <25 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        // Verify total images (50 photos + 50 diagrams)
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart.ImageParts.Count();
        Assert.True(imageParts >= 100, $"Should have at least 100 total images, found {imageParts}");
    }

    #endregion

    #region Memory Usage Tests

    [Fact]
    public async Task Performance_MemoryUsage_StaysWithinReasonableLimits()
    {
        // Arrange - Large document with many elements
        var markdown = GenerateMarkdownWithSize(2000, includeDiagrams: true, diagramCount: 20);
        var markdownPath = CreateTestMarkdownFile(markdown, "memory-test.md");
        var outputPath = GetOutputPath("memory-test.docx");

        // Measure memory before
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(false);

        // Act
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);

        // Measure memory after
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(false);

        var memoryUsedMB = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        
        // Memory usage should be reasonable (<500MB as per design doc)
        Assert.True(memoryUsedMB < 500, 
            $"Memory usage should be <500MB, used {memoryUsedMB:F2}MB");
    }

    [Fact]
    public async Task Performance_MultipleExports_NoMemoryLeak()
    {
        // Arrange
        var markdown = GenerateMarkdownWithSize(200, includeDiagrams: true, diagramCount: 5);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(false);

        // Act - Perform multiple exports
        for (int i = 1; i <= 10; i++)
        {
            var markdownPath = CreateTestMarkdownFile(markdown, $"export{i}.md");
            var outputPath = GetOutputPath($"export{i}.docx");

            var result = await _exportService.ExportToWordAsync(
                markdown,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            Assert.True(result.Success, $"Export {i} should succeed");
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(false);

        var memoryIncreaseMB = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);

        // Assert - Memory increase should be minimal (no significant leak)
        Assert.True(memoryIncreaseMB < 100, 
            $"Memory increase after 10 exports should be <100MB, increased {memoryIncreaseMB:F2}MB");
    }

    #endregion

    #region Throughput Tests

    [Fact]
    public async Task Performance_Throughput_ProcessesElementsEfficiently()
    {
        // Arrange - Document with many elements
        var sb = new StringBuilder();
        sb.AppendLine("# Throughput Test");
        
        // Add 100 headings
        for (int i = 1; i <= 100; i++)
        {
            sb.AppendLine($"## Heading {i}");
            sb.AppendLine($"Content for section {i}");
            sb.AppendLine();
        }
        
        // Add 50 lists
        for (int i = 1; i <= 50; i++)
        {
            sb.AppendLine($"### List {i}");
            sb.AppendLine("- Item 1");
            sb.AppendLine("- Item 2");
            sb.AppendLine("- Item 3");
            sb.AppendLine();
        }
        
        // Add 20 tables
        for (int i = 1; i <= 20; i++)
        {
            sb.AppendLine($"### Table {i}");
            sb.AppendLine("| A | B | C |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine("| 1 | 2 | 3 |");
            sb.AppendLine();
        }

        var markdown = sb.ToString();
        var markdownPath = CreateTestMarkdownFile(markdown, "throughput.md");
        var outputPath = GetOutputPath("throughput.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.Equal(20, result.Statistics.TablesProcessed);
        
        // Calculate throughput
        var totalElements = 100 + 50 + 20; // headings + lists + tables
        var elementsPerSecond = totalElements / stopwatch.Elapsed.TotalSeconds;
        
        // Should process at least 10 elements per second
        Assert.True(elementsPerSecond > 10, 
            $"Should process >10 elements/second, processed {elementsPerSecond:F2} elements/second");
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task Performance_StressTest_VeryLargeDocument()
    {
        // Arrange - Very large document (5MB+)
        var markdown = GenerateMarkdownWithSize(5000, includeDiagrams: true, diagramCount: 30);
        var markdownPath = CreateTestMarkdownFile(markdown, "stress-test.md");
        var outputPath = GetOutputPath("stress-test.docx");

        var fileInfo = new FileInfo(markdownPath);
        Assert.True(fileInfo.Length > 5 * 1024 * 1024, 
            $"Test file should be >5MB, actual: {fileInfo.Length / (1024 * 1024)}MB");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed even for very large files. Error: {result.ErrorMessage}");
        
        // Should complete within reasonable time (allowing more time for very large files)
        Assert.True(stopwatch.Elapsed.TotalSeconds < 60, 
            $"Very large file should export in <60 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
        
        Assert.True(File.Exists(outputPath), "Output file should exist");
        Assert.True(new FileInfo(outputPath).Length > 0, "Output file should not be empty");
    }

    [Fact]
    public async Task Performance_StressTest_MaximumDiagrams()
    {
        // Arrange - Maximum diagrams test (150 diagrams)
        var markdown = GenerateMarkdownWithSize(500, includeDiagrams: true, diagramCount: 150);
        var markdownPath = CreateTestMarkdownFile(markdown, "max-diagrams.md");
        var outputPath = GetOutputPath("max-diagrams.docx");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            null,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed with maximum diagrams. Error: {result.ErrorMessage}");
        Assert.Equal(150, result.Statistics.MermaidDiagramsRendered);
        
        // Should complete within reasonable time
        Assert.True(stopwatch.Elapsed.TotalSeconds < 60, 
            $"150 diagrams should export in <60 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    #endregion

    #region Progress Reporting Performance

    [Fact]
    public async Task Performance_ProgressReporting_DoesNotSlowDownExport()
    {
        // Arrange
        var markdown = GenerateMarkdownWithSize(500, includeDiagrams: true, diagramCount: 20);
        var markdownPath = CreateTestMarkdownFile(markdown, "progress-test.md");
        var outputPath = GetOutputPath("progress-test.docx");

        int progressUpdateCount = 0;
        var progress = new Progress<ExportProgress>(p => progressUpdateCount++);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _exportService.ExportToWordAsync(
            markdown,
            markdownPath,
            outputPath,
            progress,
            CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Export should succeed. Error: {result.ErrorMessage}");
        Assert.True(progressUpdateCount > 0, "Should have received progress updates");
        
        // Progress reporting should not significantly impact performance
        Assert.True(stopwatch.Elapsed.TotalSeconds < 15, 
            $"Export with progress reporting should complete in <15 seconds, took {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    #endregion
}
