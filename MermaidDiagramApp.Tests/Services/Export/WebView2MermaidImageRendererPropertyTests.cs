using Xunit;
using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Property-based tests for WebView2MermaidImageRenderer.
/// Feature: markdown-to-word-export, Property 14: Mermaid diagram rendering
/// Validates: Requirements 4.1
/// </summary>
public class WebView2MermaidImageRendererPropertyTests
{
    /// <summary>
    /// Property: For any valid Mermaid code block, the export process should render it
    /// using WebView2 and embed the result as an image.
    /// 
    /// Note: This test uses mocks because WebView2 requires a UI thread and window context.
    /// The actual rendering is tested in integration tests.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task RenderToImageAsync_WithValidMermaidCode_CreatesImageFile()
    {
        // Arrange: Create a simple valid Mermaid diagram
        var validCode = "graph TD\n    A[Start] --> B[End]";

        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();

        // Mock the ExecuteScriptAsync to simulate successful rendering
        var svgContent = "<svg><rect width=\"100\" height=\"100\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act
            var result = await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.PNG,
                CancellationToken.None
            );

            // Assert: The result should be the output path
            Assert.Equal(outputPath, result);

            // Assert: The file should exist
            Assert.True(File.Exists(result), "Output file should exist");

            // Assert: The file should have content
            var fileInfo = new FileInfo(result);
            Assert.True(fileInfo.Length > 0, "Output file should have content");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Property: For any valid Mermaid code, rendering should not throw exceptions
    /// when given valid inputs.
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task RenderToImageAsync_WithValidInputs_DoesNotThrow()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();

        var svgContent = "<svg><rect width=\"100\" height=\"100\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act & Assert: Should not throw
            await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.PNG,
                CancellationToken.None
            );
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Property: For any null or empty Mermaid code, rendering should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderToImageAsync_WithInvalidCode_ThrowsArgumentException(string invalidCode)
    {
        // Arrange
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();
        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await renderer.RenderToImageAsync(
                    invalidCode,
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

    /// <summary>
    /// Property: For any null or empty output path, rendering should throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderToImageAsync_WithInvalidOutputPath_ThrowsArgumentException(string invalidPath)
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();
        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderToImageAsync(
                validCode,
                invalidPath,
                ImageFormat.PNG,
                CancellationToken.None
            )
        );
    }

    /// <summary>
    /// Property: Rendering should respect cancellation tokens.
    /// </summary>
    [Fact]
    public async Task RenderToImageAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();
        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

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
}

/// <summary>
/// Property-based tests for diagram image format.
/// Feature: markdown-to-word-export, Property 15: Diagram image format
/// Validates: Requirements 4.2
/// </summary>
public class MermaidImageFormatPropertyTests
{
    /// <summary>
    /// Property: For any rendered Mermaid diagram, the embedded image should be in PNG format
    /// with transparent background.
    /// </summary>
    [Fact]
    public async Task RenderToImageAsync_WithPngFormat_CreatesPngFile()
    {
        // Arrange
        var validCode = "graph TD\n    A[Start] --> B[End]";
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();

        var svgContent = "<svg><rect width=\"100\" height=\"100\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
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

            // Assert: File should exist
            Assert.True(File.Exists(result), "PNG file should exist");

            // Assert: File should have PNG signature
            var fileBytes = await File.ReadAllBytesAsync(result);
            Assert.True(fileBytes.Length > 8, "File should have content");

            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
            Assert.Equal(0x89, fileBytes[0]);
            Assert.Equal(0x50, fileBytes[1]);
            Assert.Equal(0x4E, fileBytes[2]);
            Assert.Equal(0x47, fileBytes[3]);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Property: For any rendered Mermaid diagram in PNG format, the image should have
    /// transparent background (RGBA format).
    /// </summary>
    [Fact]
    public async Task RenderToImageAsync_PngFormat_HasTransparentBackground()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();

        var svgContent = "<svg width=\"200\" height=\"100\"><rect width=\"100\" height=\"50\" fill=\"blue\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
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

            // Assert: Load the PNG and check for transparency
            using var bitmap = SkiaSharp.SKBitmap.Decode(outputPath);
            Assert.NotNull(bitmap);

            // Check that the bitmap has an alpha channel
            Assert.True(
                bitmap.ColorType == SkiaSharp.SKColorType.Rgba8888 ||
                bitmap.ColorType == SkiaSharp.SKColorType.Bgra8888,
                "PNG should have RGBA color type for transparency"
            );
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Property: For any Mermaid diagram, rendering to SVG format should produce valid SVG content.
    /// </summary>
    [Fact]
    public async Task RenderToImageAsync_WithSvgFormat_CreatesSvgFile()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();

        var svgContent = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"100\" height=\"100\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");

        try
        {
            // Act
            var result = await renderer.RenderToImageAsync(
                validCode,
                outputPath,
                ImageFormat.SVG,
                CancellationToken.None
            );

            // Assert: File should exist
            Assert.True(File.Exists(result), "SVG file should exist");

            // Assert: File should contain SVG content
            var content = await File.ReadAllTextAsync(result);
            Assert.Contains("<svg", content);
            Assert.Contains("</svg>", content);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Property: For any unsupported format, rendering should throw NotSupportedException.
    /// </summary>
    [Fact]
    public async Task RenderToImageAsync_WithUnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        var validCode = "graph TD\n    A --> B";
        var mockWebView = new Mock<IWebView2Wrapper>();
        var mockLogger = new Mock<ILogger>();

        var svgContent = "<svg><rect width=\"100\" height=\"100\"/></svg>";
        var svgJson = System.Text.Json.JsonSerializer.Serialize(svgContent);

        mockWebView
            .Setup(w => w.ExecuteScriptAsync(It.IsAny<string>()))
            .ReturnsAsync(svgJson);

        var renderer = new WebView2MermaidImageRenderer(mockWebView.Object, mockLogger.Object);
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act & Assert: Cast to an invalid enum value
            var invalidFormat = (ImageFormat)999;
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await renderer.RenderToImageAsync(
                    validCode,
                    outputPath,
                    invalidFormat,
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
}
