using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;
using System.Linq;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Implementation of IMarkdownParser using the Markdig library.
/// </summary>
public class MarkdigMarkdownParser : IMarkdownParser
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdigMarkdownParser()
    {
        // Create a Markdig pipeline with common extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <summary>
    /// Parses Markdown content into a structured document.
    /// </summary>
    /// <param name="markdownContent">The Markdown content to parse.</param>
    /// <returns>A parsed Markdown document.</returns>
    public MarkdownDocument Parse(string markdownContent)
    {
        if (string.IsNullOrEmpty(markdownContent))
        {
            return new MarkdownDocument();
        }

        return Markdown.Parse(markdownContent, _pipeline);
    }

    /// <summary>
    /// Extracts all Mermaid code blocks from a parsed Markdown document.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    /// <returns>A collection of Mermaid blocks.</returns>
    public IEnumerable<MermaidBlock> ExtractMermaidBlocks(MarkdownDocument document)
    {
        if (document == null)
        {
            yield break;
        }

        foreach (var block in document.Descendants<FencedCodeBlock>())
        {
            // Check if this is a Mermaid code block
            var info = block.Info?.ToLowerInvariant();
            if (info == "mermaid")
            {
                yield return new MermaidBlock
                {
                    Code = ExtractCodeFromBlock(block),
                    LineNumber = block.Line + 1 // Convert to 1-based line number
                };
            }
        }
    }

    /// <summary>
    /// Extracts all image references from a parsed Markdown document.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    /// <returns>A collection of image references.</returns>
    public IEnumerable<ImageReference> ExtractImageReferences(MarkdownDocument document)
    {
        if (document == null)
        {
            yield break;
        }

        // Find all LinkInline elements that are images
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (link.IsImage)
            {
                yield return new ImageReference
                {
                    OriginalPath = link.Url ?? string.Empty,
                    AltText = ExtractAltText(link),
                    LineNumber = link.Line + 1 // Convert to 1-based line number
                };
            }
        }
    }

    /// <summary>
    /// Extracts the code content from a fenced code block.
    /// </summary>
    private string ExtractCodeFromBlock(FencedCodeBlock block)
    {
        if (block.Lines.Lines == null || block.Lines.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        for (int i = 0; i < block.Lines.Count; i++)
        {
            var line = block.Lines.Lines[i];
            lines.Add(line.Slice.ToString());
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Extracts the alt text from an image link.
    /// </summary>
    private string ExtractAltText(LinkInline link)
    {
        if (link.FirstChild == null)
        {
            return string.Empty;
        }

        var textParts = new List<string>();
        foreach (var child in link)
        {
            if (child is LiteralInline literal)
            {
                textParts.Add(literal.Content.ToString());
            }
        }

        return string.Join("", textParts);
    }
}
