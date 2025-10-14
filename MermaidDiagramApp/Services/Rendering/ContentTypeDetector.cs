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

        // Default to Markdown instead of Unknown
        return ContentType.Markdown;
    }

    private ContentType AnalyzeMarkdownContent(string content)
    {
        // First, check if document contains Mermaid code blocks (```mermaid)
        // This takes priority as it's the most explicit indicator
        var hasMermaidBlocks = ContainsMermaidCodeBlocks(content);
        System.Diagnostics.Debug.WriteLine($"[ContentDetector] Has Mermaid blocks: {hasMermaidBlocks}");
        
        if (hasMermaidBlocks)
        {
            return ContentType.MarkdownWithMermaid;
        }

        // Check if content has Markdown indicators anywhere in the document
        // If it has Markdown syntax, treat as Markdown (not Mermaid)
        if (ContainsMarkdownIndicators(content))
        {
            return ContentType.Markdown;
        }

        // Check first 10 lines for Mermaid keywords
        // Only treat as pure Mermaid if it starts with Mermaid syntax
        // and has no Markdown indicators
        if (ContainsMermaidKeywords(content, checkFirst10Lines: true))
        {
            return ContentType.Mermaid;
        }

        // Default to Markdown for .md files
        return ContentType.Markdown;
    }

    private bool ContainsMarkdownIndicators(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        // Check for common Markdown syntax patterns
        var patterns = new[]
        {
            @"^#{1,6}\s+",           // Headers (# ## ### etc at start of line)
            @"\*\*[^*]+\*\*",       // Bold with **
            @"__[^_]+__",            // Bold with __
            @"^\s*[-*+]\s+",         // Unordered lists
            @"^\s*\d+\.\s+",        // Ordered lists
            @"\[.+\]\(.+\)",        // Links [text](url)
            @"^>\s+",                // Blockquotes
            @"^\s*```",              // Code blocks
            @"\|.+\|",               // Tables
        };

        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
                return true;
        }

        return false;
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
        // Match: ```mermaid (with optional whitespace and newline/end of string)
        var pattern = @"```\s*mermaid\b";
        var result = Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        System.Diagnostics.Debug.WriteLine($"[ContentDetector] Mermaid block pattern match: {result}");
        if (result)
        {
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            System.Diagnostics.Debug.WriteLine($"[ContentDetector] Found at position: {match.Index}, matched text: '{match.Value}'");
        }
        return result;
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
