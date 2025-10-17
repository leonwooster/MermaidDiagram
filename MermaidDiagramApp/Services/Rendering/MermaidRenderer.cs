using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services.Rendering;

/// <summary>
/// Renders Mermaid diagram content using Mermaid.js in WebView2.
/// Encapsulates all Mermaid-specific rendering logic.
/// </summary>
public class MermaidRenderer : IContentRenderer
{
    private readonly List<string> _supportedFeatures = new()
    {
        "Flowcharts",
        "Sequence Diagrams",
        "Class Diagrams",
        "State Diagrams",
        "ER Diagrams",
        "Gantt Charts",
        "Pie Charts",
        "User Journey",
        "Git Graph",
        "Mindmaps",
        "Timeline",
        "Quadrant Charts",
        "Requirement Diagrams",
        "C4 Diagrams"
    };

    public ContentType SupportedType => ContentType.Mermaid;

    public bool CanRender(ContentType type)
    {
        return type == ContentType.Mermaid;
    }

    public Task<RenderingResult> RenderAsync(string content, IRenderingContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Task.FromResult(RenderingResult.ErrorResult("Content is empty", ContentType.Mermaid));
            }

            // Basic validation - check for Mermaid keywords
            if (!ContainsMermaidKeywords(content))
            {
                return Task.FromResult(RenderingResult.ErrorResult("Content does not appear to be a valid Mermaid diagram", ContentType.Mermaid));
            }

            // The actual rendering will be done by JavaScript in WebView2
            var result = RenderingResult.SuccessResult(content, ContentType.Mermaid);
            result.RenderDuration = stopwatch.Elapsed;
            result.Metadata["Theme"] = context.Theme.ToString();

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Task.FromResult(RenderingResult.ErrorResult($"Mermaid rendering failed: {ex.Message}", ContentType.Mermaid));
        }
    }

    public IReadOnlyList<string> GetSupportedFeatures()
    {
        return _supportedFeatures.AsReadOnly();
    }

    /// <summary>
    /// Generates the JavaScript command to render Mermaid in WebView2.
    /// </summary>
    public string GenerateRenderScript(string content, string theme)
    {
        // Escape content for JavaScript
        var escapedContent = System.Text.Json.JsonSerializer.Serialize(content);
        
        return $@"
            (async function() {{
                try {{
                    await window.renderMermaid({escapedContent}, '{theme}');
                }} catch (error) {{
                    console.error('Mermaid render error:', error);
                    window.chrome.webview.postMessage({{
                        type: 'renderError',
                        error: error.message
                    }});
                }}
            }})();
        ";
    }

    private bool ContainsMermaidKeywords(string content)
    {
        var keywords = new[] { "graph", "flowchart", "sequenceDiagram", "classDiagram", 
            "stateDiagram", "erDiagram", "gantt", "pie", "journey", "gitGraph", 
            "mindmap", "timeline", "quadrantChart", "requirementDiagram", "C4Context" };

        return keywords.Any(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
