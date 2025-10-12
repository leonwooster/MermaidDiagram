using System;
using System.Collections.Generic;

namespace MermaidDiagramApp.Models;

/// <summary>
/// Represents the result of a rendering operation.
/// </summary>
public class RenderingResult
{
    public bool Success { get; set; }
    public string RenderedContent { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public ContentType DetectedContentType { get; set; }
    public TimeSpan RenderDuration { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static RenderingResult SuccessResult(string content, ContentType contentType)
    {
        return new RenderingResult
        {
            Success = true,
            RenderedContent = content,
            DetectedContentType = contentType
        };
    }

    public static RenderingResult ErrorResult(string errorMessage, ContentType contentType = ContentType.Unknown)
    {
        return new RenderingResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            DetectedContentType = contentType
        };
    }
}
