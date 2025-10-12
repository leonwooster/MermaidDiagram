using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services.Rendering;

/// <summary>
/// Detects content type from file content and extension.
/// Implements caching for performance optimization.
/// </summary>
public class ContentTypeDetector : IContentTypeDetector
{
    private readonly ConcurrentDictionary<string, Func<string, ContentType>> _customRules = new();
    private readonly ConcurrentDictionary<string, ContentType> _detectionCache = new();
    
    // Mermaid diagram keywords to detect in content
    private static readonly string[] MermaidKeywords = new[]
    {
        "graph",
        "flowchart",
        "sequenceDiagram",
        "classDiagram",
        "stateDiagram",
        "erDiagram",
        "gantt",
        "pie",
        "journey",
        "gitGraph",
        "mindmap",
        "timeline",
        "quadrantChart",
        "requirementDiagram",
        "C4Context"
    };

    public ContentType DetectContentType(string content, string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ContentType.Unknown;

        // Normalize extension
        fileExtension = fileExtension?.ToLowerInvariant().TrimStart('.') ?? string.Empty;

        // Check cache first
        var cacheKey = $"{fileExtension}:{content.GetHashCode()}";
        if (_detectionCache.TryGetValue(cacheKey, out var cachedType))
            return cachedType;

        ContentType detectedType;

        // Check custom rules first
        if (_customRules.TryGetValue(fileExtension, out var customRule))
        {
            detectedType = customRule(content);
        }
        else
        {
            detectedType = DetectContentTypeInternal(content, fileExtension);
        }

        // Cache the result
        _detectionCache.TryAdd(cacheKey, detectedType);

        return detectedType;
    }

    private ContentType DetectContentTypeInternal(string content, string fileExtension)
    {
        // .mmd files are always Mermaid
        if (fileExtension == "mmd")
            return ContentType.Mermaid;

        // .md files need content analysis
        if (fileExtension == "md" || fileExtension == "markdown")
        {
            return AnalyzeMarkdownContent(content);
        }

        // For unknown extensions, try to detect from content
        if (ContainsMermaidKeywords(content, checkFirst10Lines: true))
            return ContentType.Mermaid;

        return ContentType.Unknown;
    }

    private ContentType AnalyzeMarkdownContent(string content)
    {
        // First, check if document contains Mermaid code blocks (```mermaid)
        // This takes priority as it's the most explicit indicator
        if (ContainsMermaidCodeBlocks(content))
        {
            return ContentType.MarkdownWithMermaid;
        }

        // Check first 5 lines for Mermaid keywords (not in code blocks)
        // If file starts with raw Mermaid syntax, treat as pure Mermaid
        var lines = content.Split('\n', StringSplitOptions.None).Take(5).ToArray();
        var firstLines = string.Join('\n', lines);

        // Only treat as pure Mermaid if it starts immediately with Mermaid syntax
        // and doesn't have typical Markdown indicators (# headers, **, etc.)
        if (ContainsMermaidKeywords(firstLines, checkFirst10Lines: false) && 
            !ContainsMarkdownIndicators(firstLines))
        {
            return ContentType.Mermaid;
        }

        // Otherwise, it's pure Markdown
        return ContentType.Markdown;
    }

    private bool ContainsMarkdownIndicators(string content)
    {
        // Check for common Markdown syntax
        return content.Contains("# ") ||      // Headers
               content.Contains("## ") ||
               content.Contains("**") ||      // Bold
               content.Contains("__") ||
               content.Contains("- ") ||      // Lists
               content.Contains("* ") ||
               content.Contains("1. ") ||     // Ordered lists
               content.Contains("[") ||       // Links
               content.Contains("]");
    }

    private bool ContainsMermaidKeywords(string content, bool checkFirst10Lines)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var searchContent = content;
        if (checkFirst10Lines)
        {
            var lines = content.Split('\n', StringSplitOptions.None).Take(10);
            searchContent = string.Join('\n', lines);
        }

        // Check for Mermaid keywords at the start of lines
        foreach (var keyword in MermaidKeywords)
        {
            // Match keyword at start of line or after whitespace
            var pattern = $@"(^|\n)\s*{Regex.Escape(keyword)}\b";
            if (Regex.IsMatch(searchContent, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    private bool ContainsMermaidCodeBlocks(string content)
    {
        // Look for ```mermaid code blocks
        var pattern = @"```\s*mermaid\s*\n";
        return Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase);
    }

    public void RegisterDetectionRule(string extension, Func<string, ContentType> rule)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be null or empty", nameof(extension));

        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        extension = extension.ToLowerInvariant().TrimStart('.');
        _customRules[extension] = rule;
        
        // Clear cache when rules change
        ClearCache();
    }

    public void ClearCache()
    {
        _detectionCache.Clear();
    }
}
