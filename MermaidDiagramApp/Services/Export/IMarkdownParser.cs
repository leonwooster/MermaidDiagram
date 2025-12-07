using Markdig.Syntax;
using System.Collections.Generic;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Interface for parsing Markdown content and extracting specific elements.
/// </summary>
public interface IMarkdownParser
{
    /// <summary>
    /// Parses Markdown content into a structured document.
    /// </summary>
    /// <param name="markdownContent">The Markdown content to parse.</param>
    /// <returns>A parsed Markdown document.</returns>
    MarkdownDocument Parse(string markdownContent);

    /// <summary>
    /// Extracts all Mermaid code blocks from a parsed Markdown document.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    /// <returns>A collection of Mermaid blocks.</returns>
    IEnumerable<MermaidBlock> ExtractMermaidBlocks(MarkdownDocument document);

    /// <summary>
    /// Extracts all image references from a parsed Markdown document.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    /// <returns>A collection of image references.</returns>
    IEnumerable<ImageReference> ExtractImageReferences(MarkdownDocument document);
}
