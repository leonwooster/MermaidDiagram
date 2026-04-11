using Xunit;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Property-based tests for ClipboardService.
/// Feature: copy-diagram-to-clipboard
/// </summary>
public class ClipboardServicePropertyTests
{
    // ---------------------------------------------------------------
    // Property 3: ClipboardService rejects empty input
    // For any null or zero-length byte array, CopyPngToClipboardAsync
    // returns false without attempting clipboard operations.
    // Validates: Requirements 5.1
    // ---------------------------------------------------------------

    [Fact]
    public async Task NullInput_ReturnsFalse()
    {
        var mockLogger = new Mock<ILogger>();
        var service = new ClipboardService(mockLogger.Object);

        var result = await service.CopyPngToClipboardAsync(null!);

        Assert.False(result);
        mockLogger.Verify(
            l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>()),
            Times.Never);
    }

    [Fact]
    public async Task EmptyArray_ReturnsFalse()
    {
        var mockLogger = new Mock<ILogger>();
        var service = new ClipboardService(mockLogger.Object);

        var result = await service.CopyPngToClipboardAsync(Array.Empty<byte>());

        Assert.False(result);
        mockLogger.Verify(
            l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<IReadOnlyDictionary<string, object?>?>()),
            Times.Never);
    }

    [Property(MaxTest = 10)]
    public bool NullInput_AlwaysReturnsFalse(bool _unused)
    {
        var mockLogger = new Mock<ILogger>();
        var service = new ClipboardService(mockLogger.Object);

        var result = service.CopyPngToClipboardAsync(null!).GetAwaiter().GetResult();

        return !result;
    }

    [Property(MaxTest = 10)]
    public bool EmptyArrayInput_AlwaysReturnsFalse(bool _unused)
    {
        var mockLogger = new Mock<ILogger>();
        var service = new ClipboardService(mockLogger.Object);

        var result = service.CopyPngToClipboardAsync(Array.Empty<byte>()).GetAwaiter().GetResult();

        return !result;
    }
}
