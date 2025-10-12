using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services.Rendering;

/// <summary>
/// Orchestrates the content rendering pipeline: detection, renderer selection, and rendering.
/// Keeps MainWindow decoupled from rendering implementation details.
/// </summary>
public class RenderingOrchestrator
{
    private readonly IContentTypeDetector _contentTypeDetector;
    private readonly ContentRendererFactory _rendererFactory;

    public event EventHandler<RenderingStateChangedEventArgs>? RenderingStateChanged;

    public RenderingOrchestrator(IContentTypeDetector contentTypeDetector, ContentRendererFactory rendererFactory)
    {
        _contentTypeDetector = contentTypeDetector ?? throw new ArgumentNullException(nameof(contentTypeDetector));
        _rendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
    }

    /// <summary>
    /// Renders content by automatically detecting type and selecting appropriate renderer.
    /// </summary>
    public async Task<RenderingResult> RenderAsync(string content, IRenderingContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Notify rendering started
            OnRenderingStateChanged(RenderingState.Started, null);

            // Detect content type (or use forced type if specified)
            var contentType = context.ForcedContentType 
                ?? _contentTypeDetector.DetectContentType(content, context.FileExtension);

            if (contentType == ContentType.Unknown)
            {
                var result = RenderingResult.ErrorResult("Unable to detect content type", ContentType.Unknown);
                OnRenderingStateChanged(RenderingState.Failed, result);
                return result;
            }

            // Get appropriate renderer
            var renderer = _rendererFactory.GetRenderer(contentType);
            if (renderer == null)
            {
                var result = RenderingResult.ErrorResult($"No renderer available for content type: {contentType}", contentType);
                OnRenderingStateChanged(RenderingState.Failed, result);
                return result;
            }

            // Render content
            var renderResult = await renderer.RenderAsync(content, context);
            renderResult.DetectedContentType = contentType;
            renderResult.RenderDuration = stopwatch.Elapsed;

            // Notify rendering completed
            if (renderResult.Success)
            {
                OnRenderingStateChanged(RenderingState.Completed, renderResult);
            }
            else
            {
                OnRenderingStateChanged(RenderingState.Failed, renderResult);
            }

            return renderResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorResult = RenderingResult.ErrorResult($"Rendering orchestration failed: {ex.Message}", ContentType.Unknown);
            OnRenderingStateChanged(RenderingState.Failed, errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// Gets the renderer for a specific content type.
    /// </summary>
    public IContentRenderer? GetRenderer(ContentType contentType)
    {
        return _rendererFactory.GetRenderer(contentType);
    }

    /// <summary>
    /// Detects content type without rendering.
    /// </summary>
    public ContentType DetectContentType(string content, string fileExtension)
    {
        return _contentTypeDetector.DetectContentType(content, fileExtension);
    }

    private void OnRenderingStateChanged(RenderingState state, RenderingResult? result)
    {
        RenderingStateChanged?.Invoke(this, new RenderingStateChangedEventArgs(state, result));
    }
}

/// <summary>
/// Event args for rendering state changes.
/// </summary>
public class RenderingStateChangedEventArgs : EventArgs
{
    public RenderingState State { get; }
    public RenderingResult? Result { get; }

    public RenderingStateChangedEventArgs(RenderingState state, RenderingResult? result)
    {
        State = state;
        Result = result;
    }
}

/// <summary>
/// Rendering state enumeration.
/// </summary>
public enum RenderingState
{
    Started,
    Completed,
    Failed
}
