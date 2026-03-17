// MainWindow.RenderMode.cs
// Partial class for MainWindow containing render mode override handlers,
// zoom controls, pan/drag mode, and content type indicator updates.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Services.Rendering;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        #region Render Mode Indicators

        private void UpdateRenderModeIndicator(ContentType contentType)
        {
            _currentContentType = contentType;
            
            switch (contentType)
            {
                case ContentType.Mermaid:
                    RenderModeText.Text = "Mermaid Diagram";
                    RenderModeIcon.Glyph = "\uE8A5";
                    break;
                case ContentType.Markdown:
                    RenderModeText.Text = "Markdown Document";
                    RenderModeIcon.Glyph = "\uE8A5";
                    break;
                case ContentType.MarkdownWithMermaid:
                    RenderModeText.Text = "Hybrid Document";
                    RenderModeIcon.Glyph = "\uE8FD";
                    break;
                default:
                    RenderModeText.Text = "Unknown";
                    RenderModeIcon.Glyph = "\uE897";
                    break;
            }
            
            UpdateContentTypeIndicator(contentType);
        }
        
        private void UpdateContentTypeIndicator(ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Mermaid:
                    RenderModeText.Text = "Mermaid";
                    RenderModeIcon.Glyph = "\uE8BC";
                    break;
                case ContentType.Markdown:
                    RenderModeText.Text = "Markdown";
                    RenderModeIcon.Glyph = "\uE8A5";
                    break;
                case ContentType.MarkdownWithMermaid:
                    RenderModeText.Text = "Hybrid";
                    RenderModeIcon.Glyph = "\uE8FD";
                    break;
                default:
                    RenderModeText.Text = "Unknown";
                    RenderModeIcon.Glyph = "\uE9CE";
                    break;
            }
        }

        #endregion

        #region Render Mode Overrides

        private void RenderModeOverride_Click(object sender, RoutedEventArgs e)
        {
            // Flyout will open automatically
        }

        private async void AutoDetectMode_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Switching to auto-detect mode");
            _lastPreviewedCode = null;
            await UpdatePreview();
        }

        private async void ForceMermaidMode_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Forcing Mermaid rendering mode");
            
            var code = CodeEditor.Text;
            var context = new RenderingContext
            {
                FileExtension = "mmd",
                FilePath = _currentFilePath,
                ForcedContentType = ContentType.Mermaid,
                EnableMermaidInMarkdown = false,
                Theme = ThemeMode.Dark
            };

            await ExecuteRenderingScript(code, ContentType.Mermaid, context);
            UpdateRenderModeIndicator(ContentType.Mermaid);
        }

        private async void ForceMarkdownMode_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Forcing Markdown rendering mode");
            
            var code = CodeEditor.Text;
            var context = new RenderingContext
            {
                FileExtension = "md",
                FilePath = _currentFilePath,
                ForcedContentType = ContentType.Markdown,
                EnableMermaidInMarkdown = true,
                Theme = ThemeMode.Dark
            };

            await ExecuteRenderingScript(code, ContentType.Markdown, context);
            UpdateRenderModeIndicator(ContentType.Markdown);
        }

        #endregion

        #region Zoom and Pan Controls

        private void PreviewBrowser_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (_isFullScreen)
                {
                    ToggleFullScreen_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (_isPresentationMode)
                {
                    PresentationMode_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            else if (e.Key == Windows.System.VirtualKey.F11)
            {
                ToggleFullScreen_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void DragModeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isPanModeEnabled = DragModeToggle.IsChecked ?? false;
            UpdatePanMode();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            // Handle zoom in
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            // Handle zoom out
        }

        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            // Handle zoom reset
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

        #endregion
    }
}
