using Xunit;
using Moq;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Unit tests for WebView2MermaidImageRenderer.
/// Tests valid Mermaid rendering, PNG format output, transparent background,
/// and error handling for invalid syntax.
/// Requirements: 4.1, 4.2
/// </summary>
public class WebView2MermaidImageRendererTests
{
    private readonly Mock<IWebView2Wrapper> _mockWebView;
    private readonly Mock<ILogger> _mockLogger;

    public WebView2MermaidImageRendererTests()
    {
        _mockWebView = new Mock<IWebView2Wrapper>();
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public async Task RenderToImageAsync_WithValidMermaidCode_CreatesFile()
    {
        // Arrange
        var validCode = "graph TD\n    A[Start] --> B[Process] --> C[End]";
        var svgContent = "<svg width=\"300\" height=\"200\"><rect width=\"100\" height=\"50\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        _mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");

        try
        {
            // Act
            var result = await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.PNG,
                CancellationToken.None
            );

            // Assert
            Assert.Equal(outputPath, result);
            Assert.True(File.Exists(result));

            var fileInfo = new FileInfo(result);
            Assert.True(fileInfo.Length > 0);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_PngFormat_OutputsValidPngFile()
    {
        // Arrange
        var validCode = "flowchart LR\n    A --> B --> C";
        var svgContent = "<svg width=\"200\" height=\"100\"><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        _mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");

        try
        {
            // Act
            await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.PNG,
                CancellationToken.None
            );

            // Assert: Check PNG file signature
            var fileBytes = await File.ReadAllBytesAsync(outputPath);
            Assert.True(fileBytes.Length >= 8);

            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
            Assert.Equal(0x89, fileBytes[0]);
            Assert.Equal(0x50, fileBytes[1]);
            Assert.Equal(0x4E, fileBytes[2]);
            Assert.Equal(0x47, fileBytes[3]);
            Assert.Equal(0x0D, fileBytes[4]);
            Assert.Equal(0x0A, fileBytes[5]);
            Assert.Equal(0x1A, fileBytes[6]);
            Assert.Equal(0x0A, fileBytes[7]);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_PngFormat_HasTransparentBackground()
    {
        // Arrange
        var validCode = "sequenceDiagram\n    Alice->>Bob: Hello";
        var svgContent = "<svg width=\"300\" height=\"200\"><rect x=\"10\" y=\"10\" width=\"100\" height=\"50\" fill=\"blue\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        _mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");

        try
        {
            // Act
            await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.PNG,
                CancellationToken.None
            );

            // Assert: Load and verify transparency
            using var bitmap = SkiaSharp.SKBitmap.Decode(outputPath);
            Assert.NotNull(bitmap);

            // Verify RGBA color type (supports transparency)
            Assert.True(
                bitmap.ColorType == SkiaSharp.SKColorType.Rgba8888 ||
                bitmap.ColorType == SkiaSharp.SKColorType.Bgra8888,
                "PNG should support transparency with RGBA color type"
            );

            // Check that at least some pixels are transparent
            // (corners should be transparent in most diagrams)
            var topLeftPixel = bitmap.GetPixel(0, 0);
            var topRightPixel = bitmap.GetPixel(bitmap.Width - 1, 0);
            var bottomLeftPixel = bitmap.GetPixel(0, bitmap.Height - 1);
            var bottomRightPixel = bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1);

            // At least one corner should be transparent
            var hasTransparency =
                topLeftPixel.Alpha < 255 ||
                topRightPixel.Alpha < 255 ||
                bottomLeftPixel.Alpha < 255 ||
                bottomRightPixel.Alpha < 255;

            Assert.True(hasTransparency, "PNG should have transparent pixels");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_WithInvalidSyntax_ThrowsInvalidOperationException()
    {
        // Arrange: Simulate WebView2 returning empty SVG (rendering failed)
        var invalidCode = "graph TD\n    A[Unclosed bracket";
        var emptySvgJson = System.Text.Json.JsonSerializer.Serialize("");

        _mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(emptySvgJson);

        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await renderer.RenderToImageAsync(
                    invalidCode,
                    outputPath,
                    ImageFormat.PNG,
                    CancellationToken.None
                )
            );

            Assert.Contains("Failed to get SVG content", exception.Message);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_WithNullCode_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await renderer.RenderToImageAsync(
                    null,
                    outputPath,
                    ImageFormat.PNG,
                    CancellationToken.None
                )
            );
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_WithEmptyCode_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await renderer.RenderToImageAsync(
                    "",
                    outputPath,
                    ImageFormat.PNG,
                    CancellationToken.None
                )
            );
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_WithNullOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderToImageAsync(
                validCode,
                null,
                ImageFormat.PNG,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task RenderToImageAsync_WithSvgFormat_SavesSvgFile()
    {
        // Arrange
        var validCode = "pie title Pets\n    \"Dogs\" : 386\n    \"Cats\" : 85";
        var svgContent = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"300\"><circle cx=\"200\" cy=\"150\" r=\"100\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        _mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");

        try
        {
            // Act
            await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.SVG,
                CancellationToken.None
            );

            // Assert
            Assert.True(File.Exists(outputPath));

            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Equal(svgContent, content);
            Assert.Contains("<svg", content);
            Assert.Contains("xmlns", content);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await renderer.RenderToImageAsync(
                    validCode,
                    outputPath,
                    ImageFormat.PNG,
                    cts.Token
                )
            );
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RenderToImageAsync_LogsDebugMessages()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var svgContent = "<svg><rect width=\"100\" height=\"100\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        _mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(_mockWebView.Object, _mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act
            await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.PNG,
                CancellationToken.None
            );

            // Assert: Verify logging occurred
            _mockLogger.Verify(
                l => l.Log(LogLevel.Debug, It.IsAny<string>(), null, null),
                Times.AtLeastOnce()
            );

            _mockLogger.Verify(
                l => l.Log(LogLevel.Information, It.IsAny<string>(), null, null),
                Times.AtLeastOnce()
            );
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Constructor_WithNullWebView_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WebView2MermaidImageRenderer(null, _mockLogger.Object)
        );
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WebView2MermaidImageRenderer(_mockWebView.Object, null)
        );
    }
}
