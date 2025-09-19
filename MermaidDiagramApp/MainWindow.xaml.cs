using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT;
using System.Runtime.InteropServices;
using Svg.Skia;
using SkiaSharp;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using TextControlBoxNS;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.Windows.AppLifecycle;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MermaidDiagramApp
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private DispatcherTimer? _timer;
        private bool _isWebViewReady = false;
        private string _lastPreviewedCode = "";
        private bool _isFullScreen = false;
        private bool _isPresentationMode = false;
        private bool _isPanModeEnabled = false;
        private bool _isBuilderVisible = false;
        private MermaidLinter _linter;
        private Version? _mermaidVersion;

        public DiagramBuilderViewModel BuilderViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            BuilderViewModel = new DiagramBuilderViewModel();
            BuilderPanel.DataContext = BuilderViewModel;
            BuilderViewModel.PropertyChanged += DiagramBuilderViewModel_PropertyChanged;

            _linter = new MermaidLinter();

            CodeEditor.EnableSyntaxHighlighting = true;
            CodeEditor.SelectSyntaxHighlightingById(TextControlBoxNS.SyntaxHighlightID.Markdown);
            (this.Content as FrameworkElement).Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            _ = CheckForMermaidUpdatesAsync();
            UpdateBuilderVisibility(); // Ensure builder is hidden on startup
            
            // Restore window state on startup
            _ = RestoreWindowStateAsync();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private async Task CheckForMermaidUpdatesAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder.Path;
                var versionFilePath = Path.Combine(localFolder, "mermaid-version.txt");

                if (!File.Exists(versionFilePath))
                {
                    return; // File not copied yet, skip check
                }

                var currentVersionStr = await File.ReadAllTextAsync(versionFilePath);
                string latestVersionStr;

                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync("https://registry.npmjs.org/mermaid");
                    using (var jsonDoc = JsonDocument.Parse(response))
                    {
                        latestVersionStr = jsonDoc.RootElement.GetProperty("dist-tags").GetProperty("latest").GetString() ?? string.Empty;
                    }
                }

                if (!string.IsNullOrEmpty(latestVersionStr))
                {
                    var currentVersion = new Version(currentVersionStr.Trim());
                    var latestVersion = new Version(latestVersionStr.Trim());

                    if (latestVersion > currentVersion)
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            UpdateInfoBar.Message = $"A new version of Mermaid.js ({latestVersionStr}) is available. You are using an older version.";
                            UpdateInfoBar.IsOpen = true;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check for Mermaid.js updates: {ex.Message}");
            }
        }

        private async Task InitializeWebViewAsync()
        {
            await CopyAssetsToLocalFolder();
            await PreviewBrowser.EnsureCoreWebView2Async();
            PreviewBrowser.NavigationCompleted += PreviewBrowser_NavigationCompleted;

            try
            {
                var localFolderPath = ApplicationData.Current.LocalFolder.Path;
                PreviewBrowser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "mermaid.local", localFolderPath, CoreWebView2HostResourceAccessKind.Allow);
                // Add a cache-busting query string to ensure the latest version of the host file is always loaded.
                var navigationUrl = $"https://mermaid.local/MermaidHost.html?t={DateTime.Now.Ticks}";
                PreviewBrowser.CoreWebView2.Navigate(navigationUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load MermaidHost.html: {ex.Message}");
            }
        }

        private async Task CopyAssetsToLocalFolder()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var packagePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;

            string[] assetsToCopy = { "MermaidHost.html", "mermaid.min.js", "mermaid-version.txt" };

            foreach (var assetName in assetsToCopy)
            {
                var sourcePath = Path.Combine(packagePath, "Assets", assetName);
                var destPath = Path.Combine(localFolder.Path, assetName);

                // Always overwrite the destination file to ensure the latest version is used during development.
                await Task.Run(() => File.Copy(sourcePath, destPath, true));
            }
        }

        private void PreviewBrowser_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation completed. IsSuccess: {args.IsSuccess}, WebErrorStatus: {args.WebErrorStatus}");
            if (args.IsSuccess)
            {
                // Get the Mermaid.js version once the page is loaded
                _ = Task.Run(async () =>
                {
                    var versionJson = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("mermaid.version()");
                    var versionString = JsonSerializer.Deserialize<string>(versionJson);
                    if (Version.TryParse(versionString, out var version))
                    {
                        _mermaidVersion = version;
                    }
                });

                _isWebViewReady = true;
                _ = UpdatePreview(); // Initial render

                // Start timer only after the page is loaded
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
        }

        private async void Timer_Tick(object? sender, object e)
        {
            var currentCode = CodeEditor.Text;
            if (currentCode != _lastPreviewedCode)
            {
                CheckForSyntaxIssues(currentCode);

                // BuilderViewModel.ParseMermaidCode(currentCode); // Commented out to disable buggy visual builder logic
                await UpdatePreview();
            }
        }

        private void CheckForSyntaxIssues(string code)
        {
            if (_mermaidVersion == null) return;

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

        private async Task UpdatePreview()
        {
            if (!_isWebViewReady || PreviewBrowser?.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("Preview not ready or CoreWebView2 is null.");
                return;
            }

            var code = CodeEditor.Text;
            _lastPreviewedCode = code; // Cache the code that is being sent to the preview
            System.Diagnostics.Debug.WriteLine($"Updating preview with code: {code}");
            var escapedCode = System.Text.Json.JsonSerializer.Serialize(code);
            await PreviewBrowser.CoreWebView2.ExecuteScriptAsync($"renderDiagram({escapedCode})");
        }

        private void NewClassDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "classDiagram\n    class Animal {\n        +String name\n        +int age\n        +void eat()\n    }";
            _ = UpdatePreview();
        }

        private void NewSequenceDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "sequenceDiagram\n    participant Alice\n    participant Bob\n    Alice->>Bob: Hello Bob, how are you?\n    Bob-->>Alice: I am good, thanks!";
            _ = UpdatePreview();
        }

        private void NewStateDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "stateDiagram-v2\n    [*] --> Still\n    Still --> [*]\n    Still --> Moving\n    Moving --> Still\n    Moving --> Crash\n    Crash --> [*]";
            _ = UpdatePreview();
        }

        private void NewActivityDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                activityDiagram
                    start
                    :Initial Action;
                    if (condition) then (yes)
                        :Success;
                    else (no)
                        :Failure;
                    endif
                    :Another Action;
                    stop
                """;
            _ = UpdatePreview();
        }

        private void NewFlowchart_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                graph TD
                    A[Start] --> B{Is it?};
                    B -->|Yes| C[OK];
                    C --> D[End];
                    B -->|No| E[Not OK];
                    E --> D[End];
                """;
            _ = UpdatePreview();
        }

        private void NewGanttChart_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                gantt
                    title A Gantt Diagram
                    dateFormat  YYYY-MM-DD
                    section Section
                    A task           :a1, 2024-01-01, 30d
                    Another task     :after a1  , 20d
                    section Another
                    Task in sec      :2024-01-12  , 12d
                    another task      : 24d
                """;
            _ = UpdatePreview();
        }

        private void NewPieChart_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                pie
                    title Key elements in Product X
                    "Calcium" : 42.96
                    "Potassium" : 50.05
                    "Magnesium" : 10.01
                    "Iron" :  5
                """;
            _ = UpdatePreview();
        }

        private void NewGitGraph_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                gitGraph
                   commit
                   commit
                   branch develop
                   checkout develop
                   commit
                   commit
                   checkout main
                   merge develop
                   commit
                   commit
                """;
            _ = UpdatePreview();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _timer?.Stop();
            
            // Save window state before closing
            var appWindow = GetAppWindowForCurrentWindow();
            WindowStateManager.SaveWindowState(appWindow);
        }

        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (_isFullScreen)
                {
                    ToggleFullScreen_Click(this, new RoutedEventArgs());
                }
                if (_isPresentationMode)
                {
                    PresentationMode_Click(this, new RoutedEventArgs());
                }
            }
        }

        private void PanTool_Click(object sender, RoutedEventArgs e)
        {
            _isPanModeEnabled = PanTool.IsChecked;
            UpdatePanMode();
        }

        private async void UpdatePanMode()
        {
            if (!_isWebViewReady || PreviewBrowser?.CoreWebView2 == null) return;

            if (_isPanModeEnabled)
            {
                await PreviewBrowser.CoreWebView2.ExecuteScriptAsync(@"
                    const body = document.body;
                    body.style.cursor = 'grab';

                    let isDown = false;
                    let startX, startY, scrollLeft, scrollTop;

                    const mouseDownHandler = (e) => {
                        isDown = true;
                        body.style.cursor = 'grabbing';
                        startX = e.pageX;
                        startY = e.pageY;
                        scrollLeft = window.scrollX;
                        scrollTop = window.scrollY;
                    };

                    const mouseLeaveHandler = () => {
                        isDown = false;
                        body.style.cursor = 'grab';
                    };

                    const mouseUpHandler = () => {
                        isDown = false;
                        body.style.cursor = 'grab';
                    };

                    const mouseMoveHandler = (e) => {
                        if (!isDown) return;
                        e.preventDefault();
                        const x = e.pageX;
                        const y = e.pageY;
                        const walkX = x - startX;
                        const walkY = y - startY;
                        window.scrollTo(scrollLeft - walkX, scrollTop - walkY);
                    };

                    window.panHandlers = { mouseDownHandler, mouseLeaveHandler, mouseUpHandler, mouseMoveHandler };
                    document.addEventListener('mousedown', mouseDownHandler, true);
                    document.addEventListener('mouseleave', mouseLeaveHandler, true);
                    document.addEventListener('mouseup', mouseUpHandler, true);
                    document.addEventListener('mousemove', mouseMoveHandler, true);
                ");
            }
            else
            {
                await PreviewBrowser.CoreWebView2.ExecuteScriptAsync(@"
                    document.body.style.cursor = 'default';
                    if (window.panHandlers) {
                        document.removeEventListener('mousedown', window.panHandlers.mouseDownHandler, true);
                        document.removeEventListener('mouseleave', window.panHandlers.mouseLeaveHandler, true);
                        document.removeEventListener('mouseup', window.panHandlers.mouseUpHandler, true);
                        document.removeEventListener('mousemove', window.panHandlers.mouseMoveHandler, true);
                        window.panHandlers = null;
                    }
                ");
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
                System.Diagnostics.Debug.WriteLine($"Failed to restore window state: {ex.Message}");
            }
        }

        private void PresentationMode_Click(object sender, RoutedEventArgs e)
        {
            var appWindow = GetAppWindowForCurrentWindow();

            if (_isPresentationMode)
            {
                appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                MainMenuBar.Visibility = Visibility.Visible;
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorPreviewSplitter.Visibility = Visibility.Visible;
                UpdateBuilderVisibility(); // Restore builder visibility based on its state
            }
            else
            {
                appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                MainMenuBar.Visibility = Visibility.Collapsed;
                EditorColumn.Width = new GridLength(0);
                EditorPreviewSplitter.Visibility = Visibility.Collapsed;
                BuilderColumn.Width = new GridLength(0);
                BuilderSplitter.Visibility = Visibility.Collapsed;
            }
            _isPresentationMode = !_isPresentationMode;
        }

        private void ToggleFullScreen_Click(object sender, RoutedEventArgs e)
        {
            _isFullScreen = !_isFullScreen;
            if (_isFullScreen)
            {
                MainMenuBar.Visibility = Visibility.Collapsed;
                EditorColumn.Width = new GridLength(0);
                EditorPreviewSplitter.Visibility = Visibility.Collapsed;
                BuilderColumn.Width = new GridLength(0);
                BuilderSplitter.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainMenuBar.Visibility = Visibility.Visible;
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorPreviewSplitter.Visibility = Visibility.Visible;
                UpdateBuilderVisibility(); // Restore builder visibility based on its state
            }
        }

        private void BuilderTool_Click(object sender, RoutedEventArgs e)
        {
            _isBuilderVisible = BuilderTool.IsChecked;
            UpdateBuilderVisibility();
        }

        private void UpdateBuilderVisibility()
        {
            if (_isBuilderVisible)
            {
                BuilderColumn.Width = new GridLength(300, GridUnitType.Pixel); // Or use GridLength.Auto
                BuilderSplitter.Visibility = Visibility.Visible;
                BuilderPanel.Visibility = Visibility.Visible;
            }
            else
            {
                BuilderColumn.Width = new GridLength(0);
                BuilderSplitter.Visibility = Visibility.Collapsed;
                BuilderPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var package = Windows.ApplicationModel.Package.Current;
            var version = package.Id.Version;
            var versionString = $"Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var installDate = package.InstalledDate;
            var installDateString = $"Installed: {installDate.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

            VersionTextBlock.Text = versionString;
            InstallDateTextBlock.Text = installDateString;

            AboutDialog.XamlRoot = this.Content.XamlRoot;
            await AboutDialog.ShowAsync();
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            WinRT_InterOp.InitializeWithWindow(openPicker, this);

            openPicker.FileTypeFilter.Add(".mmd");
            openPicker.FileTypeFilter.Add(".md");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                CodeEditor.Text = await FileIO.ReadTextAsync(file);
                await UpdatePreview();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Mermaid Diagram", new List<string>() { ".mmd" });
            savePicker.SuggestedFileName = "NewDiagram";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, CodeEditor.Text);
            }
        }

        private async void ExportSvg_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewBrowser?.CoreWebView2 == null) return;

            var svg = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("getSvg()");
            var unescapedSvg = System.Text.Json.JsonSerializer.Deserialize<string>(svg);

            if (string.IsNullOrEmpty(unescapedSvg)) return;

            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("SVG Image", new List<string>() { ".svg" });
            savePicker.SuggestedFileName = "Diagram";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, unescapedSvg);
            }
        }

        private async void ExportPng_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewBrowser?.CoreWebView2 == null) return;

            var svgJson = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("getSvg()");
            var svgString = System.Text.Json.JsonSerializer.Deserialize<string>(svgJson);

            if (string.IsNullOrEmpty(svgString)) return;

            PngExportDialog.XamlRoot = this.Content.XamlRoot;
            var result = await PngExportDialog.ShowAsync();

            if (result != ContentDialogResult.Primary) return;

            var scale = (float)ScaleNumberBox.Value;

            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("PNG Image", new List<string>() { ".png" });
            savePicker.SuggestedFileName = "Diagram";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                using var svg = new SKSvg();
                using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgString));
                if (svg.Load(svgStream) is { } picture)
                {
                    var dimensions = new SKSizeI((int)(picture.CullRect.Width * scale), (int)(picture.CullRect.Height * scale));
                    using var bitmap = new SKBitmap(new SKImageInfo(dimensions.Width, dimensions.Height));
                    using var canvas = new SKCanvas(bitmap);
                    canvas.Clear(SKColor.Parse("#222222")); // Dark background to match theme
                    var matrix = SKMatrix.CreateScale(scale, scale);
                    canvas.DrawPicture(picture, ref matrix);
                    canvas.Flush();

                    using var fileStream = await file.OpenStreamForWriteAsync();
                    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                    data.SaveTo(fileStream);
                }
            }
        }

        private void DiagramBuilderViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Commented out to disable buggy visual builder logic that overwrites user's code.
            /*
            if (e.PropertyName == nameof(DiagramBuilderViewModel.GeneratedMermaidCode))
            {
                if (CodeEditor.Text != BuilderViewModel.GeneratedMermaidCode)
                {
                    CodeEditor.Text = BuilderViewModel.GeneratedMermaidCode;
                }
            }
            */
        }

        private async void UpdateMermaid_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateInfoBar.ActionButton is Button updateButton)
            {
                updateButton.IsEnabled = false;
            }
            UpdateInfoBar.Message = "Updating Mermaid.js...";
            UpdateInfoBar.Severity = InfoBarSeverity.Informational;

            try
            {
                string latestVersionStr;
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync("https://registry.npmjs.org/mermaid");
                    using (var jsonDoc = JsonDocument.Parse(response))
                    {
                        latestVersionStr = jsonDoc.RootElement.GetProperty("dist-tags").GetProperty("latest").GetString() ?? string.Empty;
                    }
                }

                if (string.IsNullOrEmpty(latestVersionStr))
                {
                    UpdateInfoBar.Message = "Could not determine the latest version.";
                    UpdateInfoBar.Severity = InfoBarSeverity.Error;
                    return;
                }

                var downloadUrl = $"https://cdn.jsdelivr.net/npm/mermaid@" + latestVersionStr + "/dist/mermaid.min.js";
                using (var client = new HttpClient())
                {
                    var newMermaidJsContent = await client.GetStringAsync(downloadUrl);

                    var localFolder = ApplicationData.Current.LocalFolder.Path;
                    var localFilePath = Path.Combine(localFolder, "mermaid.min.js");
                    await File.WriteAllTextAsync(localFilePath, newMermaidJsContent);

                    var versionFilePath = Path.Combine(localFolder, "mermaid-version.txt");
                    await File.WriteAllTextAsync(versionFilePath, latestVersionStr);
                }

                UpdateInfoBar.IsOpen = false;
                RestartDialog.XamlRoot = this.Content.XamlRoot;
                var result = await RestartDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Restart the application
                    AppInstance.Restart("");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update Mermaid.js: {ex.Message}");
                UpdateInfoBar.Message = $"Update failed: {ex.Message}";
                UpdateInfoBar.Severity = InfoBarSeverity.Error;
                if (UpdateInfoBar.ActionButton is Button button)
                {
                    button.IsEnabled = true;
                }
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
