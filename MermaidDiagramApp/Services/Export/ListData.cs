using System.Collections.Generic;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Represents list data for Word document generation.
/// </summary>
public class ListData
{
    /// <summary>
    /// Gets or sets the list items.
    /// </summary>
    public List<ListItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single item in a list, which may contain nested items.
/// </summary>
public class ListItem
{
    /// <summary>
    /// Gets or sets the text content of the list item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nesting level (0 for top-level items).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets nested list items.
    /// </summary>
    public List<ListItem> NestedItems { get; set; } = new();
}
