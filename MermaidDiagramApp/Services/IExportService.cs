using System.Threading.Tasks;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Encapsulates SVG and PNG export logic (image data manipulation and file writing).
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Adds a dark background rectangle to SVG content to make white lines visible.
    /// </summary>
    /// <param name="svgContent">The original SVG content.</param>
    /// <returns>SVG content with a background rectangle inserted, or the original content if parsing fails.</returns>
    string AddBackgroundToSvg(string svgContent);

    /// <summary>
    /// Scales PNG image data by the specified factor using SkiaSharp.
    /// </summary>
    /// <param name="pngData">The original PNG image data.</param>
    /// <param name="scale">The scale factor (e.g., 2.0 for 2x size).</param>
    /// <returns>The scaled PNG image data.</returns>
    Task<byte[]> ScaleImageAsync(byte[] pngData, float scale);

    /// <summary>
    /// Saves SVG content to a file.
    /// </summary>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="svgContent">The SVG content to save.</param>
    Task SaveSvgAsync(string filePath, string svgContent);

    /// <summary>
    /// Saves PNG image data to a file.
    /// </summary>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="pngData">The PNG image data to save.</param>
    Task SavePngAsync(string filePath, byte[] pngData);
}
