using System.Threading;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Interface for rendering Mermaid diagrams to image files.
/// </summary>
public interface IMermaidImageRenderer
{
    /// <summary>
    /// Renders a Mermaid diagram to an image file.
    /// </summary>
    /// <param name="mermaidCode">The Mermaid diagram code.</param>
    /// <param name="outputPath">The path where the image will be saved.</param>
    /// <param name="format">The desired image format.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The path to the rendered image file.</returns>
    Task<string> RenderToImageAsync(
        string mermaidCode,
        string outputPath,
        ImageFormat format,
        CancellationToken cancellationToken);
}

/// <summary>
/// Supported image formats for Mermaid diagram rendering.
/// </summary>
public enum ImageFormat
{
    PNG,
    SVG
}
