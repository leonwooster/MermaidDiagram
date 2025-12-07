using Xunit;
using Moq;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Markdig.Syntax;
using System.Collections.Generic;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Unit tests for MarkdownToWordExportService.
/// </summary>
public class MarkdownToWordExportServiceTests
{
    private readonly Mock<IMarkdownParser> _mockParser;
    private readonly Mock<IWordDocumentGenerator> _mockWordGenerator;
    private readonly Mock<IMermaidImageRenderer> _mockMermaidRenderer;
    private readonly Mock<ILogger> _mockLogger;
    private readonly MarkdownToWordExportService _service;

    public MarkdownToWordExportServiceTests()
    {
        _mockParser = new Mock<IMarkdownParser>();
        _mockWordGenerator = new Mock<IWordDocumentGenerator>();
        _mockMermaidRenderer = new Mock<IMermaidImageRenderer>();
        _mockLogger = new Mock<ILogger>();

        _service = new MarkdownToWordExportService(
            _mockParser.Object,
            _mockWordGenerator.Object,
            _mockMermaidRenderer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExportToWordAsync_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var markdownContent = "# Test Heading\n\nSome content.";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        try
        {
            // Act
            var result = await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(outputPath, result.OutputPath);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WithEmptyContent_ReturnsFailureResult()
    {
        // Arrange
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act
            var result = await _service.ExportToWordAsync(
                string.Empty,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WithEmptyOutputPath_ReturnsFailureResult()
    {
        // Arrange
        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();

        try
        {
            // Act
            var result = await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                string.Empty,
                null,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_ReportsProgressAtMultipleStages()
    {
        // Arrange
        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var progressReports = new List<ExportProgress>();
        var progress = new Progress<ExportProgress>(p => progressReports.Add(p));

        try
        {
            // Act
            await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                progress,
                CancellationToken.None);

            // Assert
            Assert.NotEmpty(progressReports);
            Assert.Contains(progressReports, p => p.Stage == ExportStage.Parsing);
            Assert.Contains(progressReports, p => p.Stage == ExportStage.RenderingDiagrams);
            Assert.Contains(progressReports, p => p.Stage == ExportStage.ResolvingImages);
            Assert.Contains(progressReports, p => p.Stage == ExportStage.GeneratingDocument);
            Assert.Contains(progressReports, p => p.Stage == ExportStage.Complete);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _service.ExportToWordAsync(
                    markdownContent,
                    markdownPath,
                    outputPath,
                    null,
                    cts.Token);
            });
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WithMermaidDiagrams_RendersDiagrams()
    {
        // Arrange
        var markdownContent = "# Test\n\n```mermaid\ngraph TD\n  A --> B\n```";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var mermaidBlocks = new List<MermaidBlock>
        {
            new MermaidBlock
            {
                Code = "graph TD\n  A --> B",
                LineNumber = 3
            }
        };

        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(mermaidBlocks);
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var tempImagePath = Path.GetTempFileName();
        _mockMermaidRenderer.Setup(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempImagePath);

        try
        {
            File.WriteAllText(tempImagePath, "test");

            // Act
            var result = await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.Statistics.MermaidDiagramsRendered);
            _mockMermaidRenderer.Verify(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                ImageFormat.PNG,
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            if (File.Exists(tempImagePath))
                File.Delete(tempImagePath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WithImages_ResolvesImagePaths()
    {
        // Arrange
        var markdownContent = "# Test\n\n![Alt text](image.png)";
        var markdownPath = Path.Combine(Path.GetTempPath(), "test.md");
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var imageReferences = new List<ImageReference>
        {
            new ImageReference
            {
                OriginalPath = "image.png",
                AltText = "Alt text",
                LineNumber = 3
            }
        };

        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(imageReferences);

        try
        {
            File.WriteAllText(markdownPath, markdownContent);

            // Act
            var result = await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            // Note: Image won't be embedded if file doesn't exist, but path resolution should occur
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WithException_ReturnsFailureResult()
    {
        // Arrange
        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        _mockParser.Setup(p => p.Parse(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Test exception"));

        try
        {
            // Act
            var result = await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Test exception", result.ErrorMessage);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_CallsWordGeneratorMethods()
    {
        // Arrange
        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        try
        {
            // Act
            await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            _mockWordGenerator.Verify(w => w.CreateDocument(outputPath), Times.Once);
            _mockWordGenerator.Verify(w => w.Save(), Times.Once);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_RecordsDuration()
    {
        // Arrange
        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        try
        {
            // Act
            var result = await _service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Duration > TimeSpan.Zero);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
