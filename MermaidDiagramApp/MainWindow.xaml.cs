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
using Microsoft.UI.Dispatching;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using TextControlBoxNS;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Services.Rendering;
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
        private string? _lastPreviewedCode = "";
        private bool _isFullScreen = false;
        private bool _isPresentationMode = false;
        private bool _isPanModeEnabled = false;
        private bool _isBuilderVisible = false;
        private MermaidLinter _linter;
        private Version? _mermaidVersion;
        private readonly ILogger _logger = LoggingService.Instance.GetLogger<MainWindow>();
        
        // Rendering orchestration components
        private readonly RenderingOrchestrator _renderingOrchestrator;
        private readonly IContentTypeDetector _contentTypeDetector;
        private readonly ContentRendererFactory _rendererFactory;
        private ContentType _currentContentType = ContentType.Unknown;
        private string _currentFilePath = string.Empty;

        public DiagramBuilderViewModel BuilderViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            BuilderViewModel = new DiagramBuilderViewModel();
            BuilderPanel.DataContext = BuilderViewModel;
            BuilderViewModel.PropertyChanged += DiagramBuilderViewModel_PropertyChanged;

            _linter = new MermaidLinter();
            
            // Initialize rendering orchestration
            _contentTypeDetector = new ContentTypeDetector();
            _rendererFactory = new ContentRendererFactory();
            _renderingOrchestrator = new RenderingOrchestrator(_contentTypeDetector, _rendererFactory);
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
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private async Task CheckForMermaidUpdatesAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                // Ensure the Mermaid folder exists
                var mermaidFolder = await localFolder.CreateFolderAsync("Mermaid", CreationCollisionOption.OpenIfExists);
                
                // Try to read the version file
                try
                {
                    var versionFile = await mermaidFolder.GetFileAsync("mermaid-version.txt");
                    var currentVersionStr = (await FileIO.ReadTextAsync(versionFile)).Trim();
                    if (!string.IsNullOrEmpty(currentVersionStr))
                    {
                        _logger.LogInformation($"Current Mermaid.js version from file: {currentVersionStr}");
                        await CheckForNewerVersionAsync(currentVersionStr);
                    }
                    else
                    {
                        _logger.LogWarning("Version file is empty, checking for updates with default version");
                        await CheckForNewerVersionAsync("10.9.0");
                    }
                }
                catch (FileNotFoundException)
                {
                    _logger.LogInformation("Mermaid version file not found. Checking with default version.");
                    await CheckForNewerVersionAsync("10.9.0");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to check for Mermaid.js updates: {ex.Message}", ex);
                // Still try to check with default version even if folder creation fails
                await CheckForNewerVersionAsync("10.9.0");
            }
        }

        private async void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _lastPreviewedCode = null;
                await UpdatePreview();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Manual refresh failed: {ex.Message}", ex);
            }
        }

        private async Task CheckForNewerVersionAsync(string currentVersionStr)
        {
            try
            {
                // Ensure the Mermaid folder exists
                var localFolder = ApplicationData.Current.LocalFolder;
                var mermaidFolder = await localFolder.CreateFolderAsync("Mermaid", CreationCollisionOption.OpenIfExists);
                
                // Check if we have a valid version string
                if (string.IsNullOrEmpty(currentVersionStr))
                {
                    // Try to read the version file again
                    try
                    {
                        var versionFile = await mermaidFolder.GetFileAsync("mermaid-version.txt");
                        currentVersionStr = (await FileIO.ReadTextAsync(versionFile)).Trim();
                    }
                    catch
                    {
                        // If we still can't get the version, use the default
                        currentVersionStr = "10.9.0";
                    }
                }
                
                await CheckForNewerVersionInternalAsync(currentVersionStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CheckForNewerVersionAsync: {ex.Message}", ex);
            }
        }

        private async Task CheckForNewerVersionInternalAsync(string currentVersionStr)
        {
            if (string.IsNullOrEmpty(currentVersionStr))
            {
                _logger.LogWarning("Current version string is null or empty, using default version");
                currentVersionStr = "10.9.0";
            }

            try
            {
                string latestVersionStr;
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10); // Add a timeout to prevent hanging
                    var response = await client.GetStringAsync("https://registry.npmjs.org/mermaid");
                    
                    if (string.IsNullOrEmpty(response))
                    {
                        _logger.LogWarning("Received empty response from npm registry");
                        return;
                    }

                    using (var jsonDoc = JsonDocument.Parse(response))
                    {
                        if (!jsonDoc.RootElement.TryGetProperty("dist-tags", out var distTags) ||
                            !distTags.TryGetProperty("latest", out var latestVersion))
                        {
                            _logger.LogWarning("Could not find latest version in npm registry response");
                            return;
                        }
                        
                        latestVersionStr = latestVersion.GetString() ?? string.Empty;
                        _logger.LogInformation($"Latest Mermaid.js version from npm: {latestVersionStr}, Current version: {currentVersionStr}");
                    }
                }

                if (string.IsNullOrEmpty(latestVersionStr))
                {
                    _logger.LogWarning("Latest version string is null or empty");
                    return;
                }

                // Clean up version strings (remove any non-numeric or dot characters)
                var cleanCurrentVersion = new string(currentVersionStr.Where(c => char.IsDigit(c) || c == '.').ToArray());
                var cleanLatestVersion = new string(latestVersionStr.Trim().Where(c => char.IsDigit(c) || c == '.').ToArray());

                if (string.IsNullOrEmpty(cleanCurrentVersion) || string.IsNullOrEmpty(cleanLatestVersion))
                {
                    _logger.LogWarning($"Invalid version strings after cleaning - Current: '{cleanCurrentVersion}', Latest: '{cleanLatestVersion}'");
                    return;
                }

                try
                {
                    // Ensure version strings have at least two parts (major.minor)
                    if (cleanCurrentVersion.Count(c => c == '.') < 1) cleanCurrentVersion += ".0";
                    if (cleanLatestVersion.Count(c => c == '.') < 1) cleanLatestVersion += ".0";

                    var currentVersion = new Version(cleanCurrentVersion);
                    var latestVersion = new Version(cleanLatestVersion);
                    
                    _logger.LogDebug($"Comparing versions - Current: {currentVersion}, Latest: {latestVersion}");
                    
                    // Determine if update is needed and update UI accordingly
                    var shouldUpdate = currentVersion < latestVersion;
                    var message = shouldUpdate 
                        ? $"A new version of Mermaid.js ({latestVersion}) is available. You are using version {currentVersion}."
                        : "You are using the latest version of Mermaid.js.";

                    _logger.LogInformation($"Update available: {shouldUpdate}. {message}");

                    // Use the dispatcher to update the UI on the UI thread
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                    {
                        try
                        {
                            if (UpdateInfoBar == null)
                            {
                                _logger.LogError("Error: UpdateInfoBar is null");
                                return;
                            }

                            if (UpdateInfoBar.ActionButton is Button button)
                            {
                                if (shouldUpdate)
                                {
                                    _logger.LogInformation("Showing update notification in UI");
                                    button.Visibility = Visibility.Visible;
                                    button.IsEnabled = true;
                                    UpdateInfoBar.Message = message;
                                    UpdateInfoBar.Severity = InfoBarSeverity.Informational;
                                    UpdateInfoBar.IsOpen = true;
                                }
                                else
                                {
                                    _logger.LogInformation("No update available, hiding notification");
                                    button.Visibility = Visibility.Collapsed;
                                    UpdateInfoBar.IsOpen = false;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Warning: Update button not found in UpdateInfoBar");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error updating UI: {ex.Message}", ex);
                        }                        
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error comparing versions: {ex.Message}", ex);
                    // If there's an error, assume we're on the latest version to be safe
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                    {
                        if (UpdateInfoBar.ActionButton is Button button)
                        {
                            button.Visibility = Visibility.Collapsed;
                            UpdateInfoBar.IsOpen = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking for newer Mermaid.js version: {ex.Message}", ex);
                // Don't re-throw here to prevent app crashes
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    if (UpdateInfoBar.ActionButton is Button button)
                    {
                        button.Visibility = Visibility.Collapsed;
                        UpdateInfoBar.IsOpen = false;
                    }
                });
            }
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                _isWebViewReady = false;

                var assetsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets");
                _logger.LogInformation($"Initializing WebView with assets from: {assetsPath}");

                // Ensure WebView2 is initialized
                var webView2Environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
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
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

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

        private async Task CopyAssetsToLocalFolder()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var mermaidFolder = await localFolder.CreateFolderAsync("Mermaid", CreationCollisionOption.OpenIfExists);
            var assetsSourcePath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets");

            // First, copy all assets except mermaid files
            await Task.Run(() =>
            {
                foreach (string file in Directory.GetFiles(assetsSourcePath))
                {
                    // Skip mermaid files in the root copy
                    if (Path.GetFileName(file).StartsWith("mermaid")) continue;
                    
                    string destFile = Path.Combine(localFolder.Path, Path.GetFileName(file));
                    try
                    {
                        if (File.Exists(destFile))
                        {
                            File.Delete(destFile);
                        }
                        File.Copy(file, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to copy asset {file}: {ex.Message}", ex);
                    }
                }
            });

            // Check if we need to copy the default mermaid files
            var mermaidFile = Path.Combine(mermaidFolder.Path, "mermaid.min.js");
            var versionFile = Path.Combine(mermaidFolder.Path, "mermaid-version.txt");

            if (!File.Exists(mermaidFile) || !File.Exists(versionFile))
            {
                var sourceMermaid = Path.Combine(assetsSourcePath, "mermaid.min.js");
                if (File.Exists(sourceMermaid))
                {
                    try
                    {
                        File.Copy(sourceMermaid, mermaidFile, true);
                        await File.WriteAllTextAsync(versionFile, "10.9.0"); // Default version
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to copy mermaid files: {ex.Message}", ex);
                    }
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Create the destination directory if it doesn't exist
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Get the files in the source directory and copy them to the new location
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                try
                {
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Copy(file, destFile, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to copy asset {file}: {ex.Message}");
                }
            }

            // Recursively copy subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(subDir, Path.Combine(destinationDir, Path.GetFileName(subDir)));
            }
        }

        private void PreviewBrowser_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
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
            }
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

                // Create rendering context
                var fileExtension = !string.IsNullOrEmpty(_currentFilePath) 
                    ? Path.GetExtension(_currentFilePath).TrimStart('.') 
                    : "mmd";

                var context = new RenderingContext
                {
                    FileExtension = fileExtension,
                    FilePath = _currentFilePath,
                    EnableMermaidInMarkdown = true,
                    Theme = ThemeMode.Dark // TODO: Detect from app theme
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
                await ExecuteRenderingScript(code, renderResult.DetectedContentType, context);

                _lastPreviewedCode = code;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdatePreview: {ex.Message}", ex);
            }
        }

        private async Task ExecuteRenderingScript(string content, ContentType contentType, RenderingContext context)
        {
            // Check if WebView is ready
            if (!_isWebViewReady)
            {
                _logger.LogDebug("WebView not ready yet, skipping render");
                return;
            }

            var escapedContent = System.Text.Json.JsonSerializer.Serialize(content);
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
                    script = markdownRenderer?.GenerateRenderScript(content, enableMermaid, theme)
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing render script: {ex.Message}", ex);
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

        private void UpdateRenderModeIndicator(ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Mermaid:
                    RenderModeText.Text = "Mermaid Diagram";
                    RenderModeIcon.Glyph = "\uE8A5"; // Chart icon
                    break;
                case ContentType.Markdown:
                    RenderModeText.Text = "Markdown Document";
                    RenderModeIcon.Glyph = "\uE8A5"; // Document icon
                    break;
                case ContentType.MarkdownWithMermaid:
                    RenderModeText.Text = "Markdown with Diagrams";
                    RenderModeIcon.Glyph = "\uE8A5"; // Combined icon
                    break;
                default:
                    RenderModeText.Text = "Unknown";
                    RenderModeIcon.Glyph = "\uE897"; // Warning icon
                    break;
            }
        }

        private void RenderModeOverride_Click(object sender, RoutedEventArgs e)
        {
            // Flyout will open automatically
        }

        private async void AutoDetectMode_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Switching to auto-detect mode");
            // Clear any forced content type
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

        private void PreviewBrowser_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle Escape key when WebView has focus (e.g., immediately after entering full screen)
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
                _logger.LogError($"Failed to restore window state: {ex.Message}", ex);
            }
        }

        private string AddBackgroundToSvg(string svgContent)
        {
            try
            {
                // Simple approach: just insert a background rectangle right after the opening <svg> tag
                var svgTagEndIndex = svgContent.IndexOf('>');
                if (svgTagEndIndex > 0)
                {
                    // Extract width and height if available, otherwise use large default values
                    var widthMatch = Regex.Match(svgContent, @"width=""([^""]+)""");
                    var heightMatch = Regex.Match(svgContent, @"height=""([^""]+)""");
                    
                    string width = widthMatch.Success ? widthMatch.Groups[1].Value.Replace("px", "") : "1200";
                    string height = heightMatch.Success ? heightMatch.Groups[1].Value.Replace("px", "") : "800";
                    
                    // Create a background rectangle
                    var backgroundRect = $"<rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" fill=\"#222222\"/>";
                    
                    // Insert the background right after the opening <svg> tag
                    return svgContent.Insert(svgTagEndIndex + 1, backgroundRect);
                }
                
                return svgContent; // Return original if we can't parse it
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add background to SVG: {ex.Message}", ex);
                return svgContent; // Return original on error
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

        private async void CheckSyntax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = CodeEditor.Text;
                
                if (string.IsNullOrWhiteSpace(code))
                {
                    var emptyDialog = new ContentDialog
                    {
                        Title = "No Code to Check",
                        Content = "The editor is empty. Please add some Mermaid diagram code first.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await emptyDialog.ShowAsync();
                    return;
                }

                var analyzer = new MermaidSyntaxAnalyzer();
                var fixer = new MermaidSyntaxFixer();
                var currentCode = code;
                var totalFixesApplied = 0;

                // Iterative fix-and-recheck loop
                while (true)
                {
                    // Analyze the current code
                    var issues = analyzer.Analyze(currentCode);

                    // Show the dialog
                    var dialog = new SyntaxIssuesDialog
                    {
                        XamlRoot = this.Content.XamlRoot
                    };
                    dialog.LoadIssues(issues, currentCode);

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // User clicked "Apply Fixes"
                        var selectedIssues = dialog.ViewModel.Issues.Where(i => i.IsSelected).ToList();
                        
                        if (selectedIssues.Any())
                        {
                            // Apply fixes
                            var fixedCode = fixer.ApplyFixes(currentCode, selectedIssues);
                            totalFixesApplied += selectedIssues.Count;

                            // Update the editor
                            CodeEditor.Text = fixedCode;
                            currentCode = fixedCode;

                            // Update preview
                            await UpdatePreview();

                            // Recheck for remaining issues
                            var remainingIssues = analyzer.Analyze(fixedCode);

                            if (remainingIssues.Count > 0)
                            {
                                // Show dialog asking if user wants to continue fixing
                                var continueDialog = new ContentDialog
                                {
                                    Title = "More Issues Found",
                                    Content = $"Applied {selectedIssues.Count} fix(es). Found {remainingIssues.Count} remaining issue(s).\n\nDo you want to review and fix the remaining issues?",
                                    PrimaryButtonText = "Yes, Continue",
                                    CloseButtonText = "No, Done",
                                    DefaultButton = ContentDialogButton.Primary,
                                    XamlRoot = this.Content.XamlRoot
                                };

                                var continueResult = await continueDialog.ShowAsync();
                                
                                if (continueResult == ContentDialogResult.Primary)
                                {
                                    // Continue to next iteration
                                    continue;
                                }
                                else
                                {
                                    // User chose to stop
                                    break;
                                }
                            }
                            else
                            {
                                // No more issues found - show success and exit
                                var successDialog = new ContentDialog
                                {
                                    Title = "All Issues Fixed!",
                                    Content = $"Successfully applied {totalFixesApplied} fix(es) in total.\n\nNo remaining syntax issues detected. Your diagram is clean!",
                                    CloseButtonText = "OK",
                                    XamlRoot = this.Content.XamlRoot
                                };
                                await successDialog.ShowAsync();
                                break;
                            }
                        }
                        else
                        {
                            // No issues selected, exit
                            break;
                        }
                    }
                    else
                    {
                        // User cancelled, exit
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking syntax: {ex.Message}", ex);
                
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"An error occurred while checking syntax: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async Task<bool> FileExistsAsync(StorageFolder folder, string fileName)
        {
            try
            {
                var item = await folder.TryGetItemAsync(fileName);
                return item != null && item.IsOfType(StorageItemTypes.File);
            }
            catch
            {
                return false;
            }
        }

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var package = Windows.ApplicationModel.Package.Current;
            var version = package.Id.Version;
            var versionString = $"App Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var installDate = package.InstalledDate;
            var installDateString = $"Installed: {installDate.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

            // Get Mermaid.js version
            string mermaidVersionString = "Mermaid.js Version: Checking...";
            string versionFilePath = string.Empty;
            string versionContent = string.Empty;

            try
            {
                // Try to get the version from the Mermaid folder
                var localFolder = ApplicationData.Current.LocalFolder;
                var mermaidFolder = await localFolder.CreateFolderAsync("Mermaid", CreationCollisionOption.OpenIfExists);
                var versionFile = await mermaidFolder.CreateFileAsync("mermaid-version.txt", CreationCollisionOption.OpenIfExists);
                versionFilePath = versionFile.Path;
                
                try
                {
                    versionContent = await FileIO.ReadTextAsync(versionFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading version file: {ex.Message}", ex);
                }

                // If we couldn't read the version, use the default
                if (string.IsNullOrWhiteSpace(versionContent))
                {
                    versionContent = "10.9.0";
                    try
                    {
                        await FileIO.WriteTextAsync(versionFile, versionContent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error writing default version: {ex.Message}", ex);
                    }
                }

                mermaidVersionString = $"Mermaid.js Version: {versionContent.Trim()}";
                _logger.LogInformation($"Mermaid version: {versionContent.Trim()}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error determining Mermaid.js version: {ex.Message}", ex);
                mermaidVersionString = "Mermaid.js Version: Error checking version";
            }

            // Get markdown-it version
            string markdownVersionString = "markdown-it Version: Checking...";
            try
            {
                if (_isWebViewReady && PreviewBrowser?.CoreWebView2 != null)
                {
                    var markdownVersionJson = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync(
                        "window.md && window.md.constructor && window.md.constructor.version ? window.md.constructor.version : 'Unknown'");
                    var markdownVersion = JsonSerializer.Deserialize<string>(markdownVersionJson);
                    
                    if (!string.IsNullOrEmpty(markdownVersion) && markdownVersion != "Unknown")
                    {
                        markdownVersionString = $"markdown-it Version: {markdownVersion}";
                    }
                    else
                    {
                        // Fallback to hardcoded version from UnifiedRenderer.html
                        markdownVersionString = "markdown-it Version: 13.0.1";
                    }
                }
                else
                {
                    // WebView not ready, use hardcoded version
                    markdownVersionString = "markdown-it Version: 13.0.1";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error determining markdown-it version: {ex.Message}", ex);
                markdownVersionString = "markdown-it Version: 13.0.1";
            }

            VersionTextBlock.Text = versionString;
            InstallDateTextBlock.Text = installDateString;
            MermaidVersionTextBlock.Text = mermaidVersionString;
            MarkdownVersionTextBlock.Text = markdownVersionString;

            AboutDialog.XamlRoot = this.Content.XamlRoot;
            await AboutDialog.ShowAsync();
        }

        private async void OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            var provider = LoggingService.Instance.LogFileProvider;
            if (provider == null)
            {
                _logger.LogWarning("Log file provider is not available.");
                await ShowMessageAsync("Logs unavailable", "Logging has not been initialised yet. Please try again after restarting the application.");
                return;
            }

            try
            {
                Directory.CreateDirectory(provider.LogsDirectory);

                if (!File.Exists(provider.CurrentLogFilePath))
                {
                    using (File.Create(provider.CurrentLogFilePath))
                    {
                        // create empty file so launcher has a target
                    }
                }

                var storageFile = await StorageFile.GetFileFromPathAsync(provider.CurrentLogFilePath);
                var launched = await Launcher.LaunchFileAsync(storageFile);
                if (!launched)
                {
                    _logger.LogWarning("Windows failed to open the log file automatically.");
                    await ShowMessageAsync("Open Log File", "Windows could not open the log file automatically. It is located in the Logs folder inside the app's LocalState.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open log file: {ex.Message}", ex);
                await ShowMessageAsync("Open Log File Failed", $"Could not open the log file.\n\n{ex.Message}");
            }
        }

        private async void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            var provider = LoggingService.Instance.LogFileProvider;
            if (provider == null)
            {
                _logger.LogWarning("Log file provider is not available.");
                await ShowMessageAsync("Logs unavailable", "Logging has not been initialised yet. Please try again after restarting the application.");
                return;
            }

            try
            {
                Directory.CreateDirectory(provider.LogsDirectory);

                var storageFolder = await StorageFolder.GetFolderFromPathAsync(provider.LogsDirectory);
                var launched = await Launcher.LaunchFolderAsync(storageFolder);
                if (!launched)
                {
                    _logger.LogWarning("Windows failed to open the log folder automatically.");
                    await ShowMessageAsync("Open Log Folder", "Windows could not open the log folder automatically. Please browse to the folder manually.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open log folder: {ex.Message}", ex);
                await ShowMessageAsync("Open Log Folder Failed", $"Could not open the log folder.\n\n{ex.Message}");
            }
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            WinRT_InterOp.InitializeWithWindow(openPicker, this);

            openPicker.FileTypeFilter.Add(".mmd");
            openPicker.FileTypeFilter.Add(".md");
            openPicker.FileTypeFilter.Add(".markdown");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                _currentFilePath = file.Path;
                CodeEditor.Text = await FileIO.ReadTextAsync(file);
                await UpdatePreview();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            
            // Add file type options based on current content type
            if (_currentContentType == ContentType.Markdown || _currentContentType == ContentType.MarkdownWithMermaid)
            {
                savePicker.FileTypeChoices.Add("Markdown Document", new List<string>() { ".md" });
                savePicker.FileTypeChoices.Add("Markdown", new List<string>() { ".markdown" });
                savePicker.FileTypeChoices.Add("Mermaid Diagram", new List<string>() { ".mmd" });
                savePicker.SuggestedFileName = "NewDocument";
            }
            else
            {
                savePicker.FileTypeChoices.Add("Mermaid Diagram", new List<string>() { ".mmd" });
                savePicker.FileTypeChoices.Add("Markdown Document", new List<string>() { ".md" });
                savePicker.SuggestedFileName = "NewDiagram";
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, CodeEditor.Text);
                _currentFilePath = file.Path;
                _logger.LogInformation($"File saved: {file.Path}");
            }
        }

        private async void ExportSvg_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewBrowser?.CoreWebView2 == null) return;

            var svg = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("getSvg()");
            var unescapedSvg = System.Text.Json.JsonSerializer.Deserialize<string>(svg);

            if (string.IsNullOrEmpty(unescapedSvg)) return;

            // Add dark background to SVG to make white lines visible
            var modifiedSvg = AddBackgroundToSvg(unescapedSvg);

            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("SVG Image", new List<string>() { ".svg" });
            savePicker.SuggestedFileName = "Diagram";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, modifiedSvg);
            }
        }

        private async void ExportPng_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewBrowser?.CoreWebView2 == null) return;

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
                try
                {
                    // Use WebView2's built-in screenshot capability to capture exactly what's displayed
                    using var stream = new MemoryStream();
                    await PreviewBrowser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream.AsRandomAccessStream());
                    stream.Position = 0; // Reset stream position after writing

                    // If scaling is needed, we'll process the image
                    if (Math.Abs(scale - 1.0f) > 0.01f) // If scale is not 1.0
                    {
                        // Load the screenshot into SkiaSharp for scaling
                        using var originalBitmap = SKBitmap.Decode(stream);
                        var newWidth = (int)(originalBitmap.Width * scale);
                        var newHeight = (int)(originalBitmap.Height * scale);

                        using var scaledBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                        using var fileStream = await file.OpenStreamForWriteAsync();
                        using var data = scaledBitmap.Encode(SKEncodedImageFormat.Png, 100);
                        data.SaveTo(fileStream);
                    }
                    else
                    {
                        // No scaling needed, save directly
                        using var fileStream = await file.OpenStreamForWriteAsync();
                        await stream.CopyToAsync(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to capture WebView2 screenshot: {ex.Message}", ex);
                    // Fallback to original SVG method if screenshot fails
                    await ExportPngFallback(file, scale);
                }
            }
        }

        private async Task ExportPngFallback(StorageFile file, float scale)
        {
            try
            {
                var svgJson = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("getSvg()");
                var svgString = System.Text.Json.JsonSerializer.Deserialize<string>(svgJson);

                if (string.IsNullOrEmpty(svgString)) return;

                using var svg = new SKSvg();
                using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgString));
                if (svg.Load(svgStream) is { } picture)
                {
                    var dimensions = new SKSizeI((int)(picture.CullRect.Width * scale), (int)(picture.CullRect.Height * scale));
                    using var bitmap = new SKBitmap(new SKImageInfo(dimensions.Width, dimensions.Height));
                    using var canvas = new SKCanvas(bitmap);
                    canvas.Clear(SKColor.Parse("#222222"));
                    var matrix = SKMatrix.CreateScale(scale, scale);
                    canvas.DrawPicture(picture, ref matrix);
                    canvas.Flush();

                    using var fileStream = await file.OpenStreamForWriteAsync();
                    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                    data.SaveTo(fileStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fallback PNG export also failed: {ex.Message}", ex);
            }
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };

            if (this.Content is FrameworkElement rootElement)
            {
                dialog.XamlRoot = rootElement.XamlRoot;
            }

            await dialog.ShowAsync();
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

                    // Ensure the local Mermaid folder exists
                    var localFolder = ApplicationData.Current.LocalFolder;
                    var mermaidFolder = await localFolder.CreateFolderAsync("Mermaid", CreationCollisionOption.OpenIfExists);
                    
                    try
                    {
                        // Save the mermaid.js file
                        var localFile = await mermaidFolder.CreateFileAsync("mermaid.min.js", CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(localFile, newMermaidJsContent);
                        System.Diagnostics.Debug.WriteLine($"Successfully saved mermaid.min.js to {localFile.Path}");

                        // Save the version file
                        var versionFile = await mermaidFolder.CreateFileAsync("mermaid-version.txt", CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteTextAsync(versionFile, latestVersionStr);
                        System.Diagnostics.Debug.WriteLine($"Successfully saved version {latestVersionStr} to {versionFile.Path}");
                        
                        // Clear the WebView2 cache to ensure the new version is loaded
                        if (PreviewBrowser?.CoreWebView2 != null)
                        {
                            try 
                            {
                                await PreviewBrowser.CoreWebView2.Profile.ClearBrowsingDataAsync();
                                System.Diagnostics.Debug.WriteLine("Successfully cleared WebView2 cache");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Failed to clear WebView2 cache: {ex.Message}");
                            }
                        }

                        // Write the updated Mermaid.js to the temporary folder for immediate use
                        try
                        {
                            var tempFolder = ApplicationData.Current.TemporaryFolder;
                            var tempMermaidFile = await tempFolder.CreateFileAsync("mermaid.min.js", CreationCollisionOption.ReplaceExisting);
                            await FileIO.WriteTextAsync(tempMermaidFile, newMermaidJsContent);
                            System.Diagnostics.Debug.WriteLine($"Saved updated Mermaid.js to temporary folder: {tempMermaidFile.Path}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Failed to copy Mermaid.js to temp folder: {ex.Message}");
                            // Continue with restart even if temp copy fails
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving Mermaid files: {ex.Message}");
                        throw; // Re-throw to be caught by the outer try-catch
                    }
                }

                // Update the UI to show success
                UpdateInfoBar.Message = "Mermaid.js has been successfully updated to version " + latestVersionStr + ". Restarting application...";
                UpdateInfoBar.Severity = InfoBarSeverity.Success;
                UpdateInfoBar.IsOpen = true;
                
                // Force UI update
                await Task.Delay(1000);
                
                // Restart the application immediately
                System.Diagnostics.Debug.WriteLine("Restarting application to apply Mermaid.js update...");
                AppInstance.Restart("");
                Application.Current.Exit();
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
