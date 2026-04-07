using System;

namespace MermaidDiagramApp.Models;

/// <summary>
/// Event arguments for zoom panel state changes.
/// </summary>
public class ZoomPanelStateChangedEventArgs : EventArgs
{
    public bool IsOpen { get; init; }
    public double ZoomLevel { get; init; }
    public string? SvgContent { get; init; }
}
