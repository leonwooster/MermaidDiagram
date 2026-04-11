using Moq;
using Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for ClipboardService edge cases.
/// Feature: copy-diagram-to-clipboard
/// Requirements: 5.1
/// </summary>
public class ClipboardServiceTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly ClipboardService _service;

    public ClipboardServiceTests()
    {
        _mockLogger = new Mock<ILogger>();
        _service = new ClipboardService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ClipboardService(null!));
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public async Task CopyPngToClipboardAsync_NullInput_ReturnsFalse()
    {
        var result = await _service.CopyPngToClipboardAsync(null!);
        Assert.False(result);
    }

    [Fact]
    public async Task CopyPngToClipboardAsync_EmptyArray_ReturnsFalse()
    {
        var result = await _service.CopyPngToClipboardAsync(Array.Empty<byte>());
        Assert.False(result);
    }
}
