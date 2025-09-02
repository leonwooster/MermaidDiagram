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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using TextControlBoxNS;

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
        private bool _isPanModeEnabled = false;

        public MainWindow()
        {
            this.InitializeComponent();
            InitializeWebView();
            this.Closed += MainWindow_Closed;
        }

        private async void InitializeWebView()
        {
            await PreviewBrowser.EnsureCoreWebView2Async();
            PreviewBrowser.NavigationCompleted += PreviewBrowser_NavigationCompleted;
            try
            {
                var packagePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                string htmlPath = Path.Combine(packagePath, "Assets", "MermaidHost.html");
                string htmlContent = await File.ReadAllTextAsync(htmlPath);
                PreviewBrowser.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load MermaidHost.html: {ex.Message}");
            }
        }

        private void PreviewBrowser_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation completed. IsSuccess: {args.IsSuccess}, WebErrorStatus: {args.WebErrorStatus}");
            if (args.IsSuccess)
            {
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
                await UpdatePreview();
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
        }

        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape && _isFullScreen)
            {
                ToggleFullScreen_Click(this, new RoutedEventArgs());
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

        private void ToggleFullScreen_Click(object sender, RoutedEventArgs e)
        {
            _isFullScreen = !_isFullScreen;
            if (_isFullScreen)
            {
                MainMenuBar.Visibility = Visibility.Collapsed;
                EditorColumn.Width = new GridLength(0);
                GridSplitter.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainMenuBar.Visibility = Visibility.Visible;
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                GridSplitter.Visibility = Visibility.Visible;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
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
                    canvas.Clear(SKColors.White);
                    var matrix = SKMatrix.CreateScale(scale, scale);
                    canvas.DrawPicture(picture, ref matrix);
                    canvas.Flush();

                    using var fileStream = await file.OpenStreamForWriteAsync();
                    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                    data.SaveTo(fileStream);
                }
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
