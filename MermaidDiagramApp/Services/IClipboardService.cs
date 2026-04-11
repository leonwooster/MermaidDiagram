using System.Threading.Tasks;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Abstracts Windows clipboard operations for PNG image data.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Places PNG image bytes on the Windows clipboard as a bitmap.
    /// Calls Clipboard.Flush so data persists after app exit.
    /// </summary>
    /// <param name="pngData">PNG image bytes to copy.</param>
    /// <returns>True if the clipboard was set successfully.</returns>
    Task<bool> CopyPngToClipboardAsync(byte[] pngData);
}
