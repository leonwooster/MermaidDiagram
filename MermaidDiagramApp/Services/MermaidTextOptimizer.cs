using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Automatically optimizes Mermaid diagram text to prevent overflow issues.
/// Detects long node labels and subgraph titles, then adds line breaks to improve readability.
/// </summary>
public class MermaidTextOptimizer
{
    private readonly ILogger _logger;
    
    // Configuration thresholds
    private const int MaxLabelLength = 40; // Characters before considering line break
    private const int MaxSubgraphTitleLength = 20; // Characters for subgraph titles (reduced from 30)
    private const int PreferredLineLength = 20; // Target length per line (reduced from 25)
    
    public MermaidTextOptimizer(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Optimizes Mermaid diagram content by adding line breaks to long labels.
    /// </summary>
    /// <param name="mermaidContent">The original Mermaid diagram content</param>
    /// <returns>Optimized Mermaid content with line breaks added where needed</returns>
    public string OptimizeDiagram(string mermaidContent)
    {
        if (string.IsNullOrWhiteSpace(mermaidContent))
        {
            return mermaidContent;
        }

        try
        {
            var optimizedContent = mermaidContent;
            int changesCount = 0;

            // 1. Optimize subgraph titles
            var subgraphResult = OptimizeSubgraphTitles(optimizedContent);
            optimizedContent = subgraphResult.content;
            changesCount += subgraphResult.changes;

            // 2. Optimize node labels (in square brackets, quotes, or parentheses)
            var nodeResult = OptimizeNodeLabels(optimizedContent);
            optimizedContent = nodeResult.content;
            changesCount += nodeResult.changes;

            if (changesCount > 0)
            {
                _logger.LogInformation($"Mermaid text optimizer: Applied {changesCount} optimizations");
            }

            return optimizedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error optimizing Mermaid diagram: {ex.Message}", ex);
            return mermaidContent; // Return original on error
        }
    }

    /// <summary>
    /// Optimizes subgraph titles by adding line breaks.
    /// Pattern: subgraph Title["Long Title Here"]
    /// </summary>
    private (string content, int changes) OptimizeSubgraphTitles(string content)
    {
        int changesCount = 0;
        
        // Match: subgraph identifier["title"] or subgraph identifier[title]
        var pattern = @"subgraph\s+(\w+)\[""([^""]+)""\]";
        
        var result = Regex.Replace(content, pattern, match =>
        {
            var identifier = match.Groups[1].Value;
            var title = match.Groups[2].Value;

            if (title.Length > MaxSubgraphTitleLength && !title.Contains("<br/>"))
            {
                var optimizedTitle = AddLineBreaks(title, PreferredLineLength);
                if (optimizedTitle != title)
                {
                    changesCount++;
                    _logger.LogDebug($"Optimized subgraph title: '{title}' -> '{optimizedTitle}'");
                    return $"subgraph {identifier}[\"{optimizedTitle}\"]";
                }
            }

            return match.Value;
        });
        
        return (result, changesCount);
    }

    /// <summary>
    /// Optimizes node labels in various formats.
    /// Patterns: 
    /// - node["label"]
    /// - node("label")
    /// - node>"label"]
    /// - node{{"label"}}
    /// </summary>
    private (string content, int changes) OptimizeNodeLabels(string content)
    {
        int changesCount = 0;
        
        // Match node definitions with labels in quotes
        // Pattern: nodeId["label text"] or nodeId("label text") or nodeId{{"label"}}
        var patterns = new[]
        {
            @"(\w+)\[""([^""]+)""\]",  // Square brackets with quotes
            @"(\w+)\(""([^""]+)""\)",  // Parentheses with quotes
            @"(\w+)\{\{""([^""]+)""\}\}",  // Double curly braces
            @"(\w+)>""([^""]+)""\]",  // Asymmetric shape
        };

        var result = content;

        foreach (var pattern in patterns)
        {
            result = Regex.Replace(result, pattern, match =>
            {
                var nodeId = match.Groups[1].Value;
                var label = match.Groups[2].Value;

                // Check if label is too long and doesn't already have line breaks
                if (label.Length > MaxLabelLength && !label.Contains("<br/>"))
                {
                    var optimizedLabel = AddLineBreaks(label, PreferredLineLength);
                    if (optimizedLabel != label)
                    {
                        changesCount++;
                        _logger.LogDebug($"Optimized node label: '{label}' -> '{optimizedLabel}'");
                        
                        // Reconstruct the node definition with optimized label
                        var brackets = GetBracketStyle(match.Value);
                        return $"{nodeId}{brackets.open}\"{optimizedLabel}\"{brackets.close}";
                    }
                }

                return match.Value;
            });
        }

        return (result, changesCount);
    }

    /// <summary>
    /// Adds line breaks to long text at natural break points.
    /// Tries to break at spaces, parentheses, or other punctuation.
    /// Also handles special cases like port numbers in parentheses.
    /// </summary>
    private string AddLineBreaks(string text, int targetLength)
    {
        if (text.Length <= targetLength)
        {
            return text;
        }

        // Special handling for common patterns like "Service Name (Port XXXX)"
        // Break before the parenthesis to keep port info on separate line
        var portPattern = @"^(.+?)\s*(\(Port\s+\d+\))(.*)$";
        var portMatch = Regex.Match(text, portPattern, RegexOptions.IgnoreCase);
        if (portMatch.Success)
        {
            var mainText = portMatch.Groups[1].Value.Trim();
            var portInfo = portMatch.Groups[2].Value.Trim();
            var remaining = portMatch.Groups[3].Value.Trim();
            
            var parts = new List<string>();
            
            // Break main text if still too long
            if (mainText.Length > targetLength)
            {
                parts.AddRange(BreakIntoLines(mainText, targetLength));
            }
            else
            {
                parts.Add(mainText);
            }
            
            // Add port info on its own line
            parts.Add(portInfo);
            
            // Add remaining text if any
            if (!string.IsNullOrEmpty(remaining))
            {
                if (remaining.Length > targetLength)
                {
                    parts.AddRange(BreakIntoLines(remaining, targetLength));
                }
                else
                {
                    parts.Add(remaining);
                }
            }
            
            return string.Join("<br/>", parts);
        }

        // Default word-based breaking
        return string.Join("<br/>", BreakIntoLines(text, targetLength));
    }

    /// <summary>
    /// Breaks text into lines at word boundaries.
    /// </summary>
    private List<string> BreakIntoLines(string text, int targetLength)
    {
        var lines = new List<string>();
        var currentLine = "";
        var words = text.Split(' ');

        foreach (var word in words)
        {
            // Check if adding this word would exceed target length
            var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";

            if (testLine.Length > targetLength && !string.IsNullOrEmpty(currentLine))
            {
                // Current line is full, start a new line
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        // Add the last line
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    /// <summary>
    /// Determines the bracket style used in a node definition.
    /// </summary>
    private (string open, string close) GetBracketStyle(string nodeDefinition)
    {
        if (nodeDefinition.Contains("[["))
            return ("[[", "]]");
        if (nodeDefinition.Contains("{{"))
            return ("{{", "}}");
        if (nodeDefinition.Contains("[("))
            return ("[(", ")]");
        if (nodeDefinition.Contains(">"))
            return (">", "]");
        if (nodeDefinition.Contains("("))
            return ("(", ")");
        if (nodeDefinition.Contains("{"))
            return ("{", "}");
        
        return ("[", "]"); // Default to square brackets
    }

    /// <summary>
    /// Checks if a diagram would benefit from optimization.
    /// </summary>
    public bool NeedsOptimization(string mermaidContent)
    {
        if (string.IsNullOrWhiteSpace(mermaidContent))
        {
            return false;
        }

        // Check for long labels without line breaks
        var hasLongLabels = Regex.IsMatch(mermaidContent, 
            $@"\[""[^""]{{{MaxLabelLength},}}[^<br/>]+""\]");
        
        var hasLongSubgraphTitles = Regex.IsMatch(mermaidContent,
            $@"subgraph\s+\w+\[""[^""]{{{MaxSubgraphTitleLength},}}[^<br/>]+""\]");

        return hasLongLabels || hasLongSubgraphTitles;
    }
}
