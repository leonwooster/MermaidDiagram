using System;

namespace MermaidDiagramApp.Models;

public class TabChangedEventArgs : EventArgs
{
    public TabState? Tab { get; init; }
    public TabState? PreviousTab { get; init; }
}
