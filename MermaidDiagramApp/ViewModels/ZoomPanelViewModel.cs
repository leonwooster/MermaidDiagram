using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MermaidDiagramApp.Commands;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services;

namespace MermaidDiagramApp.ViewModels;

/// <summary>
/// ViewModel for the zoom panel UI. Binds to ZoomPanel UserControl.
/// Subscribes to IZoomPanelService.StateChanged to keep properties in sync.
/// </summary>
public class ZoomPanelViewModel : INotifyPropertyChanged
{
    private readonly IZoomPanelService _zoomPanelService;

    private bool _isOpen;
    private string _zoomLevelDisplay = "100%";
    private bool _canZoomIn = true;
    private bool _canZoomOut = true;

    public ZoomPanelViewModel(IZoomPanelService zoomPanelService)
    {
        _zoomPanelService = zoomPanelService ?? throw new ArgumentNullException(nameof(zoomPanelService));

        // Initialize commands
        ZoomInCommand = new RelayCommand(_ => _zoomPanelService.ZoomIn());
        ZoomOutCommand = new RelayCommand(_ => _zoomPanelService.ZoomOut());
        CloseCommand = new RelayCommand(_ => ExecuteClose());

        // Subscribe to service state changes
        _zoomPanelService.StateChanged += OnServiceStateChanged;

        // Initialize from current service state
        UpdateFromServiceState(_zoomPanelService.ZoomLevel, _zoomPanelService.IsOpen);
    }

    /// <summary>
    /// Callback delegate for layout restoration, invoked by CloseCommand after closing the service.
    /// </summary>
    public Action? RequestClose { get; set; }

    #region Bindable Properties

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
    }

    public string ZoomLevelDisplay
    {
        get => _zoomLevelDisplay;
        private set => SetProperty(ref _zoomLevelDisplay, value);
    }

    public bool CanZoomIn
    {
        get => _canZoomIn;
        private set => SetProperty(ref _canZoomIn, value);
    }

    public bool CanZoomOut
    {
        get => _canZoomOut;
        private set => SetProperty(ref _canZoomOut, value);
    }

    #endregion

    #region Commands

    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand CloseCommand { get; }

    #endregion

    #region Private Methods

    private void ExecuteClose()
    {
        _zoomPanelService.Close();
        RequestClose?.Invoke();
    }

    private void OnServiceStateChanged(object? sender, ZoomPanelStateChangedEventArgs e)
    {
        UpdateFromServiceState(e.ZoomLevel, e.IsOpen);
    }

    private void UpdateFromServiceState(double zoomLevel, bool isOpen)
    {
        IsOpen = isOpen;
        ZoomLevelDisplay = FormatZoomLevel(zoomLevel);
        CanZoomIn = zoomLevel < 5.0;
        CanZoomOut = zoomLevel > 0.25;
    }

    internal static string FormatZoomLevel(double level)
    {
        var percentage = (int)Math.Round(level * 100);
        return $"{percentage}%";
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    #endregion
}
