namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents progress information for an export operation.
/// </summary>
public class ExportProgress
{
    /// <summary>
    /// Gets or sets the percentage complete (0-100).
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// Gets or sets a description of the current operation.
    /// </summary>
    public string CurrentOperation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current stage of the export process.
    /// </summary>
    public ExportStage Stage { get; set; }
}

/// <summary>
/// Represents the stages of the export process.
/// </summary>
public enum ExportStage
{
    Parsing,
    RenderingDiagrams,
    ResolvingImages,
    GeneratingDocument,
    Complete
}
