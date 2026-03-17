using Moq;
using Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for ExportService.
/// Requirements: 6.4
/// </summary>
public class ExportServiceTests : IDisposable
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly ExportService _service;
    private readonly List<string> _tempFiles = new();

    public ExportServiceTests()
    {
        _mockLogger = new Mock<ILogger>();
        _service = new ExportService(_mockLogger.Object);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { }
        }
    }

    private string CreateTempFile()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        return path;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ExportService(null!));
        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region AddBackgroundToSvg Tests

    [Fact]
    public void AddBackgroundToSvg_WithValidSvg_InsertsBackgroundRect()
    {
        var svg = @"<svg width=""200"" height=""100"" xmlns=""http://www.w3.org/2000/svg""><circle r=""50""/></svg>";

        var result = _service.AddBackgroundToSvg(svg);

        Assert.Contains(@"<rect x=""0"" y=""0"" width=""200"" height=""100"" fill=""#222222""/>", result);
        Assert.Contains("<circle", result);
    }

    [Fact]
    public void AddBackgroundToSvg_WithEmptyString_ReturnsEmptyString()
    {
        var result = _service.AddBackgroundToSvg(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void AddBackgroundToSvg_WithNull_ReturnsEmptyString()
    {
        var result = _service.AddBackgroundToSvg(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void AddBackgroundToSvg_WithMalformedSvg_NoClosingBracket_ReturnsOriginal()
    {
        var malformed = "<svg width=\"100\" height=\"50\"";

        var result = _service.AddBackgroundToSvg(malformed);

        Assert.Equal(malformed, result);
    }

    [Fact]
    public void AddBackgroundToSvg_ExtractsWidthAndHeight()
    {
        var svg = @"<svg width=""400px"" height=""300px""><text>Hello</text></svg>";

        var result = _service.AddBackgroundToSvg(svg);

        Assert.Contains(@"width=""400""", result);
        Assert.Contains(@"height=""300""", result);
    }

    [Fact]
    public void AddBackgroundToSvg_WithoutDimensions_UsesDefaults()
    {
        var svg = @"<svg xmlns=""http://www.w3.org/2000/svg""><line/></svg>";

        var result = _service.AddBackgroundToSvg(svg);

        Assert.Contains(@"width=""1200""", result);
        Assert.Contains(@"height=""800""", result);
    }

    #endregion

    #region SaveSvgAsync Tests

    [Fact]
    public async Task SaveSvgAsync_WritesContentToFile()
    {
        var path = CreateTempFile();
        var svgContent = @"<svg><circle r=""10""/></svg>";

        await _service.SaveSvgAsync(path, svgContent);

        var written = await File.ReadAllTextAsync(path);
        Assert.Equal(svgContent, written);
    }

    [Fact]
    public async Task SaveSvgAsync_WithEmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SaveSvgAsync(string.Empty, "<svg/>"));
    }

    #endregion

    #region SavePngAsync Tests

    [Fact]
    public async Task SavePngAsync_WritesBytesToFile()
    {
        var path = CreateTempFile();
        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        await _service.SavePngAsync(path, pngData);

        var written = await File.ReadAllBytesAsync(path);
        Assert.Equal(pngData, written);
    }

    [Fact]
    public async Task SavePngAsync_WithEmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePngAsync(string.Empty, new byte[] { 1, 2, 3 }));
    }

    #endregion

    #region ScaleImageAsync Tests

    [Fact]
    public async Task ScaleImageAsync_WithEmptyData_ReturnsEmptyArray()
    {
        var result = await _service.ScaleImageAsync(Array.Empty<byte>(), 2.0f);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ScaleImageAsync_WithNullData_ReturnsEmptyArray()
    {
        var result = await _service.ScaleImageAsync(null!, 2.0f);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ScaleImageAsync_WithScaleOne_ReturnsOriginalData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var result = await _service.ScaleImageAsync(data, 1.0f);

        Assert.Equal(data, result);
    }

    #endregion
}
