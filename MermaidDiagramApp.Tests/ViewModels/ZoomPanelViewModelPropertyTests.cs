using System.ComponentModel;
using System.Windows.Input;
using Xunit;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services;

namespace MermaidDiagramApp.Tests.ViewModels;

/// <summary>
/// Property-based tests for ZoomPanelViewModel.
/// Feature: diagram-click-to-enlarge
/// </summary>
public class ZoomPanelViewModelPropertyTests
{
    private const double MinZoom = 0.25;
    private const double MaxZoom = 5.0;

    /// <summary>
    /// Creates a ZoomPanelViewModel with a real ZoomPanelService.
    /// </summary>
    private static (ZoomPanelViewModel vm, ZoomPanelService service) CreateViewModel()
    {
        var service = new ZoomPanelService();
        var vm = new ZoomPanelViewModel(service);
        return (vm, service);
    }

    /// <summary>
    /// Creates a ZoomPanelViewModel with a mocked IZoomPanelService.
    /// </summary>
    private static (ZoomPanelViewModel vm, Mock<IZoomPanelService> mockService) CreateViewModelWithMock()
    {
        var mockService = new Mock<IZoomPanelService>();
        mockService.SetupGet(s => s.ZoomLevel).Returns(1.0);
        mockService.SetupGet(s => s.IsOpen).Returns(false);
        var vm = new ZoomPanelViewModel(mockService.Object);
        return (vm, mockService);
    }

    // ---------------------------------------------------------------
    // ZoomLevelDisplay format: "{level*100}%"
    // **Validates: Requirements 4.3**
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void ZoomLevelDisplay_MatchesPercentageFormat_AfterSetZoomLevel(double level)
    {
        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");
        service.SetZoomLevel(level);

        var clampedLevel = Math.Max(MinZoom, Math.Min(MaxZoom, level));
        if (double.IsNaN(level) || double.IsInfinity(level))
            clampedLevel = MinZoom;

        var expected = $"{(int)Math.Round(clampedLevel * 100)}%";
        Assert.Equal(expected, vm.ZoomLevelDisplay);
    }

    [Fact]
    public void ZoomLevelDisplay_Shows100Percent_AtDefault()
    {
        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");

        Assert.Equal("100%", vm.ZoomLevelDisplay);
    }

    [Fact]
    public void ZoomLevelDisplay_Shows125Percent_AfterZoomIn()
    {
        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");
        service.ZoomIn();

        Assert.Equal("125%", vm.ZoomLevelDisplay);
    }

    [Fact]
    public void ZoomLevelDisplay_Shows75Percent_AfterZoomOut()
    {
        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");
        service.ZoomOut();

        Assert.Equal("75%", vm.ZoomLevelDisplay);
    }

    // ---------------------------------------------------------------
    // CanZoomIn / CanZoomOut reflect bounds
    // **Validates: Requirements 4.6**
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void CanZoomIn_IsFalse_WhenAtOrAboveMax(PositiveInt extraSteps)
    {
        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");
        service.SetZoomLevel(MaxZoom);

        // Try zooming in more
        var steps = Math.Min(extraSteps.Get, 10);
        for (int i = 0; i < steps; i++)
            service.ZoomIn();

        Assert.False(vm.CanZoomIn);
    }

    [Property(MaxTest = 100)]
    public void CanZoomOut_IsFalse_WhenAtOrBelowMin(PositiveInt extraSteps)
    {
        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");
        service.SetZoomLevel(MinZoom);

        // Try zooming out more
        var steps = Math.Min(extraSteps.Get, 10);
        for (int i = 0; i < steps; i++)
            service.ZoomOut();

        Assert.False(vm.CanZoomOut);
    }

    [Property(MaxTest = 100)]
    public void CanZoomIn_IsTrue_WhenBelowMax(double level)
    {
        // Only test levels that clamp to below max
        var clamped = Math.Max(MinZoom, Math.Min(MaxZoom, level));
        if (double.IsNaN(level) || double.IsInfinity(level))
            clamped = MinZoom;
        if (clamped >= MaxZoom)
            return; // skip — not below max

        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");
        service.SetZoomLevel(level);

        Assert.True(vm.CanZoomIn);
    }

    [Property(MaxTest = 100)]
    public void CanZoomOut_IsTrue_WhenAboveMin(double level)
    {
        var clamped = Math.Max(MinZoom, Math.Min(MaxZoom, level));
        if (double.IsNaN(level) || double.IsInfinity(level))
            clamped = MinZoom;
        if (clamped <= MinZoom)
            return; // skip — not above min

        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");
        service.SetZoomLevel(level);

        Assert.True(vm.CanZoomOut);
    }

    // ---------------------------------------------------------------
    // Commands delegate to service
    // **Validates: Requirements 4.1, 4.2, 4.4**
    // ---------------------------------------------------------------

    [Fact]
    public void ZoomInCommand_DelegatesToService()
    {
        var (vm, mockService) = CreateViewModelWithMock();

        vm.ZoomInCommand.Execute(null);

        mockService.Verify(s => s.ZoomIn(), Times.Once);
    }

    [Fact]
    public void ZoomOutCommand_DelegatesToService()
    {
        var (vm, mockService) = CreateViewModelWithMock();

        vm.ZoomOutCommand.Execute(null);

        mockService.Verify(s => s.ZoomOut(), Times.Once);
    }

    [Fact]
    public void CloseCommand_DelegatesToService_AndInvokesRequestClose()
    {
        var (vm, mockService) = CreateViewModelWithMock();
        var closeCalled = false;
        vm.RequestClose = () => closeCalled = true;

        vm.CloseCommand.Execute(null);

        mockService.Verify(s => s.Close(), Times.Once);
        Assert.True(closeCalled);
    }

    [Fact]
    public void CloseCommand_DelegatesToService_WhenRequestCloseIsNull()
    {
        var (vm, mockService) = CreateViewModelWithMock();
        vm.RequestClose = null;

        // Should not throw
        vm.CloseCommand.Execute(null);

        mockService.Verify(s => s.Close(), Times.Once);
    }

    // ---------------------------------------------------------------
    // PropertyChanged fires correctly
    // **Validates: Requirements 4.3**
    // ---------------------------------------------------------------

    [Fact]
    public void PropertyChanged_Fires_WhenServiceStateChanges()
    {
        var (vm, service) = CreateViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        service.Open("<svg></svg>");

        // IsOpen changes from false to true
        Assert.Contains(nameof(ZoomPanelViewModel.IsOpen), changedProperties);

        // Now zoom in — ZoomLevelDisplay changes from "100%" to "125%"
        changedProperties.Clear();
        service.ZoomIn();

        Assert.Contains(nameof(ZoomPanelViewModel.ZoomLevelDisplay), changedProperties);
    }

    [Fact]
    public void IsOpen_UpdatesFromService_OnOpen()
    {
        var (vm, service) = CreateViewModel();

        Assert.False(vm.IsOpen);

        service.Open("<svg></svg>");

        Assert.True(vm.IsOpen);
    }

    [Fact]
    public void IsOpen_UpdatesFromService_OnClose()
    {
        var (vm, service) = CreateViewModel();
        service.Open("<svg></svg>");

        service.Close();

        Assert.False(vm.IsOpen);
    }

    // ---------------------------------------------------------------
    // Commands are non-null after construction
    // **Validates: Requirements 4.1, 4.2, 4.4**
    // ---------------------------------------------------------------

    [Fact]
    public void AllCommands_AfterConstruction_AreNonNull()
    {
        var (vm, _) = CreateViewModel();

        Assert.NotNull(vm.ZoomInCommand);
        Assert.NotNull(vm.ZoomOutCommand);
        Assert.NotNull(vm.CloseCommand);
    }

    [Fact]
    public void AllCommands_CanExecuteByDefault()
    {
        var (vm, _) = CreateViewModel();

        Assert.True(vm.ZoomInCommand.CanExecute(null));
        Assert.True(vm.ZoomOutCommand.CanExecute(null));
        Assert.True(vm.CloseCommand.CanExecute(null));
    }

    // ---------------------------------------------------------------
    // FormatZoomLevel static helper
    // **Validates: Requirements 4.3**
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(1.0, "100%")]
    [InlineData(1.25, "125%")]
    [InlineData(0.25, "25%")]
    [InlineData(5.0, "500%")]
    [InlineData(0.5, "50%")]
    [InlineData(2.0, "200%")]
    public void FormatZoomLevel_ProducesExpectedOutput(double level, string expected)
    {
        Assert.Equal(expected, ZoomPanelViewModel.FormatZoomLevel(level));
    }
}
