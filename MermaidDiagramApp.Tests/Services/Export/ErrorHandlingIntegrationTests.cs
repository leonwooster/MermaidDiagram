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
/// Integration tests for error handling scenarios in the export workflow.
/// Tests Requirements 1.4, 2.4, 4.3, 5.3
/// </summary>
public class ErrorHandlingIntegrationTests
{
    private readonly Mock<ILogger> _mockLogger;

    public ErrorHandlingIntegrationTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    #region Missing Image Handling Tests (Requirement 5.3)

    [Fact]
    public async Task ExportToWordAsync_WithMissingImage_InsertsPlaceholderText()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test\n\n![Missing Image](nonexistent.png)";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var imageReferences = new List<ImageReference>
        {
            new ImageReference
            {
                OriginalPath = "nonexistent.png",
                AltText = "Missing Image",
                LineNumber = 3,
                ResolvedPath = null // Image not found
            }
        };

        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(imageReferences);

        try
        {
            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            
            // Verify that a placeholder was added instead of the image
            mockWordGenerator.Verify(w => w.AddParagraph(
                It.Is<string>(s => s.Contains("Image not found") && s.Contains("nonexistent.png")),
                It.IsAny<ParagraphStyle>()), Times.Once);
            
            // Verify warning was logged
            _mockLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.Is<string>(s => s.Contains("not found") || s.Contains("could not be resolved")),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.AtLeastOnce);
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
    public async Task ExportToWordAsync_WithInaccessibleImage_InsertsErrorMessage()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test\n\n![Image](test.png)";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        var imagePath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var imageReferences = new List<ImageReference>
        {
            new ImageReference
            {
                OriginalPath = "test.png",
                AltText = "Image",
                LineNumber = 3,
                ResolvedPath = imagePath
            }
        };

        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(imageReferences);

        // Simulate access denied error
        mockWordGenerator.Setup(w => w.AddImage(It.IsAny<string>(), It.IsAny<ImageOptions>()))
            .Throws(new UnauthorizedAccessException("Access denied"));

        try
        {
            File.WriteAllText(imagePath, "test");

            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            
            // Verify that an error message was added
            mockWordGenerator.Verify(w => w.AddParagraph(
                It.Is<string>(s => s.Contains("access denied") || s.Contains("Image error")),
                It.IsAny<ParagraphStyle>()), Times.Once);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    #endregion

    #region Mermaid Syntax Error Handling Tests (Requirement 4.3)

    [Fact]
    public async Task ExportToWordAsync_WithMermaidSyntaxError_InsertsErrorMessage()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test\n\n```mermaid\ninvalid syntax here\n```";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var mermaidBlocks = new List<MermaidBlock>
        {
            new MermaidBlock
            {
                Code = "invalid syntax here",
                LineNumber = 3
            }
        };

        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(mermaidBlocks);
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        // Simulate Mermaid syntax error
        mockMermaidRenderer.Setup(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mermaid syntax error: Invalid diagram type"));

        try
        {
            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            
            // Verify that an error message was added
            mockWordGenerator.Verify(w => w.AddParagraph(
                It.Is<string>(s => s.Contains("Mermaid") && s.Contains("error")),
                It.IsAny<ParagraphStyle>()), Times.Once);
            
            // Verify that the original code was included
            mockWordGenerator.Verify(w => w.AddCodeBlock(
                It.Is<string>(s => s.Contains("invalid syntax here")),
                "mermaid"), Times.Once);
            
            // Verify warning was logged
            _mockLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.Is<string>(s => s.Contains("Mermaid") && s.Contains("syntax")),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Once);
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
    public async Task ExportToWordAsync_WithMermaidRenderFailure_ContinuesWithOtherDiagrams()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test\n\n```mermaid\ngraph TD\n  A --> B\n```\n\n```mermaid\ninvalid\n```";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var mermaidBlocks = new List<MermaidBlock>
        {
            new MermaidBlock { Code = "graph TD\n  A --> B", LineNumber = 3 },
            new MermaidBlock { Code = "invalid", LineNumber = 7 }
        };

        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(mermaidBlocks);
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var tempImagePath = Path.GetTempFileName();
        
        // First diagram succeeds, second fails
        mockMermaidRenderer.SetupSequence(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempImagePath)
            .ThrowsAsync(new InvalidOperationException("Syntax error"));

        try
        {
            File.WriteAllText(tempImagePath, "test");

            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            
            // Verify both diagrams were attempted
            mockMermaidRenderer.Verify(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                ImageFormat.PNG,
                It.IsAny<CancellationToken>()), Times.Exactly(2));
            
            // Only one diagram should be successfully rendered
            Assert.Equal(1, result.Statistics.MermaidDiagramsRendered);
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

    #endregion

    #region File System Error Handling Tests (Requirements 1.4, 2.4)

    [Fact]
    public async Task ExportToWordAsync_WithInvalidOutputDirectory_ReturnsFailureResult()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        // Use an invalid path with illegal characters
        var outputPath = Path.Combine("Z:\\NonExistent\\Path\\", "output.docx");

        var document = new MarkdownDocument();
        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        try
        {
            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Access denied", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            
            // Verify error was logged
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WithFileAccessDenied_ReturnsFailureResult()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        // Simulate access denied when creating document
        mockWordGenerator.Setup(w => w.CreateDocument(It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("Access denied to file"));

        try
        {
            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Access denied", result.ErrorMessage);
            
            // Verify error was logged
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.Is<string>(s => s.Contains("Access denied")),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Once);
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
    public async Task ExportToWordAsync_WithIOError_ReturnsFailureResult()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        // Simulate I/O error
        mockWordGenerator.Setup(w => w.CreateDocument(It.IsAny<string>()))
            .Throws(new IOException("Disk full or I/O error"));

        try
        {
            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("I/O error", result.ErrorMessage);
            
            // Verify error was logged
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    #endregion

    #region Temporary File Cleanup Tests (Requirement 7.4)

    [Fact]
    public async Task ExportToWordAsync_OnSuccess_CleansUpTemporaryFiles()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test\n\n```mermaid\ngraph TD\n  A --> B\n```";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var mermaidBlocks = new List<MermaidBlock>
        {
            new MermaidBlock { Code = "graph TD\n  A --> B", LineNumber = 3 }
        };

        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(mermaidBlocks);
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var tempImagePath = Path.GetTempFileName();
        mockMermaidRenderer.Setup(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempImagePath);

        try
        {
            File.WriteAllText(tempImagePath, "test");

            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            
            // Verify cleanup was logged (temporary files should be deleted)
            _mockLogger.Verify(l => l.Log(
                LogLevel.Debug,
                It.Is<string>(s => s.Contains("Deleted temporary file") || s.Contains("temporary")),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(markdownPath))
                File.Delete(markdownPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            // Temp file should already be cleaned up by the service
        }
    }

    [Fact]
    public async Task ExportToWordAsync_OnError_StillCleansUpTemporaryFiles()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test\n\n```mermaid\ngraph TD\n  A --> B\n```";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var mermaidBlocks = new List<MermaidBlock>
        {
            new MermaidBlock { Code = "graph TD\n  A --> B", LineNumber = 3 }
        };

        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(mermaidBlocks);
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var tempImagePath = Path.GetTempFileName();
        mockMermaidRenderer.Setup(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempImagePath);

        // Simulate error during document generation
        mockWordGenerator.Setup(w => w.CreateDocument(It.IsAny<string>()))
            .Throws(new IOException("Test error"));

        try
        {
            File.WriteAllText(tempImagePath, "test");

            // Act
            var result = await service.ExportToWordAsync(
                markdownContent,
                markdownPath,
                outputPath,
                null,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            
            // Verify cleanup still occurred despite error
            _mockLogger.Verify(l => l.Log(
                LogLevel.Debug,
                It.Is<string>(s => s.Contains("Deleted temporary file") || s.Contains("temporary")),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.AtLeastOnce);
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
    public async Task ExportToWordAsync_OnCancellation_CleansUpTemporaryFiles()
    {
        // Arrange
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();

        var service = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            _mockLogger.Object);

        var markdownContent = "# Test\n\n```mermaid\ngraph TD\n  A --> B\n```";
        var markdownPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var document = new MarkdownDocument();
        var mermaidBlocks = new List<MermaidBlock>
        {
            new MermaidBlock { Code = "graph TD\n  A --> B", LineNumber = 3 }
        };

        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(mermaidBlocks);
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var tempImagePath = Path.GetTempFileName();
        var cts = new CancellationTokenSource();
        
        mockMermaidRenderer.Setup(m => m.RenderToImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ImageFormat>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string code, string path, ImageFormat format, CancellationToken ct) =>
            {
                await Task.Delay(100, ct);
                cts.Cancel(); // Cancel during rendering
                ct.ThrowIfCancellationRequested();
                return tempImagePath;
            });

        try
        {
            File.WriteAllText(tempImagePath, "test");

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await service.ExportToWordAsync(
                    markdownContent,
                    markdownPath,
                    outputPath,
                    null,
                    cts.Token);
            });
            
            // Verify cleanup occurred even on cancellation
            _mockLogger.Verify(l => l.Log(
                LogLevel.Debug,
                It.Is<string>(s => s.Contains("Deleted temporary file") || s.Contains("temporary")),
                It.IsAny<Exception>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.AtLeastOnce);
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

    #endregion
}
