using System.Threading.Tasks;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Handles exporting individual diagram SVGs to PNG files.
/// </summary>
public interface IDiagramExportService
{
    /// <summary>
    /// Rasterizes SVG content to PNG bytes at the given scale.
    /// </summary>
    /// <param name="svgContent">The SVG markup to rasterize.</param>
    /// <param name="scale">The scale factor (e.g., 2.0 for 2x size).</param>
    /// <returns>PNG image bytes, or an empty array if the SVG is invalid.</returns>
    Task<byte[]> RasterizeSvgToPngAsync(string svgContent, float scale = 2.0f);
}
