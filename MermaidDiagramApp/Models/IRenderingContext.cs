namespace MermaidDiagramApp.Models;

/// <summary>
/// Interface for providing context information to renderers.
/// </summary>
public interface IRenderingContext
{
    /// <summary>
    /// Gets the file extension of the content being rendered.
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Gets the forced content type, if manual override is enabled.
    /// </summary>
    ContentType? ForcedContentType { get; }

    /// <summary>
    /// Gets whether Mermaid diagrams should be rendered within Markdown content.
    /// </summary>
    bool EnableMermaidInMarkdown { get; }

    /// <summary>
    /// Gets the theme mode for rendering.
    /// </summary>
    ThemeMode Theme { get; }

    /// <summary>
    /// Gets the file path of the content being rendered.
    /// </summary>
    string FilePath { get; }
}
