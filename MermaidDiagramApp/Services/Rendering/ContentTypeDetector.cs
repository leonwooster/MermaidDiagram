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
        // CONTENT-FIRST DETECTION: Analyze content patterns before considering file extension
        // This ensures pasted content is correctly detected regardless of file type
        
        var hasMermaidBlocks = ContainsMermaidCodeBlocks(content);
        var hasMarkdownIndicators = ContainsMarkdownIndicators(content);
        var hasMermaidKeywords = ContainsMermaidKeywords(content, checkFirst10Lines: true);
        
        System.Diagnostics.Debug.WriteLine($"[ContentDetector] Analysis - MermaidBlocks: {hasMermaidBlocks}, MarkdownIndicators: {hasMarkdownIndicators}, MermaidKeywords: {hasMermaidKeywords}, Extension: '{fileExtension}'");
        
        // Priority 1: Mermaid code blocks (```mermaid) = MarkdownWithMermaid
        // This is the strongest indicator - explicit Mermaid blocks in markdown
        if (hasMermaidBlocks)
        {
            System.Diagnostics.Debug.WriteLine($"[ContentDetector] Detected MarkdownWithMermaid (explicit mermaid code blocks)");
            return ContentType.MarkdownWithMermaid;
        }
        
        // Priority 2: Markdown indicators = Markdown or MarkdownWithMermaid
        // If content has markdown syntax (headers, lists, tables, etc.)
        if (hasMarkdownIndicators)
        {
            // Check if there are also Mermaid keywords in the content
            // This handles cases where Mermaid diagrams are mixed with markdown but not in code blocks
            if (hasMermaidKeywords)
            {
                System.Diagnostics.Debug.WriteLine($"[ContentDetector] Detected MarkdownWithMermaid (markdown + mermaid keywords)");
                return ContentType.MarkdownWithMermaid;
            }
            
            System.Diagnostics.Debug.WriteLine($"[ContentDetector] Detected Markdown (has markdown indicators)");
            return ContentType.Markdown;
        }
        
        // Priority 3: Pure Mermaid keywords at start of content
        // If content starts with Mermaid diagram syntax (no markdown formatting)
        if (hasMermaidKeywords)
        {
            System.Diagnostics.Debug.WriteLine($"[ContentDetector] Detected Mermaid (pure mermaid syntax)");
            return ContentType.Mermaid;
        }
        
        // Priority 4: File extension hints (only as fallback)
        // Use file extension only when content analysis is inconclusive
        if (!string.IsNullOrEmpty(fileExtension))
        {
            if (fileExtension == "mmd")
            {
                System.Diagnostics.Debug.WriteLine($"[ContentDetector] Defaulting to Mermaid (mmd extension, no clear content indicators)");
                return ContentType.Mermaid;
            }
            
            if (fileExtension == "md" || fileExtension == "markdown")
            {
                System.Diagnostics.Debug.WriteLine($"[ContentDetector] Defaulting to Markdown (md extension, no clear content indicators)");
                return ContentType.Markdown;
            }
        }
        
        // Priority 5: Final fallback
        // When pasting content with no file extension and no clear indicators, default to Markdown
        // Markdown is more forgiving and can still render plain text
        System.Diagnostics.Debug.WriteLine($"[ContentDetector] Defaulting to Markdown (no clear indicators)");
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
