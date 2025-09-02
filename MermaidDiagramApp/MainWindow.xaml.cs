using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            await PreviewBrowser.EnsureCoreWebView2Async();
            PreviewBrowser.CoreWebView2.Navigate("ms-appx-web:///Assets/MermaidHost.html");

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private async void Timer_Tick(object? sender, object e)
        {
            if (PreviewBrowser?.CoreWebView2 == null) return;

            var code = CodeEditor.Text;
            var escapedCode = System.Text.Json.JsonSerializer.Serialize(code);
            await PreviewBrowser.CoreWebView2.ExecuteScriptAsync($"renderDiagram({escapedCode})");
        }

        private void NewClassDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "classDiagram\n    class Animal {\n        +String name\n        +int age\n        +void eat()\n    }";
        }

        private void NewSequenceDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "sequenceDiagram\n    participant Alice\n    participant Bob\n    Alice->>Bob: Hello Bob, how are you?\n    Bob-->>Alice: I am good, thanks!";
        }

        private void NewStateDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "stateDiagram-v2\n    [*] --> Still\n    Still --> [*]\n    Still --> Moving\n    Moving --> Still\n    Moving --> Crash\n    Crash --> [*]";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            var window = this;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.FileTypeFilter.Add(".mmd");
            openPicker.FileTypeFilter.Add(".md");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                CodeEditor.Text = await FileIO.ReadTextAsync(file);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            var window = this;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

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
            var window = this;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

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

            var savePicker = new FileSavePicker();
            var window = this;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("PNG Image", new List<string>() { ".png" });
            savePicker.SuggestedFileName = "Diagram";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                using var svg = new SKSvg();
                svg.Load(svgString);

                using var stream = await file.OpenStreamForWriteAsync();
                svg.Save(stream, SKColors.White, SKEncodedImageFormat.Png, 100);
            }
        }
    }
}
