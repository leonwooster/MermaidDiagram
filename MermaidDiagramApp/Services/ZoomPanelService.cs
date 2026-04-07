using System;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Manages zoom panel state with clamped zoom levels and event notifications.
/// </summary>
public class ZoomPanelService : IZoomPanelService
{
    private const double ZoomIncrement = 0.25;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 5.0;
    private const double DefaultZoom = 1.0;

    public bool IsOpen { get; private set; }
    public double ZoomLevel { get; private set; } = DefaultZoom;
    public string? CurrentSvgContent { get; private set; }

    public event EventHandler<ZoomPanelStateChangedEventArgs>? StateChanged;

    public void Open(string svgContent)
    {
        IsOpen = true;
        ZoomLevel = DefaultZoom;
        CurrentSvgContent = svgContent;
        RaiseStateChanged();
    }

    public void Close()
    {
        IsOpen = false;
        ZoomLevel = DefaultZoom;
        CurrentSvgContent = null;
        RaiseStateChanged();
    }

    public void ZoomIn()
    {
        var newLevel = Clamp(ZoomLevel + ZoomIncrement);
        if (Math.Abs(newLevel - ZoomLevel) < 0.001)
            return;
        ZoomLevel = newLevel;
        RaiseStateChanged();
    }

    public void ZoomOut()
    {
        var newLevel = Clamp(ZoomLevel - ZoomIncrement);
        if (Math.Abs(newLevel - ZoomLevel) < 0.001)
            return;
        ZoomLevel = newLevel;
        RaiseStateChanged();
    }

    public void SetZoomLevel(double level)
    {
        ZoomLevel = Clamp(level);
        RaiseStateChanged();
    }

    public void ApplyWheelDelta(double deltaY)
    {
        if (deltaY > 0)
            ZoomOut();
        else if (deltaY < 0)
            ZoomIn();
    }

    private static double Clamp(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return MinZoom;
        return Math.Max(MinZoom, Math.Min(MaxZoom, value));
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new ZoomPanelStateChangedEventArgs
        {
            IsOpen = IsOpen,
            ZoomLevel = ZoomLevel,
            SvgContent = CurrentSvgContent
        });
    }
}
