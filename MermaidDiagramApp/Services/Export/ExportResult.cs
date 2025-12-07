using System;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents the result of a Markdown to Word export operation.
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Gets or sets whether the export was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the path to the generated Word document.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the error message if the export failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the duration of the export operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets statistics about the export operation.
    /// </summary>
    public ExportStatistics Statistics { get; set; } = new();
}
