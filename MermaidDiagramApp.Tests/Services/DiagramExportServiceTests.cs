using Moq;
using Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for DiagramExportService.
/// Feature: diagram-click-to-enlarge (Task 2.3)
/// </summary>
public class DiagramExportServiceTests
{
    private readonly Mock<IExportService> _mockExportService;
    private readonly Mock<ILogger> _mockLogger;
    private readonly DiagramExportService _service;

    // A minimal valid SVG that Svg.Skia can parse and rasterize
    private const string ValidSvg =
        @"<svg width=""100"" height=""50"" xmlns=""http://www.w3.org/2000/svg""><rect width=""100"" height=""50"" fill=""blue""/></svg>";

    public DiagramExportServiceTests()
    {
        _mockExportService = new Mock<IExportService>();
        _mockLogger = new Mock<ILogger>();

        // By default, AddBackgroundToSvg returns the input unchanged (passthrough)
        _mockExportService
            .Setup(s => s.AddBackgroundToSvg(It.IsAny<string>()))
            .Returns((string svg) => svg);

        _service = new DiagramExportService(_mockExportService.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullExportService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new DiagramExportService(null!, _mockLogger.Object));
        Assert.Equal("exportService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new DiagramExportService(_mockExportService.Object, null!));
        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region Null / Empty SVG Tests

    [Fact]
    public async Task RasterizeSvgToPngAsync_WithNull_ReturnsEmptyArray()
    {
        var result = await _service.RasterizeSvgToPngAsync(null!);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RasterizeSvgToPngAsync_WithEmptyString_ReturnsEmptyArray()
    {
        var result = await _service.RasterizeSvgToPngAsync(string.Empty);
        Assert.Empty(result);
    }

    #endregion

    #region Valid SVG Tests

    [Fact]
    public async Task RasterizeSvgToPngAsync_WithValidSvg_ReturnsNonEmptyPngBytes()
    {
        var result = await _service.RasterizeSvgToPngAsync(ValidSvg);

        Assert.NotEmpty(result);
        // PNG magic bytes: 0x89 0x50 0x4E 0x47
        Assert.True(result.Length >= 8, "PNG output should be at least 8 bytes");
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]);
        Assert.Equal(0x4E, result[2]);
        Assert.Equal(0x47, result[3]);
    }

    [Fact]
    public async Task RasterizeSvgToPngAsync_CallsAddBackgroundToSvg()
    {
        await _service.RasterizeSvgToPngAsync(ValidSvg);

        _mockExportService.Verify(s => s.AddBackgroundToSvg(ValidSvg), Times.Once);
    }

    #endregion

    #region Scale Parameter Tests

    [Fact]
    public async Task RasterizeSvgToPngAsync_HigherScale_ProducesLargerOutput()
    {
        var resultScale1 = await _service.RasterizeSvgToPngAsync(ValidSvg, scale: 1.0f);
        var resultScale3 = await _service.RasterizeSvgToPngAsync(ValidSvg, scale: 3.0f);

        Assert.NotEmpty(resultScale1);
        Assert.NotEmpty(resultScale3);
        // A 3x scale image should produce more bytes than a 1x scale image
        Assert.True(resultScale3.Length > resultScale1.Length,
            $"Scale 3 output ({resultScale3.Length} bytes) should be larger than scale 1 ({resultScale1.Length} bytes)");
    }

    [Fact]
    public async Task RasterizeSvgToPngAsync_DefaultScale_Is2()
    {
        // Call without explicit scale — should use default 2.0f
        var result = await _service.RasterizeSvgToPngAsync(ValidSvg);

        Assert.NotEmpty(result);
        // Verify it produces valid PNG (default scale = 2.0)
        Assert.Equal(0x89, result[0]);
    }

    #endregion
}
