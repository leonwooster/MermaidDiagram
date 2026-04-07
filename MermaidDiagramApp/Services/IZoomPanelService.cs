using System;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Manages zoom panel state: zoom level, bounds clamping, and SVG content.
/// </summary>
public interface IZoomPanelService
{
    bool IsOpen { get; }
    double ZoomLevel { get; }
    string? CurrentSvgContent { get; }

    void Open(string svgContent);
    void Close();
    void ZoomIn();
    void ZoomOut();
    void SetZoomLevel(double level);
    void ApplyWheelDelta(double deltaY);

    event EventHandler<ZoomPanelStateChangedEventArgs>? StateChanged;
}
