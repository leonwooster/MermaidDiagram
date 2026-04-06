// MainWindow.xaml.cs
// Core partial class file for MainWindow. Contains constructor, field/property declarations,
// initialization orchestration (MainWindow_Loaded), window lifecycle (MainWindow_Closed),
// window state restoration, syntax issue checking, and WinRT interop helpers.
//
// Partial class files by concern:
//   MainWindow.WebView.cs       — WebView2 initialization, message handling, rendering
//   MainWindow.UI.cs            — New diagram templates, fullscreen/presentation, menu/toolbar handlers
//   MainWindow.FileOps.cs       — File open/save/close, recent files, dialog utilities
//   MainWindow.Export.cs        — SVG/PNG export, Mermaid.js update management, about/log handlers
//   MainWindow.RenderMode.cs    — Render mode overrides, zoom/pan controls, content type indicators
//   MainWindow.Builder.cs       — Visual builder panel visibility and canvas wiring
//   MainWindow.Search.cs        — Search panel UI wiring and CodeEditor search integration
//   MainWindow.ScrollSync.cs    — Synchronized scrolling initialization and scroll-to-line
//   MainWindow.MarkdownToWord.cs — Markdown-to-Word export dialogs and progress

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Services.Rendering;
using MermaidDiagramApp.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI;

namespace MermaidDiagramApp
{
    /// <summary>
    /// Main application window. Split across partial class files by concern.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private DispatcherTimer? _timer;
        private bool _isWebViewReady = false;
        private string? _lastPreviewedCode = "";
        private bool _isFullScreen = false;
        private bool _isPresentationMode = false;
        private bool _isPanModeEnabled = false;
        private bool _isBuilderVisible = false;
        private readonly MermaidLinter _linter;
        private Version? _mermaidVersion;
        private readonly ILogger _logger;
        
        // Rendering orchestration components
        private readonly RenderingOrchestrator _renderingOrchestrator;
        private readonly IContentTypeDetector _contentTypeDetector;
        private readonly ContentRendererFactory _rendererFactory;
        private ContentType _currentContentType = ContentType.Unknown;
        private string _currentFilePath = string.Empty;
        private readonly MarkdownStyleSettingsService _styleSettingsService;
        private readonly DiagramFileService _diagramFileService;
        private readonly RecentFilesService _recentFilesService;
        private readonly IFileOperationsService _fileOperationsService;
        private readonly ISearchService _searchService;
        private readonly IMermaidUpdateService _mermaidUpdateService;
        private readonly IExportService _exportService;
        private FileSystemWatcher? _fileWatcher;
        private DateTime _lastFileChangeTime = DateTime.MinValue;

        /// <summary>
        /// Gets the MainWindowViewModel for data binding and command routing.
        /// WinUI 3 Window doesn't have DataContext, so we expose it as a property.
        /// </summary>
        public MainWindowViewModel ViewModel { get; }

        public DiagramBuilderViewModel BuilderViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            RenderingOrchestrator renderingOrchestrator,
            IContentTypeDetector contentTypeDetector,
            ContentRendererFactory rendererFactory,
            DiagramFileService diagramFileService,
            RecentFilesService recentFilesService,
            MarkdownStyleSettingsService styleSettingsService,
            MermaidLinter linter,
            ILogger logger,
            IFileOperationsService fileOperationsService,
            ISearchService searchService,
            IMermaidUpdateService mermaidUpdateService,
            IExportService exportService)
        {
            this.InitializeComponent();

            // Store ViewModel
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // Store injected services
            _renderingOrchestrator = renderingOrchestrator ?? throw new ArgumentNullException(nameof(renderingOrchestrator));
            _contentTypeDetector = contentTypeDetector ?? throw new ArgumentNullException(nameof(contentTypeDetector));
            _rendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
            _diagramFileService = diagramFileService ?? throw new ArgumentNullException(nameof(diagramFileService));
            _recentFilesService = recentFilesService ?? throw new ArgumentNullException(nameof(recentFilesService));
            _styleSettingsService = styleSettingsService ?? throw new ArgumentNullException(nameof(styleSettingsService));
            _linter = linter ?? throw new ArgumentNullException(nameof(linter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileOperationsService = fileOperationsService ?? throw new ArgumentNullException(nameof(fileOperationsService));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _mermaidUpdateService = mermaidUpdateService ?? throw new ArgumentNullException(nameof(mermaidUpdateService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            // Wire ViewModel callback delegates to MainWindow UI methods
            ViewModel.RequestNewDiagram = diagramType => NewDiagram(diagramType);
            ViewModel.RequestOpenFile = () => Open_Click(this, new RoutedEventArgs());
            ViewModel.RequestSaveFile = () => Save_Click(this, new RoutedEventArgs());
            ViewModel.RequestExportSvg = () => ExportSvg_Click(this, new RoutedEventArgs());
            ViewModel.RequestExportPng = () => ExportPng_Click(this, new RoutedEventArgs());
            ViewModel.RequestFind = () => Find_Click(this, new RoutedEventArgs());
            ViewModel.RequestCheckSyntax = () => CheckSyntax_Click(this, new RoutedEventArgs());
            ViewModel.RequestExit = () => Exit_Click(this, new RoutedEventArgs());

            BuilderViewModel = new DiagramBuilderViewModel();
            BuilderPanel.DataContext = BuilderViewModel;
            BuilderViewModel.PropertyChanged += DiagramBuilderViewModel_PropertyChanged;

            // Subscribe to rendering state changes
            _renderingOrchestrator.RenderingStateChanged += OnRenderingStateChanged;

            CodeEditor.EnableSyntaxHighlighting = true;
            CodeEditor.SelectSyntaxHighlightingById(TextControlBoxNS.SyntaxHighlightID.Markdown);
            
            if (this.Content is FrameworkElement content)
            {
                content.Loaded += MainWindow_Loaded;
            }
            this.Closed += MainWindow_Closed;
            
            // Add keyboard event handler to PreviewBrowser to handle Escape key when WebView has focus
            PreviewBrowser.KeyDown += PreviewBrowser_KeyDown;
            
            _ = CheckForMermaidUpdatesAsync();
            UpdateBuilderVisibility(); // Ensure builder is hidden on startup
            
            // Restore window state on startup
            _ = RestoreWindowStateAsync();
            
            // Initialize synchronized scrolling
            InitializeSynchronizedScrolling();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
            
            // Populate recent files menu
            PopulateRecentFilesMenu();
            
            // Wire up visual builder components (defined in MainWindow.Builder.cs)
            InitializeBuilderWiring();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _timer?.Stop();

            // Stop file watcher
            StopFileWatcher();

            // Save window state before closing
            var appWindow = GetAppWindowForCurrentWindow();
            WindowStateManager.SaveWindowState(appWindow);
        }

        private void CheckForSyntaxIssues(string code)
        {
            if (_mermaidVersion == null) return;

            // Only lint pure Mermaid content, not Markdown
            if (_currentContentType == ContentType.Markdown || _currentContentType == ContentType.MarkdownWithMermaid)
            {
                LinterInfoBar.IsOpen = false;
                return;
            }

            var issues = _linter.Lint(code, _mermaidVersion);
            if (issues.Any())
            {
                var issue = issues.First(); // For now, just handle the first issue
                LinterInfoBar.Message = issue.Description;

                var fixButton = new Button { Content = "Fix it" };
                fixButton.Click += (s, args) =>
                {
                    CodeEditor.Text = issue.ProposeFix(CodeEditor.Text);
                    LinterInfoBar.IsOpen = false;
                };
                LinterInfoBar.ActionButton = fixButton;
                LinterInfoBar.IsOpen = true;
            }
            else
            {
                LinterInfoBar.IsOpen = false;
            }
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }

        private async Task RestoreWindowStateAsync()
        {
            try
            {
                var windowState = await WindowStateManager.LoadWindowStateAsync();
                if (windowState != null)
                {
                    var appWindow = GetAppWindowForCurrentWindow();
                    
                    // Restore position and size
                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32
                    {
                        X = windowState.X,
                        Y = windowState.Y,
                        Width = windowState.Width,
                        Height = windowState.Height
                    });
                    
                    // Restore maximized state
                    if (windowState.IsMaximized && appWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.Maximize();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to restore window state: {ex.Message}", ex);
            }
        }

        [ComImport, Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize([In] IntPtr hwnd);
        }

        public static class WinRT_InterOp
        {
            public static void InitializeWithWindow(object target, object window)
            {
                var window_hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var initializeWithWindow = target.As<IInitializeWithWindow>();
                initializeWithWindow.Initialize(window_hwnd);
            }

        }
    }
}
