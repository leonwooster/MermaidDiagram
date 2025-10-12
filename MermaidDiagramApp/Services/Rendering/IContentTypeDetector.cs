using System;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services.Rendering;

/// <summary>
/// Interface for detecting content type from file content and metadata.
/// </summary>
public interface IContentTypeDetector
{
    /// <summary>
    /// Detects the content type based on content and file extension.
    /// </summary>
    /// <param name="content">The file content to analyze.</param>
    /// <param name="fileExtension">The file extension (e.g., ".md", ".mmd").</param>
    /// <returns>The detected content type.</returns>
    ContentType DetectContentType(string content, string fileExtension);

    /// <summary>
    /// Registers a custom detection rule for a specific file extension.
    /// </summary>
    /// <param name="extension">The file extension.</param>
    /// <param name="rule">The detection rule function.</param>
    void RegisterDetectionRule(string extension, Func<string, ContentType> rule);

    /// <summary>
    /// Clears the detection cache.
    /// </summary>
    void ClearCache();
}
