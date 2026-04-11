// MainWindow.Clipboard.cs
// Partial class for MainWindow containing the "Copy as Image" click handler,
// capture orchestration, and status bar auto-dismiss timer.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        private DispatcherTimer? _clipboardStatusTimer;
        private CopyAsImageOrchestrator? _copyOrchestrator;

        private CopyAsImageOrchestrator CopyOrchestrator =>
            _copyOrchestrator ??= new CopyAsImageOrchestrator(_clipboardService, _logger);

        private async void CopyAsImage_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCopyAsImageAsync();
        }

        private async Task ExecuteCopyAsImageAsync()
        {
            var message = await CopyOrchestrator.ExecuteAsync(
                PreviewBrowser?.CoreWebView2 != null,
                CodeEditor.Text,
                CapturePngAsync);

            ShowClipboardStatus(message);
        }

        private async Task<byte[]> CapturePngAsync()
        {
            // For Mermaid diagrams, extract the SVG and rasterize it to get a clean diagram image
            // (not a viewport screenshot). Fall back to CapturePreviewAsync for Markdown content
            // or if SVG extraction fails.
            if (_currentContentType == Models.ContentType.Mermaid)
            {
                try
                {
                    var pngData = await CapturePngViaSvgAsync();
                    if (pngData.Length > 0)
                        return pngData;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"SVG extraction failed, falling back to viewport capture: {ex.Message}");
                }
            }

            // Viewport capture for Markdown content or as fallback
            try
            {
                using var stream = new MemoryStream();
                await PreviewBrowser.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png,
                    stream.AsRandomAccessStream());
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"CapturePreviewAsync failed, falling back to SVG: {ex.Message}");
                return await CapturePngViaSvgAsync();
            }
        }

        /// <summary>
        /// Copies a specific diagram's SVG content as a PNG image to the clipboard.
        /// The SVG is re-rendered with light theme by the JS side, then rasterized
        /// here with a white background for clean pasting into documents.
        /// </summary>
        private async Task CopyDiagramSvgAsImageAsync(string svgContent)
        {
            try
            {
                var pngData = await RasterizeSvgWithWhiteBackgroundAsync(svgContent);
                if (pngData.Length == 0)
                {
                    ShowClipboardStatus("Failed to copy image to clipboard");
                    return;
                }

                var success = await _clipboardService.CopyPngToClipboardAsync(pngData);
                ShowClipboardStatus(success
                    ? "Image copied to clipboard"
                    : "Failed to copy image to clipboard");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Copy diagram as image failed: {ex.Message}", ex);
                ShowClipboardStatus("Failed to copy image to clipboard");
            }
        }

        /// <summary>
        /// Rasterizes SVG to PNG with a white background, suitable for clipboard/paste.
        /// </summary>
        private Task<byte[]> RasterizeSvgWithWhiteBackgroundAsync(string svgContent, float scale = 2.0f)
        {
            if (string.IsNullOrEmpty(svgContent))
                return Task.FromResult(Array.Empty<byte>());

            return Task.Run(() =>
            {
                try
                {
                    // Add white background rect to the SVG
                    var svgWithBg = AddWhiteBackgroundToSvg(svgContent);

                    using var svg = new Svg.Skia.SKSvg();
                    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgWithBg));

                    if (svg.Load(stream) is { } picture)
                    {
                        var width = (int)(picture.CullRect.Width * scale);
                        var height = (int)(picture.CullRect.Height * scale);

                        using var bitmap = new SkiaSharp.SKBitmap(new SkiaSharp.SKImageInfo(width, height));
                        using var canvas = new SkiaSharp.SKCanvas(bitmap);
                        canvas.Clear(SkiaSharp.SKColors.White);

                        var matrix = SkiaSharp.SKMatrix.CreateScale(scale, scale);
                        canvas.DrawPicture(picture, ref matrix);
                        canvas.Flush();

                        using var data = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        return data.ToArray();
                    }

                    return Array.Empty<byte>();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to rasterize SVG with white background: {ex.Message}", ex);
                    return Array.Empty<byte>();
                }
            });
        }

        private static string AddWhiteBackgroundToSvg(string svgContent)
        {
            var tagEnd = svgContent.IndexOf('>');
            if (tagEnd <= 0) return svgContent;

            var widthMatch = System.Text.RegularExpressions.Regex.Match(svgContent, @"width=""([^""]+)""");
            var heightMatch = System.Text.RegularExpressions.Regex.Match(svgContent, @"height=""([^""]+)""");

            var w = widthMatch.Success ? widthMatch.Groups[1].Value.Replace("px", "") : "1200";
            var h = heightMatch.Success ? heightMatch.Groups[1].Value.Replace("px", "") : "800";

            var rect = $"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"#ffffff\"/>";
            return svgContent.Insert(tagEnd + 1, rect);
        }

        private async Task<byte[]> CapturePngViaSvgAsync()
        {
            var svgJson = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("getSvg()");
            var svgString = System.Text.Json.JsonSerializer.Deserialize<string>(svgJson);
            if (string.IsNullOrEmpty(svgString))
                return Array.Empty<byte>();

            return await RasterizeSvgWithWhiteBackgroundAsync(svgString);
        }

        private void ShowClipboardStatus(string message)
        {
            _clipboardStatusTimer?.Stop();

            var previousText = RenderModeText.Text;
            RenderModeText.Text = message;

            _clipboardStatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _clipboardStatusTimer.Tick += (s, e) =>
            {
                _clipboardStatusTimer.Stop();
                RenderModeText.Text = previousText;
            };
            _clipboardStatusTimer.Start();
        }
    }
}
