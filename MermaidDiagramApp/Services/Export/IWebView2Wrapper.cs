using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Wrapper interface for CoreWebView2 to enable testability.
/// </summary>
public interface IWebView2Wrapper
{
    /// <summary>
    /// Executes JavaScript code in the WebView2 control.
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    /// <returns>The result of the script execution as a JSON string.</returns>
    Task<string> ExecuteScriptAsync(string script);
}
