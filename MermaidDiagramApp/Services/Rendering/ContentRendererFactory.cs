using System;
using System.Collections.Generic;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services.Rendering;

/// <summary>
/// Factory for creating appropriate content renderer instances based on content type.
/// </summary>
public class ContentRendererFactory
{
    private readonly Dictionary<ContentType, IContentRenderer> _renderers = new();

    public ContentRendererFactory()
    {
        // Register default renderers
        RegisterRenderer(new MermaidRenderer());
        RegisterRenderer(new MarkdownRenderer());
    }

    /// <summary>
    /// Registers a renderer for a specific content type.
    /// </summary>
    public void RegisterRenderer(IContentRenderer renderer)
    {
        if (renderer == null)
            throw new ArgumentNullException(nameof(renderer));

        _renderers[renderer.SupportedType] = renderer;
    }

    /// <summary>
    /// Gets the appropriate renderer for the specified content type.
    /// </summary>
    public IContentRenderer? GetRenderer(ContentType contentType)
    {
        // Handle MarkdownWithMermaid - use MarkdownRenderer
        if (contentType == ContentType.MarkdownWithMermaid)
        {
            return _renderers.GetValueOrDefault(ContentType.Markdown);
        }

        return _renderers.GetValueOrDefault(contentType);
    }

    /// <summary>
    /// Gets all registered renderers.
    /// </summary>
    public IEnumerable<IContentRenderer> GetAllRenderers()
    {
        return _renderers.Values;
    }
}
