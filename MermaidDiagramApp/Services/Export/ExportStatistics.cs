namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Contains statistics about an export operation.
/// </summary>
public class ExportStatistics
{
    /// <summary>
    /// Gets or sets the total number of Markdown elements processed.
    /// </summary>
    public int TotalElements { get; set; }

    /// <summary>
    /// Gets or sets the number of Mermaid diagrams rendered.
    /// </summary>
    public int MermaidDiagramsRendered { get; set; }

    /// <summary>
    /// Gets or sets the number of images embedded.
    /// </summary>
    public int ImagesEmbedded { get; set; }

    /// <summary>
    /// Gets or sets the number of tables processed.
    /// </summary>
    public int TablesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the size of the output file in bytes.
    /// </summary>
    public long OutputFileSize { get; set; }
}
