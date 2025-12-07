namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents options for embedding images in a Word document.
/// </summary>
public class ImageOptions
{
    /// <summary>
    /// Gets or sets the maximum width in pixels.
    /// </summary>
    public int MaxWidth { get; set; } = 600;

    /// <summary>
    /// Gets or sets the maximum height in pixels.
    /// </summary>
    public int MaxHeight { get; set; } = 800;

    /// <summary>
    /// Gets or sets whether to maintain the image's aspect ratio when scaling.
    /// </summary>
    public bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// Gets or sets the horizontal alignment of the image.
    /// </summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
}

/// <summary>
/// Horizontal alignment options for images.
/// </summary>
public enum HorizontalAlignment
{
    Left,
    Center,
    Right
}
