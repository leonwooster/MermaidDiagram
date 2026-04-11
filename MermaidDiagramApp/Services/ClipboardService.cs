using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Copies PNG image data to the Windows clipboard using the WinRT DataPackage API.
/// </summary>
public class ClipboardService : IClipboardService
{
    private readonly ILogger _logger;

    public ClipboardService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> CopyPngToClipboardAsync(byte[] pngData)
    {
        if (pngData == null || pngData.Length == 0)
            return false;

        try
        {
            var dataPackage = new DataPackage();
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(pngData.AsBuffer());
            stream.Seek(0);

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to set clipboard content: {ex.Message}", ex);
            return false;
        }
    }
}
