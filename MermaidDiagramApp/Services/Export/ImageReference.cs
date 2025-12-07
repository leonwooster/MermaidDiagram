namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents an image reference found in a Markdown document.
/// </summary>
public class ImageReference
{
    /// <summary>
    /// Gets or sets the original image path as specified in the Markdown.
    /// </summary>
    public string OriginalPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved absolute path to the image file.
    /// </summary>
    public string? ResolvedPath { get; set; }

    /// <summary>
    /// Gets or sets the alt text for the image.
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number where the image reference appears.
    /// </summary>
    public int LineNumber { get; set; }
}
