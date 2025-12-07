namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents styling options for a paragraph in a Word document.
/// </summary>
public class ParagraphStyle
{
    /// <summary>
    /// Gets or sets whether the text should be bold.
    /// </summary>
    public bool IsBold { get; set; }

    /// <summary>
    /// Gets or sets whether the text should be italic.
    /// </summary>
    public bool IsItalic { get; set; }

    /// <summary>
    /// Gets or sets whether the text should be formatted as code.
    /// </summary>
    public bool IsCode { get; set; }

    /// <summary>
    /// Gets or sets the font family for the text.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public int FontSize { get; set; } = 11;
}
