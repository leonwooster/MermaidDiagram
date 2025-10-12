using System.Collections.Generic;
using System.Threading.Tasks;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services.Rendering;

/// <summary>
/// Interface for content rendering services.
/// Implementations handle specific content types (Mermaid, Markdown, etc.).
/// </summary>
public interface IContentRenderer
{
    /// <summary>
    /// Gets the content type this renderer supports.
    /// </summary>
    ContentType SupportedType { get; }

    /// <summary>
    /// Determines if this renderer can handle the specified content type.
    /// </summary>
    bool CanRender(ContentType type);

    /// <summary>
    /// Renders the content asynchronously.
    /// </summary>
    /// <param name="content">The raw content to render.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>The rendering result.</returns>
    Task<RenderingResult> RenderAsync(string content, IRenderingContext context);

    /// <summary>
    /// Gets the list of features supported by this renderer.
    /// </summary>
    IReadOnlyList<string> GetSupportedFeatures();
}
