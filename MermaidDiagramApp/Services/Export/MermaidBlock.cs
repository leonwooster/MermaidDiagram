namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents a Mermaid code block found in a Markdown document.
/// </summary>
public class MermaidBlock
{
    /// <summary>
    /// Gets or sets the Mermaid diagram code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number where the block appears in the source document.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the path to the rendered image file (populated after rendering).
    /// </summary>
    public string? RenderedImagePath { get; set; }

    /// <summary>
    /// Gets or sets the error message if rendering failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
