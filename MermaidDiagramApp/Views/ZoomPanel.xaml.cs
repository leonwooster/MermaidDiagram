using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.ViewModels;

namespace MermaidDiagramApp.Views;

/// <summary>
/// UserControl that hosts a WebView2 for displaying an enlarged, zoomable diagram.
/// Communicates with ZoomPanelHost.html via ExecuteScriptAsync and WebMessageReceived.
/// </summary>
public sealed partial class ZoomPanel : UserControl
{
    private bool _isWebViewInitialized;
    private TaskCompletionSource<bool>? _navigationTcs;
    private readonly IZoomPanelService _zoomPanelService;

    public ZoomPanelViewModel ViewModel { get; }

    public ZoomPanel()
    {
        this.InitializeComponent();

        // Resolve dependencies from the application-wide service provider
        _zoomPanelService = App.Services.GetService(typeof(IZoomPanelService)) as IZoomPanelService
            ?? throw new InvalidOperationException("IZoomPanelService not registered in DI container.");

        ViewModel = App.Services.GetService(typeof(ZoomPanelViewModel)) as ZoomPanelViewModel
            ?? throw new InvalidOperationException("ZoomPanelViewModel not registered in DI container.");

        Unloaded += ZoomPanel_Unloaded;
    }

    /// <summary>
    /// Initializes the WebView2 control, sets up virtual host mapping, and navigates to ZoomPanelHost.html.
    /// </summary>
    public async Task InitializeWebViewAsync()
    {
        if (_isWebViewInitialized)
            return;

        var assetsPath = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets");

        var env = await CoreWebView2Environment.CreateAsync();
        await ZoomBrowser.EnsureCoreWebView2Async(env);

        var core = ZoomBrowser.CoreWebView2;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsWebMessageEnabled = true;

        const string virtualHost = "appassets";
        try { core.ClearVirtualHostNameToFolderMapping(virtualHost); }
        catch { /* no existing mapping */ }

        core.SetVirtualHostNameToFolderMapping(
            virtualHost,
            assetsPath,
            CoreWebView2HostResourceAccessKind.Allow);

        // Wire up message handler for wheel and keypress forwarding
        core.WebMessageReceived += ZoomBrowser_WebMessageReceived;

        // Wait for the page to fully load before allowing script execution
        _navigationTcs = new TaskCompletionSource<bool>();
        core.NavigationCompleted += OnInitialNavigationCompleted;
        core.Navigate($"https://{virtualHost}/ZoomPanelHost.html");
        await _navigationTcs.Task;

        _isWebViewInitialized = true;
    }

    private void OnInitialNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        sender.NavigationCompleted -= OnInitialNavigationCompleted;
        _navigationTcs?.TrySetResult(args.IsSuccess);
    }

    /// <summary>
    /// Loads SVG content into the zoom panel by calling the setDiagram() JS function.
    /// </summary>
    public async Task LoadDiagramAsync(string svgContent)
    {
        if (!_isWebViewInitialized)
            await InitializeWebViewAsync();

        // JSON-encode the SVG string so it's safe to embed in a JS call
        var escaped = JsonSerializer.Serialize(svgContent);
        await ZoomBrowser.ExecuteScriptAsync($"window.setDiagram({escaped})");
    }

    /// <summary>
    /// Applies a CSS scale transform to the diagram via the setZoom() JS function.
    /// </summary>
    public async Task SetZoomLevel(double level)
    {
        if (!_isWebViewInitialized)
            return;

        var levelStr = level.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await ZoomBrowser.ExecuteScriptAsync($"window.setZoom({levelStr})");
    }

    /// <summary>
    /// Applies a theme (light/dark) to the host page via the setTheme() JS function.
    /// </summary>
    public async Task SetTheme(string theme)
    {
        if (!_isWebViewInitialized)
            return;

        var escaped = JsonSerializer.Serialize(theme);
        await ZoomBrowser.ExecuteScriptAsync($"window.setTheme({escaped})");
    }

    /// <summary>
    /// Clears the diagram content when the zoom panel is hidden.
    /// Keeps the WebView initialized so re-opening is fast and avoids race conditions.
    /// </summary>
    public void NavigateToBlank()
    {
        if (_isWebViewInitialized && ZoomBrowser.CoreWebView2 != null)
        {
            try
            {
                _ = ZoomBrowser.ExecuteScriptAsync("document.getElementById('diagram').innerHTML = ''");
            }
            catch { /* ignore errors during cleanup */ }
        }
    }

    /// <summary>
    /// Handles WebMessageReceived from ZoomPanelHost.html.
    /// Routes zoomWheel messages to IZoomPanelService.ApplyWheelDelta()
    /// and keypress/Escape messages to IZoomPanelService.Close().
    /// </summary>
    private void ZoomBrowser_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var message = args.TryGetWebMessageAsString();
        if (string.IsNullOrEmpty(message))
            return;

        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var messageType = typeElement.GetString();

            if (messageType == "zoomWheel")
            {
                // Sub-task 5.5: route wheel delta to service
                if (root.TryGetProperty("deltaY", out var deltaYElement))
                {
                    var deltaY = deltaYElement.GetDouble();
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        _zoomPanelService.ApplyWheelDelta(deltaY);
                    });
                }
            }
            else if (messageType == "keypress")
            {
                // Sub-task 5.6: route Escape key to service close
                if (root.TryGetProperty("key", out var keyElement)
                    && keyElement.GetString() == "Escape")
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        _zoomPanelService.Close();
                    });
                }
            }
        }
        catch (JsonException)
        {
            // Ignore non-JSON messages
        }
    }

    /// <summary>
    /// Disposes the WebView2 control when the UserControl is unloaded.
    /// </summary>
    private void ZoomPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized && ZoomBrowser.CoreWebView2 != null)
        {
            ZoomBrowser.CoreWebView2.WebMessageReceived -= ZoomBrowser_WebMessageReceived;
        }

        ZoomBrowser.Close();
        _isWebViewInitialized = false;
    }
}
