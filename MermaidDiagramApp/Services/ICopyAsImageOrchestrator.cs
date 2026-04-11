using System;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Abstracts the "Copy as Image" orchestration logic so it can be tested
/// without WinUI / WebView2 dependencies.
/// </summary>
public interface ICopyAsImageOrchestrator
{
    /// <summary>
    /// Executes the copy-as-image workflow.
    /// </summary>
    /// <param name="isWebViewReady">Whether the WebView2 CoreWebView2 is initialised.</param>
    /// <param name="editorText">Current text in the code editor.</param>
    /// <param name="capturePng">Delegate that captures the preview as PNG bytes.</param>
    /// <returns>The status message to display in the status bar.</returns>
    Task<string> ExecuteAsync(
        bool isWebViewReady,
        string? editorText,
        Func<Task<byte[]>> capturePng);
}
