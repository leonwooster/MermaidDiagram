using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Wrapper implementation for CoreWebView2.
/// </summary>
public class CoreWebView2Wrapper : IWebView2Wrapper
{
    private readonly CoreWebView2 _coreWebView2;

    public CoreWebView2Wrapper(CoreWebView2 coreWebView2)
    {
        _coreWebView2 = coreWebView2 ?? throw new ArgumentNullException(nameof(coreWebView2));
    }

    public async Task<string> ExecuteScriptAsync(string script)
    {
        return await _coreWebView2.ExecuteScriptAsync(script);
    }
}
