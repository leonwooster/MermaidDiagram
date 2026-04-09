using Xunit;
using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Models;
using System;
using System.Collections.Generic;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Property-based tests for ZoomPanelService.
/// Feature: diagram-click-to-enlarge
/// </summary>
public class ZoomPanelServicePropertyTests
{
    private const double MinZoom = 0.25;
    private const double MaxZoom = 10.0;
    private const double ZoomIncrement = 0.25;

    // ---------------------------------------------------------------
    // Property 3: Zoom Bounds — ZoomLevel always in [0.25, 5.0]
    // ZoomIn() is a no-op at max; ZoomOut() is a no-op at min.
    // Validates: Requirements 4.6, 6.3
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void ZoomLevel_AlwaysWithinBounds_AfterSetZoomLevel(double level)
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");
        service.SetZoomLevel(level);

        Assert.InRange(service.ZoomLevel, MinZoom, MaxZoom);
    }

    [Property(MaxTest = 100)]
    public void ZoomLevel_AlwaysWithinBounds_AfterRepeatedZoomIn(PositiveInt count)
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");

        var iterations = Math.Min(count.Get, 200);
        for (int i = 0; i < iterations; i++)
            service.ZoomIn();

        Assert.InRange(service.ZoomLevel, MinZoom, MaxZoom);
    }

    [Property(MaxTest = 100)]
    public void ZoomLevel_AlwaysWithinBounds_AfterRepeatedZoomOut(PositiveInt count)
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");

        var iterations = Math.Min(count.Get, 200);
        for (int i = 0; i < iterations; i++)
            service.ZoomOut();

        Assert.InRange(service.ZoomLevel, MinZoom, MaxZoom);
    }

    [Fact]
    public void ZoomIn_IsNoOp_AtMaxZoom()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");
        service.SetZoomLevel(MaxZoom);

        var eventCount = 0;
        service.StateChanged += (_, _) => eventCount++;

        service.ZoomIn();

        Assert.Equal(MaxZoom, service.ZoomLevel);
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void ZoomOut_IsNoOp_AtMinZoom()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");
        service.SetZoomLevel(MinZoom);

        var eventCount = 0;
        service.StateChanged += (_, _) => eventCount++;

        service.ZoomOut();

        Assert.Equal(MinZoom, service.ZoomLevel);
        Assert.Equal(0, eventCount);
    }

    // ---------------------------------------------------------------
    // Property 3 (continued): Open/Close state transitions
    // Open() sets IsOpen=true, Close() sets IsOpen=false.
    // Validates: Requirements 3.1, 5.1
    // ---------------------------------------------------------------

    [Property(MaxTest = 50)]
    public void Open_SetsIsOpenTrue_AndStoresContent(NonEmptyString svgContent)
    {
        var service = new ZoomPanelService();
        service.Open(svgContent.Get);

        Assert.True(service.IsOpen);
        Assert.Equal(svgContent.Get, service.CurrentSvgContent);
        Assert.Equal(1.0, service.ZoomLevel);
    }

    [Property(MaxTest = 50)]
    public void Close_SetsIsOpenFalse_AndClearsState(NonEmptyString svgContent)
    {
        var service = new ZoomPanelService();
        service.Open(svgContent.Get);
        service.Close();

        Assert.False(service.IsOpen);
        Assert.Null(service.CurrentSvgContent);
        Assert.Equal(1.0, service.ZoomLevel);
    }

    [Fact]
    public void Open_WhileAlreadyOpen_ReplacesContent()
    {
        var service = new ZoomPanelService();
        service.Open("<svg>first</svg>");
        service.SetZoomLevel(2.0);

        service.Open("<svg>second</svg>");

        Assert.True(service.IsOpen);
        Assert.Equal("<svg>second</svg>", service.CurrentSvgContent);
        Assert.Equal(1.0, service.ZoomLevel);
    }

    // ---------------------------------------------------------------
    // Property 3 (continued): Wheel delta direction
    // Positive deltaY zooms out, negative deltaY zooms in.
    // Validates: Requirements 6.1, 6.2
    // ---------------------------------------------------------------

    [Fact]
    public void PositiveDeltaY_ZoomsOut()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");
        service.SetZoomLevel(2.5);

        var before = service.ZoomLevel;
        service.ApplyWheelDelta(100.0);

        Assert.True(service.ZoomLevel < before,
            $"Expected zoom to decrease from {before}, but got {service.ZoomLevel}");
    }

    [Fact]
    public void NegativeDeltaY_ZoomsIn()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");
        service.SetZoomLevel(2.5);

        var before = service.ZoomLevel;
        service.ApplyWheelDelta(-100.0);

        Assert.True(service.ZoomLevel > before,
            $"Expected zoom to increase from {before}, but got {service.ZoomLevel}");
    }

    [Fact]
    public void ApplyWheelDelta_Zero_IsNoOp()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");
        var before = service.ZoomLevel;

        var eventCount = 0;
        service.StateChanged += (_, _) => eventCount++;

        service.ApplyWheelDelta(0);

        Assert.Equal(before, service.ZoomLevel);
        Assert.Equal(0, eventCount);
    }

    [Property(MaxTest = 50)]
    public void WheelDelta_AlwaysRespectsZoomBounds(double deltaY)
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");
        service.ApplyWheelDelta(deltaY);

        Assert.InRange(service.ZoomLevel, MinZoom, MaxZoom);
    }

    // ---------------------------------------------------------------
    // Property 3 (continued): StateChanged event fires on every mutation
    // Validates: Requirements 4.1, 4.2, 4.4
    // ---------------------------------------------------------------

    [Fact]
    public void StateChanged_FiresOnOpen()
    {
        var service = new ZoomPanelService();
        var events = new List<ZoomPanelStateChangedEventArgs>();
        service.StateChanged += (_, e) => events.Add(e);

        service.Open("<svg></svg>");

        Assert.Single(events);
        Assert.True(events[0].IsOpen);
        Assert.Equal(1.0, events[0].ZoomLevel);
        Assert.Equal("<svg></svg>", events[0].SvgContent);
    }

    [Fact]
    public void StateChanged_FiresOnClose()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");

        var events = new List<ZoomPanelStateChangedEventArgs>();
        service.StateChanged += (_, e) => events.Add(e);

        service.Close();

        Assert.Single(events);
        Assert.False(events[0].IsOpen);
    }

    [Fact]
    public void StateChanged_FiresOnZoomIn()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");

        var events = new List<ZoomPanelStateChangedEventArgs>();
        service.StateChanged += (_, e) => events.Add(e);

        service.ZoomIn();

        Assert.Single(events);
        Assert.Equal(1.0 + ZoomIncrement, events[0].ZoomLevel);
    }

    [Fact]
    public void StateChanged_FiresOnZoomOut()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");

        var events = new List<ZoomPanelStateChangedEventArgs>();
        service.StateChanged += (_, e) => events.Add(e);

        service.ZoomOut();

        Assert.Single(events);
        Assert.Equal(1.0 - ZoomIncrement, events[0].ZoomLevel);
    }

    [Fact]
    public void StateChanged_FiresOnSetZoomLevel()
    {
        var service = new ZoomPanelService();
        service.Open("<svg></svg>");

        var events = new List<ZoomPanelStateChangedEventArgs>();
        service.StateChanged += (_, e) => events.Add(e);

        service.SetZoomLevel(3.0);

        Assert.Single(events);
        Assert.Equal(3.0, events[0].ZoomLevel);
    }

    [Property(MaxTest = 50)]
    public void StateChanged_EventArgs_AlwaysMatchServiceState(double zoomLevel)
    {
        var service = new ZoomPanelService();
        service.Open("<svg>test</svg>");

        ZoomPanelStateChangedEventArgs? lastEvent = null;
        service.StateChanged += (_, e) => lastEvent = e;

        service.SetZoomLevel(zoomLevel);

        Assert.NotNull(lastEvent);
        Assert.Equal(service.IsOpen, lastEvent.IsOpen);
        Assert.Equal(service.ZoomLevel, lastEvent.ZoomLevel, 3);
        Assert.Equal(service.CurrentSvgContent, lastEvent.SvgContent);
    }
}
