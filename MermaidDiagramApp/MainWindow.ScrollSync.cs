// MainWindow.ScrollSync.cs
// Partial class for MainWindow containing synchronized scrolling initialization,
// CodeEditor click handler for scroll sync, and preview-to-line synchronization logic.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        #region Synchronized Scrolling

        private MermaidCodeParser _codeParser = new MermaidCodeParser();
        private List<MermaidCodeParser.MermaidElement> _currentElements = new List<MermaidCodeParser.MermaidElement>();

        private void InitializeSynchronizedScrolling()
        {
            try
            {
                _logger.LogInformation("Initializing synchronized scrolling...");
                
                // Hook into the code editor's mouse click event
                CodeEditor.PointerPressed += CodeEditor_PointerPressed;
                
                _logger.LogInformation("Synchronized scrolling initialized (click-based) - Event handler attached");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize synchronized scrolling: {ex.Message}", ex);
            }
        }

        private async void CodeEditor_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("CodeEditor clicked!");
                
                // Small delay to ensure cursor position is updated
                await Task.Delay(50);
                
                // Get current caret line from TextControlBox
                int currentLine = CodeEditor.CurrentLineIndex;
                
                _logger.LogInformation($"Current line index: {currentLine}");
                _logger.LogInformation($"Elements count: {_currentElements.Count}");
                _logger.LogInformation($"WebView ready: {_isWebViewReady}");
                
                if (currentLine >= 0)
                {
                    _logger.LogInformation($"Clicked on line {currentLine}, syncing...");
                    await SyncPreviewToLine(currentLine);
                }
                else
                {
                    _logger.LogWarning("Current line index is negative");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Click handler error: {ex.Message}", ex);
            }
        }

        private async Task SyncPreviewToLine(int lineIndex)
        {
            if (PreviewBrowser?.CoreWebView2 == null || !_isWebViewReady)
            {
                _logger.LogDebug("SyncPreviewToLine: WebView not ready");
                return;
            }

            if (_currentElements.Count == 0)
            {
                _logger.LogDebug("SyncPreviewToLine: No elements available");
                return;
            }

            try
            {
                // Convert 0-based index to 1-based line number
                int lineNumber = lineIndex + 1;
                
                // Find the element at or before this line
                var element = _currentElements.FindLast(e => e.LineNumber <= lineNumber);
                
                if (element != null)
                {
                    _logger.LogDebug($"Syncing to element '{element.Id}' at line {element.LineNumber}");
                    
                    // Scroll to the element in the preview (works for both SVG and HTML)
                    string script = $@"
                        (function() {{
                            try {{
                                // Check if we're in Markdown or Mermaid mode
                                const container = document.getElementById('content-container');
                                const isMermaidMode = container && container.classList.contains('mermaid-mode');
                                const isMarkdownMode = container && container.classList.contains('markdown-mode');
                                
                                let element = null;
                                
                                if (isMermaidMode) {{
                                    // Look for SVG elements
                                    const svg = document.querySelector('svg');
                                    if (!svg) return 'no svg';
                                    element = svg.querySelector('[data-line=""{element.LineNumber}""]');
                                }} else if (isMarkdownMode) {{
                                    // Look for HTML elements
                                    const markdownBody = document.querySelector('.markdown-body');
                                    if (!markdownBody) return 'no markdown body';
                                    element = markdownBody.querySelector('[data-line=""{element.LineNumber}""]');
                                }}
                                
                                if (!element) return 'element not found';
                                
                                const rect = element.getBoundingClientRect();
                                const scrollTop = window.pageYOffset + rect.top - (window.innerHeight / 3);
                                
                                window.scrollTo({{
                                    top: Math.max(0, scrollTop),
                                    behavior: 'smooth'
                                }});
                                
                                // Highlight the element
                                document.querySelectorAll('[data-highlighted]').forEach(el => {{
                                    el.removeAttribute('data-highlighted');
                                    el.style.outline = '';
                                    el.style.backgroundColor = '';
                                }});
                                
                                element.setAttribute('data-highlighted', 'true');
                                element.style.outline = '2px solid #58a6ff';
                                element.style.outlineOffset = '2px';
                                
                                // For HTML elements, also add a subtle background
                                if (isMarkdownMode) {{
                                    element.style.backgroundColor = 'rgba(88, 166, 255, 0.1)';
                                }}
                                
                                return 'success';
                            }} catch (e) {{
                                return 'error: ' + e.message;
                            }}
                        }})();
                    ";
                    
                    var result = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync(script);
                    _logger.LogDebug($"Scroll result: {result}");
                }
                else
                {
                    _logger.LogDebug($"No element found for line {lineNumber}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SyncPreviewToLine error: {ex.Message}", ex);
            }
        }

        private async Task SetupScrollSynchronization()
        {
            if (PreviewBrowser?.CoreWebView2 == null)
                return;

            try
            {
                // Parse current code to extract elements
                var code = CodeEditor.Text;
                bool isMarkdown = _currentContentType == ContentType.Markdown || _currentContentType == ContentType.MarkdownWithMermaid;
                _currentElements = _codeParser.ParseCode(code, isMarkdown);
                
                _logger.LogInformation($"Parsed {_currentElements.Count} elements for scroll sync");
                
                // Wait for content to be rendered
                await Task.Delay(500);
                
                // Inject line number markers into rendered content (SVG or HTML)
                if (_currentElements.Count > 0)
                {
                    var injectionScript = _codeParser.GenerateLineNumberInjectionScript(_currentElements);
                    await PreviewBrowser.CoreWebView2.ExecuteScriptAsync(injectionScript);
                    _logger.LogInformation("Line markers injected into rendered content");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SetupScrollSynchronization error: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
