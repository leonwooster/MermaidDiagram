using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MermaidDiagramApp.Commands;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Services.Rendering;

namespace MermaidDiagramApp.ViewModels;

/// <summary>
/// ViewModel for MainWindow. Holds UI state and exposes bindable properties
/// with INotifyPropertyChanged support. Services are accepted via constructor injection.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IFileOperationsService _fileOps;
    private readonly ISearchService _search;
    private readonly IMermaidUpdateService _updateService;
    private readonly IExportService _exportService;
    private readonly RenderingOrchestrator _renderingOrchestrator;
    private readonly IContentTypeDetector _contentTypeDetector;
    private readonly MarkdownStyleSettingsService _styleSettingsService;
    private readonly ILogger _logger;

    // Backing fields
    private string _currentFilePath = string.Empty;
    private ContentType _currentContentType = ContentType.Unknown;
    private bool _isFullScreen;
    private bool _isPresentationMode;
    private bool _isPanModeEnabled;
    private bool _isBuilderVisible;
    private string _currentSearchText = string.Empty;
    private string _lastPreviewedCode = string.Empty;
    private bool _isWebViewReady;

    public MainWindowViewModel(
        IFileOperationsService fileOps,
        ISearchService search,
        IMermaidUpdateService updateService,
        IExportService exportService,
        RenderingOrchestrator renderingOrchestrator,
        IContentTypeDetector contentTypeDetector,
        MarkdownStyleSettingsService styleSettingsService,
        ILogger logger)
    {
        _fileOps = fileOps ?? throw new ArgumentNullException(nameof(fileOps));
        _search = search ?? throw new ArgumentNullException(nameof(search));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _renderingOrchestrator = renderingOrchestrator ?? throw new ArgumentNullException(nameof(renderingOrchestrator));
        _contentTypeDetector = contentTypeDetector ?? throw new ArgumentNullException(nameof(contentTypeDetector));
        _styleSettingsService = styleSettingsService ?? throw new ArgumentNullException(nameof(styleSettingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize commands
        NewClassDiagramCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("classDiagram"));
        NewSequenceDiagramCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("sequenceDiagram"));
        NewStateDiagramCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("stateDiagram"));
        NewActivityDiagramCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("activityDiagram"));
        NewFlowchartCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("flowchart"));
        NewGanttChartCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("ganttChart"));
        NewPieChartCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("pieChart"));
        NewGitGraphCommand = new RelayCommand(_ => RequestNewDiagram?.Invoke("gitGraph"));

        OpenFileCommand = new RelayCommand(_ => RequestOpenFile?.Invoke());
        SaveFileCommand = new RelayCommand(_ => RequestSaveFile?.Invoke());
        CloseFileCommand = new RelayCommand(_ => ExecuteCloseFile());
        ExportSvgCommand = new RelayCommand(_ => RequestExportSvg?.Invoke());
        ExportPngCommand = new RelayCommand(_ => RequestExportPng?.Invoke());

        ToggleFullScreenCommand = new RelayCommand(_ => IsFullScreen = !IsFullScreen);
        TogglePresentationModeCommand = new RelayCommand(_ => IsPresentationMode = !IsPresentationMode);
        ToggleBuilderCommand = new RelayCommand(_ => IsBuilderVisible = !IsBuilderVisible);

        FindCommand = new RelayCommand(_ => RequestFind?.Invoke());
        CheckSyntaxCommand = new RelayCommand(_ => RequestCheckSyntax?.Invoke());
        ExitCommand = new RelayCommand(_ => RequestExit?.Invoke());
    }

    #region UI Callback Delegates

    /// <summary>
    /// Callback invoked when a new diagram is requested. Parameter is the diagram type key.
    /// </summary>
    public Action<string>? RequestNewDiagram { get; set; }

    /// <summary>
    /// Callback invoked when the user requests to open a file (requires file picker UI).
    /// </summary>
    public Action? RequestOpenFile { get; set; }

    /// <summary>
    /// Callback invoked when the user requests to save a file (requires file picker UI).
    /// </summary>
    public Action? RequestSaveFile { get; set; }

    /// <summary>
    /// Callback invoked when the user requests SVG export (requires file picker and WebView2).
    /// </summary>
    public Action? RequestExportSvg { get; set; }

    /// <summary>
    /// Callback invoked when the user requests PNG export (requires file picker and WebView2).
    /// </summary>
    public Action? RequestExportPng { get; set; }

    /// <summary>
    /// Callback invoked when the user requests the find/search panel.
    /// </summary>
    public Action? RequestFind { get; set; }

    /// <summary>
    /// Callback invoked when the user requests a syntax check (requires WebView2).
    /// </summary>
    public Action? RequestCheckSyntax { get; set; }

    /// <summary>
    /// Callback invoked when the user requests to exit the application.
    /// </summary>
    public Action? RequestExit { get; set; }

    #endregion

    #region Commands

    /// <summary>New class diagram command.</summary>
    public ICommand NewClassDiagramCommand { get; }

    /// <summary>New sequence diagram command.</summary>
    public ICommand NewSequenceDiagramCommand { get; }

    /// <summary>New state diagram command.</summary>
    public ICommand NewStateDiagramCommand { get; }

    /// <summary>New activity diagram command.</summary>
    public ICommand NewActivityDiagramCommand { get; }

    /// <summary>New flowchart command.</summary>
    public ICommand NewFlowchartCommand { get; }

    /// <summary>New Gantt chart command.</summary>
    public ICommand NewGanttChartCommand { get; }

    /// <summary>New pie chart command.</summary>
    public ICommand NewPieChartCommand { get; }

    /// <summary>New git graph command.</summary>
    public ICommand NewGitGraphCommand { get; }

    /// <summary>Open file command.</summary>
    public ICommand OpenFileCommand { get; }

    /// <summary>Save file command.</summary>
    public ICommand SaveFileCommand { get; }

    /// <summary>Close current file command.</summary>
    public ICommand CloseFileCommand { get; }

    /// <summary>Export as SVG command.</summary>
    public ICommand ExportSvgCommand { get; }

    /// <summary>Export as PNG command.</summary>
    public ICommand ExportPngCommand { get; }

    /// <summary>Toggle full screen mode command.</summary>
    public ICommand ToggleFullScreenCommand { get; }

    /// <summary>Toggle presentation mode command.</summary>
    public ICommand TogglePresentationModeCommand { get; }

    /// <summary>Toggle visual builder panel command.</summary>
    public ICommand ToggleBuilderCommand { get; }

    /// <summary>Open find/search panel command.</summary>
    public ICommand FindCommand { get; }

    /// <summary>Check Mermaid syntax command.</summary>
    public ICommand CheckSyntaxCommand { get; }

    /// <summary>Exit application command.</summary>
    public ICommand ExitCommand { get; }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Executes the close file operation by resetting state.
    /// </summary>
    private void ExecuteCloseFile()
    {
        CurrentFilePath = string.Empty;
        CurrentContentType = ContentType.Unknown;
        LastPreviewedCode = string.Empty;
        _logger.Log(LogLevel.Information, "File closed");
    }

    #endregion

    #region UI State Properties

    /// <summary>
    /// Gets or sets the current file path being edited.
    /// </summary>
    public string CurrentFilePath
    {
        get => _currentFilePath;
        set => SetProperty(ref _currentFilePath, value);
    }

    /// <summary>
    /// Gets or sets the detected content type of the current file.
    /// </summary>
    public ContentType CurrentContentType
    {
        get => _currentContentType;
        set => SetProperty(ref _currentContentType, value);
    }

    /// <summary>
    /// Gets or sets whether the application is in full screen mode.
    /// </summary>
    public bool IsFullScreen
    {
        get => _isFullScreen;
        set => SetProperty(ref _isFullScreen, value);
    }

    /// <summary>
    /// Gets or sets whether presentation mode is active.
    /// </summary>
    public bool IsPresentationMode
    {
        get => _isPresentationMode;
        set => SetProperty(ref _isPresentationMode, value);
    }

    /// <summary>
    /// Gets or sets whether pan mode is enabled in the preview.
    /// </summary>
    public bool IsPanModeEnabled
    {
        get => _isPanModeEnabled;
        set => SetProperty(ref _isPanModeEnabled, value);
    }

    /// <summary>
    /// Gets or sets whether the visual diagram builder panel is visible.
    /// </summary>
    public bool IsBuilderVisible
    {
        get => _isBuilderVisible;
        set => SetProperty(ref _isBuilderVisible, value);
    }

    /// <summary>
    /// Gets or sets the current search text in the find panel.
    /// </summary>
    public string CurrentSearchText
    {
        get => _currentSearchText;
        set => SetProperty(ref _currentSearchText, value);
    }

    /// <summary>
    /// Gets or sets the last code that was sent to the preview pane.
    /// </summary>
    public string LastPreviewedCode
    {
        get => _lastPreviewedCode;
        set => SetProperty(ref _lastPreviewedCode, value);
    }

    /// <summary>
    /// Gets or sets whether the WebView2 control is initialized and ready.
    /// </summary>
    public bool IsWebViewReady
    {
        get => _isWebViewReady;
        set => SetProperty(ref _isWebViewReady, value);
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the property value and raises PropertyChanged if the value changed.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    #endregion
}
