using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services.Rendering;

/// <summary>
/// Renders Markdown content to HTML using markdown-it.js in WebView2.
/// Supports GitHub Flavored Markdown and embedded Mermaid diagrams.
/// </summary>
public class MarkdownRenderer : IContentRenderer
{
    private readonly List<string> _supportedFeatures = new()
    {
        "GitHub Flavored Markdown",
        "Tables",
        "Task Lists",
        "Strikethrough",
        "Code Syntax Highlighting",
        "Embedded Mermaid Diagrams",
        "Links and Images",
        "Headings and Lists"
    };

    public ContentType SupportedType => ContentType.Markdown;

    public bool CanRender(ContentType type)
    {
        return type == ContentType.Markdown || type == ContentType.MarkdownWithMermaid;
    }

    public async Task<RenderingResult> RenderAsync(string content, IRenderingContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RenderingResult.ErrorResult("Content is empty", ContentType.Markdown);
            }

            // Validate content size
            if (content.Length > 10_000_000) // 10MB limit
            {
                return RenderingResult.ErrorResult("Content is too large (>10MB)", ContentType.Markdown);
            }

            // The actual rendering will be done by JavaScript in WebView2
            // This method prepares the content and returns metadata
            var result = RenderingResult.SuccessResult(content, ContentType.Markdown);
            result.RenderDuration = stopwatch.Elapsed;
            result.Metadata["EnableMermaidInMarkdown"] = context.EnableMermaidInMarkdown;
            result.Metadata["Theme"] = context.Theme.ToString();

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return RenderingResult.ErrorResult($"Markdown rendering failed: {ex.Message}", ContentType.Markdown);
        }
    }

    public IReadOnlyList<string> GetSupportedFeatures()
    {
        return _supportedFeatures.AsReadOnly();
    }

    /// <summary>
    /// Generates the JavaScript command to render Markdown in WebView2.
    /// </summary>
    public string GenerateRenderScript(string content, bool enableMermaid, string theme)
    {
        // Escape content for JavaScript
        var escapedContent = System.Text.Json.JsonSerializer.Serialize(content);
        
        return $@"
            (async function() {{
                try {{
                    await window.renderMarkdown({escapedContent}, {enableMermaid.ToString().ToLower()}, '{theme}');
                }} catch (error) {{
                    console.error('Markdown render error:', error);
                    window.chrome.webview.postMessage({{
                        type: 'renderError',
                        error: error.message
                    }});
                }}
            }})();
        ";
    }
}
