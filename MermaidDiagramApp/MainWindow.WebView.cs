// MainWindow.WebView.cs
// Partial class for MainWindow containing WebView2 initialization, message handling,
// rendering script execution, zoom controls, and JavaScript interop code.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using Windows.Storage;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services.Rendering;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp
{
    /// <summary>
    /// Partial class for MainWindow containing WebView2 initialization, message handling,
    /// rendering, zoom controls, and JavaScript interop code.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private async Task InitializeWebViewAsync()
        {
            try
            {
                _isWebViewReady = false;

                var assetsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets");
                _logger.LogInformation($"Initializing WebView with assets from: {assetsPath}");

                // Ensure WebView2 is initialized
                var webView2Environment = await CoreWebView2Environment.CreateAsync();
                await PreviewBrowser.EnsureCoreWebView2Async(webView2Environment);

                var coreWebView2 = PreviewBrowser.CoreWebView2;

                // Enable WebView2 developer tools for debugging
                coreWebView2.Settings.AreDevToolsEnabled = true;
                coreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                coreWebView2.Settings.IsWebMessageEnabled = true;

                // Map the packaged Assets folder into a virtual host so scripts/css can be loaded
                const string virtualHost = "appassets";

                try
                {
                    coreWebView2.ClearVirtualHostNameToFolderMapping(virtualHost);
                }
                catch (Exception clearEx)
                {
                    _logger.LogDebug($"No existing host mapping to clear: {clearEx.Message}");
                }

                coreWebView2.SetVirtualHostNameToFolderMapping(
                    virtualHost,
                    assetsPath,
                    CoreWebView2HostResourceAccessKind.Allow);

                _logger.LogInformation($"Virtual host 'https://{virtualHost}/' mapped to {assetsPath}");

                // Prepare timer reference for use inside handlers
                DispatcherTimer? checkTimer = null;

                // Set up console/message handling
                coreWebView2.WebMessageReceived += (s, e) =>
                {
                    var message = e.TryGetWebMessageAsString();
                    _logger.LogDebug($"[WebView2 Message] {message}");

                    // Try to parse as JSON for structured messages
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(message);
                        var root = jsonDoc.RootElement;
                        
                        if (root.TryGetProperty("type", out var typeElement))
                        {
                            var messageType = typeElement.GetString();
                            
                            if (messageType == "ready")
                            {
                                _logger.LogInformation("Received ready message from UnifiedRenderer");
                                _isWebViewReady = true;
                                checkTimer?.Stop();
                                
                                // Initialize Markdown to Word export now that WebView2 is ready
                                InitializeMarkdownToWordExport();
                                
                                DispatcherQueue.TryEnqueue(async () =>
                                {
                                    try
                                    {
                                        _lastPreviewedCode = null;
                                        await UpdatePreview();
                                    }
                                    catch (Exception updateEx)
                                    {
                                        _logger.LogError($"Error updating preview after ready: {updateEx.Message}", updateEx);
                                    }
                                });
                            }
                            else if (messageType == "renderComplete")
                            {
                                if (root.TryGetProperty("mode", out var modeElement))
                                {
                                    _logger.LogInformation($"Render completed in {modeElement.GetString()} mode");
                                }
                            }
                            else if (messageType == "renderError")
                            {
                                if (root.TryGetProperty("error", out var errorElement))
                                {
                                    _logger.LogError($"Render error from WebView: {errorElement.GetString()}");
                                }
                            }
                            else if (messageType == "log")
                            {
                                if (root.TryGetProperty("message", out var msgElement))
                                {
                                    _logger.LogDebug($"[WebView Log] {msgElement.GetString()}");
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Not JSON, check for legacy messages
                        if (string.Equals(message, "MermaidReady", StringComparison.Ordinal))
                        {
                            _logger.LogInformation("Received legacy MermaidReady message");
                            _isWebViewReady = true;
                            checkTimer?.Stop();
                            
                            // Initialize Markdown to Word export now that WebView2 is ready
                            InitializeMarkdownToWordExport();
                            
                            DispatcherQueue.TryEnqueue(async () =>
                            {
                                try
                                {
                                    _lastPreviewedCode = null;
                                    await UpdatePreview();
                                }
                                catch (Exception updateEx)
                                {
                                    _logger.LogError($"Error updating preview: {updateEx.Message}", updateEx);
                                }
                            });
                        }
                    }
                };

                // Handle navigation errors
                PreviewBrowser.NavigationCompleted += (s, e) =>
                {
                    if (!e.IsSuccess)
                    {
                        _logger.LogWarning($"Navigation failed: {e.WebErrorStatus}");
                    }
                };

                // Set up keyboard event interception via JavaScript
                coreWebView2.WebMessageReceived += (s, e) =>
                {
                    var message = e.TryGetWebMessageAsString();
                    
                    // Handle keyboard shortcuts sent from JavaScript
                    if (message == "F11_PRESSED")
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            ToggleFullScreen_Click(this, new RoutedEventArgs());
                        });
                    }
                    else if (message == "ESCAPE_PRESSED")
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            if (_isFullScreen)
                            {
                                ToggleFullScreen_Click(this, new RoutedEventArgs());
                            }
                            else if (_isPresentationMode)
                            {
                                PresentationMode_Click(this, new RoutedEventArgs());
                            }
                        });
                    }
                };

                // Navigate to the packaged Unified Renderer page through the virtual host
                var hostPageUri = new Uri($"https://{virtualHost}/UnifiedRenderer.html");
                coreWebView2.Navigate(hostPageUri.ToString());
                _logger.LogInformation($"Navigating WebView to {hostPageUri}");

                // Set up a timer to check if renderers are loaded
                checkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
                int checkCount = 0;
                checkTimer.Tick += async (s, e) =>
                {
                    checkCount++;
                    if (checkCount > 15) // 15 second timeout
                    {
                        checkTimer.Stop();
                        _logger.LogWarning("Renderer initialization timed out");
                        return;
                    }

                    try
                    {
                        // Check if both mermaid and markdown-it are loaded
                        var isReady = await PreviewBrowser.ExecuteScriptAsync(
                            "window.mermaid !== undefined && window.md !== undefined");
                        if (isReady == "true")
                        {
                            checkTimer.Stop();
                            _logger.LogInformation("Renderers are ready!");
                            _isWebViewReady = true;
                            
                            // Initialize Markdown to Word export now that WebView2 is ready
                            InitializeMarkdownToWordExport();
                            
                            await UpdatePreview(); // Initial render
                        }
                        else
                        {
                            _logger.LogDebug($"Waiting for renderers... (attempt {checkCount})");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error checking renderer readiness: {ex.Message}", ex);
                    }
                };

                checkTimer.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing WebView: {ex.Message}", ex);
                // Show error in the UI
                var errorMessage = $"Error initializing diagram preview: {ex.Message}";
                PreviewBrowser.NavigateToString($"<div style='color:red; padding:20px;'>{errorMessage}</div>");
            }
        }

        private void PreviewBrowser_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _logger.LogDebug($"Navigation completed. IsSuccess: {args.IsSuccess}, WebErrorStatus: {args.WebErrorStatus}");
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
                
                // Sync content with Export to Word system if a Markdown or Mermaid file is loaded
                if (_markdownToWordViewModel != null && 
                    !string.IsNullOrWhiteSpace(_markdownToWordViewModel.MarkdownFilePath))
                {
                    var fileExtension = Path.GetExtension(_markdownToWordViewModel.MarkdownFilePath).ToLower();
                    
                    // Handle both Markdown files and Mermaid files
                    if (fileExtension == ".md" || fileExtension == ".markdown")
                    {
                        // For Markdown files, use content as-is
                        if (_currentContentType == ContentType.Markdown || _currentContentType == ContentType.MarkdownWithMermaid)
                        {
                            _markdownToWordViewModel.UpdateMarkdownContent(currentCode);
                        }
                    }
                    else if (fileExtension == ".mmd")
                    {
                        // For Mermaid files, wrap in Markdown code block
                        var wrappedContent = $"# Mermaid Diagram\n\n```mermaid\n{currentCode}\n```";
                        _markdownToWordViewModel.UpdateMarkdownContent(wrappedContent);
                    }
                }
            }
        }

        private async Task UpdatePreview()
        {
            try
            {
                if (PreviewBrowser?.CoreWebView2 == null)
                {
                    _logger.LogDebug("WebView not initialized yet");
                    return;
                }

                var code = CodeEditor.Text;
                if (code == _lastPreviewedCode)
                {
                    return; // Skip if the code hasn't changed
                }

                _logger.LogDebug($"Updating preview with code length: {code.Length}");

                // Clear detection cache to ensure fresh content type detection
                _contentTypeDetector.ClearCache();

                // Create rendering context
                // When no file is loaded, use empty extension to let content detection work
                var fileExtension = !string.IsNullOrEmpty(_currentFilePath) 
                    ? Path.GetExtension(_currentFilePath).TrimStart('.') 
                    : string.Empty;

                var context = new RenderingContext
                {
                    FileExtension = fileExtension,
                    FilePath = _currentFilePath,
                    EnableMermaidInMarkdown = true,
                    Theme = ThemeMode.Dark, // TODO: Detect from app theme
                    StyleSettings = _styleSettingsService.LoadSettings()
                };

                // Use orchestrator to render content
                var renderResult = await _renderingOrchestrator.RenderAsync(code, context);
                
                if (!renderResult.Success)
                {
                    _logger.LogError($"Rendering failed: {renderResult.ErrorMessage}");
                    return;
                }

                _currentContentType = renderResult.DetectedContentType;
                _logger.LogInformation($"Detected content type: {_currentContentType}");

                // Execute appropriate JavaScript rendering based on content type
                var rendered = await ExecuteRenderingScript(code, renderResult.DetectedContentType, context);

                // Only mark as previewed if the render actually executed
                if (!rendered)
                {
                    return;
                }

                // Update export ViewModel with current content so export menu is enabled
                if (_markdownToWordViewModel != null)
                {
                    if (_currentContentType == ContentType.Markdown || _currentContentType == ContentType.MarkdownWithMermaid)
                    {
                        _markdownToWordViewModel.UpdateMarkdownContent(code);
                        _logger.LogDebug("Updated export ViewModel with Markdown content");
                    }
                    else if (_currentContentType == ContentType.Mermaid)
                    {
                        // Wrap Mermaid diagram in Markdown format for export
                        var wrappedContent = $"# Mermaid Diagram\n\n```mermaid\n{code}\n```";
                        _markdownToWordViewModel.UpdateMarkdownContent(wrappedContent);
                        _logger.LogDebug("Updated export ViewModel with wrapped Mermaid content");
                    }
                }

                _lastPreviewedCode = code;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdatePreview: {ex.Message}", ex);
            }
        }

        private async Task<bool> ExecuteRenderingScript(string content, ContentType contentType, RenderingContext context)
        {
            // Check if WebView is ready
            if (!_isWebViewReady)
            {
                _logger.LogDebug("WebView not ready yet, skipping render");
                return false;
            }

            var escapedContent = JsonSerializer.Serialize(content);
            var theme = context.Theme.ToString().ToLower();

            string script;

            switch (contentType)
            {
                case ContentType.Mermaid:
                    var mermaidRenderer = _renderingOrchestrator.GetRenderer(ContentType.Mermaid) as MermaidRenderer;
                    script = mermaidRenderer?.GenerateRenderScript(content, theme) 
                        ?? $"if (window.renderMermaid) {{ window.renderMermaid({escapedContent}, '{theme}'); }}";
                    break;

                case ContentType.Markdown:
                case ContentType.MarkdownWithMermaid:
                    var markdownRenderer = _renderingOrchestrator.GetRenderer(ContentType.Markdown) as MarkdownRenderer;
                    var enableMermaid = contentType == ContentType.MarkdownWithMermaid || context.EnableMermaidInMarkdown;
                    var styleSettings = _styleSettingsService.LoadSettings();
                    script = markdownRenderer?.GenerateRenderScript(content, enableMermaid, theme, styleSettings, context.FilePath)
                        ?? $"if (window.renderMarkdown) {{ window.renderMarkdown({escapedContent}, {enableMermaid.ToString().ToLower()}, '{theme}'); }}";
                    break;

                default:
                    _logger.LogWarning($"Unknown content type: {contentType}, defaulting to Mermaid");
                    script = $"if (window.renderMermaid) {{ window.renderMermaid({escapedContent}, '{theme}'); }}";
                    break;
            }

            try
            {
                var result = await PreviewBrowser.ExecuteScriptAsync(script);
                _logger.LogDebug($"Render script executed: {result}");
                
                // Setup scroll synchronization after rendering (for Mermaid and Markdown with Mermaid)
                if (contentType == ContentType.Mermaid || 
                    contentType == ContentType.Markdown || 
                    contentType == ContentType.MarkdownWithMermaid)
                {
                    await SetupScrollSynchronization();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing render script: {ex.Message}", ex);
                return false;
            }
        }

        private void OnRenderingStateChanged(object? sender, RenderingStateChangedEventArgs e)
        {
            _logger.LogInformation($"Rendering state changed: {e.State}");
            
            if (e.State == RenderingState.Failed && e.Result != null)
            {
                _logger.LogError($"Rendering failed: {e.Result.ErrorMessage}");
            }
            else if (e.State == RenderingState.Completed && e.Result != null)
            {
                // Update status bar on UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateRenderModeIndicator(e.Result.DetectedContentType);
                });
            }
        }
    }
}
