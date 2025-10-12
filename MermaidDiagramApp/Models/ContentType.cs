namespace MermaidDiagramApp.Models;

/// <summary>
/// Represents the type of content being rendered.
/// </summary>
public enum ContentType
{
    /// <summary>
    /// Unknown or undetected content type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Mermaid diagram content (.mmd files).
    /// </summary>
    Mermaid,

    /// <summary>
    /// Markdown documentation content (.md files).
    /// </summary>
    Markdown,

    /// <summary>
    /// Markdown with embedded Mermaid diagrams.
    /// </summary>
    MarkdownWithMermaid
}
