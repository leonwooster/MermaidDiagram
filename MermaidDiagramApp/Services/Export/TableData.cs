using System.Collections.Generic;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents table data for Word document generation.
/// </summary>
public class TableData
{
    /// <summary>
    /// Gets or sets the table headers.
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the table rows (each row is a list of cell values).
    /// </summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the first row should be formatted as a header.
    /// </summary>
    public bool HasHeaderRow { get; set; } = true;
}
