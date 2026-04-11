using System;
using System.Threading.Tasks;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Implements the "Copy as Image" orchestration logic.
/// Pure service — no WinUI dependencies — so it is fully testable.
/// </summary>
public class CopyAsImageOrchestrator : ICopyAsImageOrchestrator
{
    private readonly IClipboardService _clipboardService;
    private readonly ILogger _logger;

    public CopyAsImageOrchestrator(IClipboardService clipboardService, ILogger logger)
    {
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(
        bool isWebViewReady,
        string? editorText,
        Func<Task<byte[]>> capturePng)
    {
        if (!isWebViewReady)
        {
            return "Preview is not ready yet";
        }

        if (string.IsNullOrWhiteSpace(editorText))
        {
            return "Nothing to copy \u2014 the preview is empty";
        }

        try
        {
            byte[] pngData = await capturePng();

            if (pngData.Length == 0)
            {
                return "Failed to copy image to clipboard";
            }

            var success = await _clipboardService.CopyPngToClipboardAsync(pngData);
            return success
                ? "Image copied to clipboard"
                : "Failed to copy image to clipboard";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Copy as image failed: {ex.Message}", ex);
            return "Failed to copy image to clipboard";
        }
    }
}
