using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Parses Mermaid code to extract element definitions and their line numbers
    /// </summary>
    public class MermaidCodeParser
    {
        public class MermaidElement
        {
            public string Id { get; set; } = string.Empty;
            public int LineNumber { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
        }

        public List<MermaidElement> ParseCode(string code, bool isMarkdown = false)
        {
            var elements = new List<MermaidElement>();
            if (string.IsNullOrEmpty(code))
                return elements;

            // Check if this is a Markdown file
            var mermaidBlocks = ExtractMermaidBlocks(code);
            
            if (mermaidBlocks.Count > 0 || isMarkdown)
            {
                // Parse Markdown structure for scroll sync
                var markdownElements = ParseMarkdownStructure(code);
                elements.AddRange(markdownElements);
                
                // Also parse Mermaid blocks if present
                foreach (var block in mermaidBlocks)
                {
                    var blockElements = ParseMermaidCode(block.Code, block.StartLine);
                    elements.AddRange(blockElements);
                }
            }
            else
            {
                // Parse as pure Mermaid code
                var pureElements = ParseMermaidCode(code, 0);
                elements.AddRange(pureElements);
            }

            return elements;
        }

        private List<MermaidElement> ParseMarkdownStructure(string markdown)
        {
            var elements = new List<MermaidElement>();
            if (string.IsNullOrEmpty(markdown))
                return elements;

            var lines = markdown.Split('\n');
            bool inCodeBlock = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int lineNumber = i + 1; // 1-based line numbers

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var trimmedLine = line.TrimStart();
                
                // Detect code block boundaries
                if (trimmedLine.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    // Don't parse the ``` line itself or content inside code blocks
                    continue;
                }
                
                // Skip lines inside code blocks
                if (inCodeBlock)
                {
                    continue;
                }

                // Parse headings (# H1, ## H2, etc.)
                if (trimmedLine.StartsWith("#"))
                {
                    var match = Regex.Match(trimmedLine, @"^(#{1,6})\s+(.+)$");
                    if (match.Success)
                    {
                        var level = match.Groups[1].Value.Length;
                        var text = match.Groups[2].Value.Trim();
                        // Keep the original text with special characters for better matching
                        elements.Add(new MermaidElement
                        {
                            Id = $"heading-{lineNumber}",
                            LineNumber = lineNumber,
                            Type = $"h{level}",
                            Label = text
                        });
                    }
                }
                // Parse list items
                else if (Regex.IsMatch(trimmedLine, @"^[\*\-\+]\s+") || Regex.IsMatch(trimmedLine, @"^\d+\.\s+"))
                {
                    var listMatch = Regex.Match(trimmedLine, @"^(?:[\*\-\+]|\d+\.)\s+(.+)$");
                    var listText = listMatch.Success ? listMatch.Groups[1].Value.Trim() : trimmedLine;
                    
                    elements.Add(new MermaidElement
                    {
                        Id = $"list-{lineNumber}",
                        LineNumber = lineNumber,
                        Type = "list",
                        Label = listText
                    });
                }
                // Parse paragraphs (non-empty lines that aren't special)
                else if (!trimmedLine.StartsWith(">") &&
                         !trimmedLine.StartsWith("|") &&
                         !trimmedLine.StartsWith("---") &&
                         !trimmedLine.StartsWith("==="))
                {
                    var paraText = trimmedLine.Substring(0, Math.Min(100, trimmedLine.Length));
                    elements.Add(new MermaidElement
                    {
                        Id = $"para-{lineNumber}",
                        LineNumber = lineNumber,
                        Type = "paragraph",
                        Label = paraText
                    });
                }
            }

            return elements;
        }

        private class MermaidBlock
        {
            public string Code { get; set; } = string.Empty;
            public int StartLine { get; set; }
        }

        private List<MermaidBlock> ExtractMermaidBlocks(string markdown)
        {
            var blocks = new List<MermaidBlock>();
            var lines = markdown.Split('\n');
            bool inMermaidBlock = false;
            int blockStartLine = 0;
            var currentBlock = new System.Text.StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();

                if (line.Trim().StartsWith("```mermaid", StringComparison.OrdinalIgnoreCase))
                {
                    inMermaidBlock = true;
                    blockStartLine = i + 1; // Start from next line (skip the ```mermaid line)
                    currentBlock.Clear();
                }
                else if (inMermaidBlock && line.Trim().StartsWith("```"))
                {
                    // End of Mermaid block
                    blocks.Add(new MermaidBlock
                    {
                        Code = currentBlock.ToString(),
                        StartLine = blockStartLine
                    });
                    inMermaidBlock = false;
                    currentBlock.Clear();
                }
                else if (inMermaidBlock)
                {
                    currentBlock.AppendLine(line);
                }
            }

            return blocks;
        }

        private List<MermaidElement> ParseMermaidCode(string code, int lineOffset)
        {
            var elements = new List<MermaidElement>();
            if (string.IsNullOrEmpty(code))
                return elements;

            var lines = code.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                int lineNumber = lineOffset + i + 1; // 1-based line numbers

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("%%"))
                    continue;

                // Parse subgraphs
                if (line.StartsWith("subgraph"))
                {
                    var match = Regex.Match(line, @"subgraph\s+(\w+)(?:\s*\[(.+?)\])?");
                    if (match.Success)
                    {
                        elements.Add(new MermaidElement
                        {
                            Id = match.Groups[1].Value,
                            LineNumber = lineNumber,
                            Type = "subgraph",
                            Label = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value
                        });
                    }
                }
                // Parse nodes (various formats)
                else
                {
                    // Node formats: A[Label], A(Label), A{Label}, A((Label)), etc.
                    var nodeMatches = Regex.Matches(line, @"(\w+)[\[\(\{]([^\]\)\}]+)[\]\)\}]");
                    foreach (Match match in nodeMatches)
                    {
                        var id = match.Groups[1].Value;
                        var label = match.Groups[2].Value;
                        
                        // Avoid duplicates
                        if (!elements.Exists(e => e.Id == id))
                        {
                            elements.Add(new MermaidElement
                            {
                                Id = id,
                                LineNumber = lineNumber,
                                Type = "node",
                                Label = label
                            });
                        }
                    }
                }
            }

            return elements;
        }

        public string GenerateLineNumberInjectionScript(List<MermaidElement> elements)
        {
            var elementsJson = System.Text.Json.JsonSerializer.Serialize(
                elements.ConvertAll(e => new { e.Id, e.LineNumber, e.Type, e.Label })
            );
            
            var script = $@"
(function() {{
    try {{
        console.log('Injecting line number markers...');
        
        const elementMap = {elementsJson};
        console.log('Element map:', elementMap);
        
        let injectedCount = 0;
        
        // Check if we're in Markdown mode or Mermaid mode
        const container = document.getElementById('content-container');
        const isMermaidMode = container && container.classList.contains('mermaid-mode');
        const isMarkdownMode = container && container.classList.contains('markdown-mode');
        
        if (isMermaidMode) {{
            // Inject into SVG elements (Mermaid diagrams)
            const svg = document.querySelector('svg');
            if (!svg) {{
                console.log('No SVG found');
                return;
            }}
            
            elementMap.forEach(item => {{
                let svgElement = null;
                
                if (item.Type === 'subgraph') {{
                    const clusters = svg.querySelectorAll('g.cluster');
                    clusters.forEach(cluster => {{
                        const title = cluster.querySelector('title');
                        if (title && title.textContent.includes(item.Id)) {{
                            svgElement = cluster;
                        }}
                    }});
                }} else if (item.Type === 'node') {{
                    // Try to find the node by ID
                    let nodes = svg.querySelectorAll('[id*=""' + item.Id + '""]');
                    
                    if (nodes.length === 0) {{
                        const nodeGroups = svg.querySelectorAll('g.node');
                        nodeGroups.forEach(group => {{
                            const label = group.querySelector('text');
                            if (label && label.textContent.includes(item.Id)) {{
                                nodes = [group];
                            }}
                        }});
                    }}
                    
                    if (nodes.length > 0) {{
                        svgElement = nodes[0];
                    }}
                }}
                
                if (svgElement) {{
                    svgElement.setAttribute('data-line', item.LineNumber);
                    svgElement.style.cursor = 'pointer';
                    injectedCount++;
                    console.log('Injected marker for:', item.Id, 'at line', item.LineNumber);
                }} else {{
                    console.log('Could not find SVG element for:', item.Id);
                }}
            }});
        }} else if (isMarkdownMode) {{
            // Inject into HTML elements (Markdown content) - OPTIMIZED VERSION
            const markdownBody = document.querySelector('.markdown-body');
            if (!markdownBody) {{
                console.log('No markdown body found');
                return;
            }}
            
            // Helper function to normalize text for comparison
            function normalizeText(text) {{
                return text
                    .trim()
                    .toLowerCase()
                    .replace(/\s+/g, ' ')
                    .replace(/`/g, '')  // Remove backticks
                    .replace(/\*/g, '')  // Remove asterisks (bold/italic)
                    .replace(/_/g, '')   // Remove underscores (bold/italic)
                    .replace(/\[|\]/g, '') // Remove brackets
                    .replace(/\(|\)/g, '') // Remove parentheses from links
                    .replace(/[^\w\s\-:]/g, ''); // Remove other special chars except word chars, spaces, hyphens, colons
            }}
            
            // OPTIMIZATION: Pre-collect all elements by type into arrays with normalized text
            const elementsByType = {{
                h1: [],
                h2: [],
                h3: [],
                h4: [],
                h5: [],
                h6: [],
                paragraph: [],
                list: []
            }};
            
            // Collect headings
            for (let i = 1; i <= 6; i++) {{
                const headings = markdownBody.querySelectorAll('h' + i);
                elementsByType['h' + i] = Array.from(headings).map(el => ({{
                    element: el,
                    normalizedText: normalizeText(el.textContent),
                    used: false
                }}));
            }}
            
            // Collect paragraphs
            const paragraphs = markdownBody.querySelectorAll('p');
            elementsByType.paragraph = Array.from(paragraphs).map(el => ({{
                element: el,
                normalizedText: normalizeText(el.textContent),
                used: false
            }}));
            
            // Collect list items
            const listItems = markdownBody.querySelectorAll('li');
            elementsByType.list = Array.from(listItems).map(el => ({{
                element: el,
                normalizedText: normalizeText(el.textContent),
                used: false
            }}));
            
            // OPTIMIZATION: Now match in O(n*m) where m is much smaller (elements of same type)
            elementMap.forEach(item => {{
                let htmlElement = null;
                const normalizedLabel = normalizeText(item.Label);
                
                // Get the appropriate array based on type
                let candidates = null;
                if (item.Type.startsWith('h')) {{
                    candidates = elementsByType[item.Type];
                }} else if (item.Type === 'paragraph') {{
                    candidates = elementsByType.paragraph;
                }} else if (item.Type === 'list') {{
                    candidates = elementsByType.list;
                }}
                
                if (candidates) {{
                    // Find first unused element that matches
                    for (let candidate of candidates) {{
                        if (!candidate.used) {{
                            // Try exact match first
                            if (candidate.normalizedText === normalizedLabel) {{
                                htmlElement = candidate.element;
                                candidate.used = true;
                                break;
                            }}
                            // Then try contains match (both directions)
                            if (candidate.normalizedText.includes(normalizedLabel) || 
                                normalizedLabel.includes(candidate.normalizedText)) {{
                                htmlElement = candidate.element;
                                candidate.used = true;
                                break;
                            }}
                            // Finally try matching first significant words (for complex formatted text)
                            const labelWords = normalizedLabel.split(' ').filter(w => w.length > 3);
                            const candidateWords = candidate.normalizedText.split(' ').filter(w => w.length > 3);
                            if (labelWords.length > 0 && candidateWords.length > 0) {{
                                // Check if first 2-3 significant words match
                                const wordsToCheck = Math.min(3, labelWords.length, candidateWords.length);
                                let matchCount = 0;
                                for (let i = 0; i < wordsToCheck; i++) {{
                                    if (labelWords[i] === candidateWords[i]) {{
                                        matchCount++;
                                    }}
                                }}
                                if (matchCount >= Math.min(2, wordsToCheck)) {{
                                    htmlElement = candidate.element;
                                    candidate.used = true;
                                    break;
                                }}
                            }}
                        }}
                    }}
                }}
                
                if (htmlElement) {{
                    htmlElement.setAttribute('data-line', item.LineNumber);
                    htmlElement.style.cursor = 'pointer';
                    injectedCount++;
                    console.log('Injected marker for:', item.Type, 'at line', item.LineNumber, 'label:', item.Label);
                }} else {{
                    console.log('Could not find HTML element for:', item.Type, 'at line', item.LineNumber, 'label:', item.Label);
                }}
            }});
        }}
        
        console.log('Line number injection complete. Injected:', injectedCount, 'of', elementMap.length);
    }} catch (error) {{
        console.error('Error injecting line numbers:', error);
    }}
}})();
";
            return script;
        }
    }
}
