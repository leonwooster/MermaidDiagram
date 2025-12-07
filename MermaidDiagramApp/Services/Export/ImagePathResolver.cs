using System;
using System.IO;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Utility class for resolving image paths in Markdown documents.
/// </summary>
public class ImagePathResolver
{
    /// <summary>
    /// Resolves an image path relative to the Markdown file location.
    /// </summary>
    /// <param name="imagePath">The image path from the Markdown document.</param>
    /// <param name="markdownFilePath">The absolute path to the Markdown file.</param>
    /// <returns>The resolved absolute path to the image, or the original path if it's a URL or data URI.</returns>
    public string ResolveImagePath(string imagePath, string markdownFilePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return imagePath ?? string.Empty;
        }

        // Handle HTTP/HTTPS URLs - pass through unchanged
        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return imagePath;
        }

        // Handle data URIs - pass through unchanged
        if (imagePath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return imagePath;
        }

        // Handle absolute Windows paths (e.g., C:\path\to\image.png or \\network\path)
        if (Path.IsPathRooted(imagePath))
        {
            return Path.GetFullPath(imagePath);
        }

        // Handle relative paths - resolve relative to Markdown file directory
        if (string.IsNullOrWhiteSpace(markdownFilePath))
        {
            // If no markdown file path provided, return the image path as-is
            return imagePath;
        }

        var markdownDirectory = Path.GetDirectoryName(markdownFilePath);
        if (string.IsNullOrWhiteSpace(markdownDirectory))
        {
            return imagePath;
        }

        // Combine and normalize the path
        var combinedPath = Path.Combine(markdownDirectory, imagePath);
        return Path.GetFullPath(combinedPath);
    }

    /// <summary>
    /// Validates whether a resolved image path points to an existing file.
    /// </summary>
    /// <param name="resolvedPath">The resolved image path to validate.</param>
    /// <returns>True if the path is valid and the file exists; otherwise, false.</returns>
    public bool IsValidImagePath(string resolvedPath)
    {
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return false;
        }

        // URLs and data URIs are considered valid (we can't check their existence)
        if (resolvedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            resolvedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            resolvedPath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // For file paths, check if the file exists
        try
        {
            return File.Exists(resolvedPath);
        }
        catch
        {
            // Invalid path format or access denied
            return false;
        }
    }

    /// <summary>
    /// Detects the image format based on the file extension.
    /// </summary>
    /// <param name="imagePath">The image path.</param>
    /// <returns>The detected image format.</returns>
    public ImageFileFormat DetectImageFormat(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return ImageFileFormat.Unknown;
        }

        // Handle data URIs
        if (imagePath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            if (imagePath.Contains("image/png", StringComparison.OrdinalIgnoreCase))
                return ImageFileFormat.Png;
            if (imagePath.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                imagePath.Contains("image/jpg", StringComparison.OrdinalIgnoreCase))
                return ImageFileFormat.Jpeg;
            if (imagePath.Contains("image/gif", StringComparison.OrdinalIgnoreCase))
                return ImageFileFormat.Gif;
            if (imagePath.Contains("image/svg", StringComparison.OrdinalIgnoreCase))
                return ImageFileFormat.Svg;
            if (imagePath.Contains("image/bmp", StringComparison.OrdinalIgnoreCase))
                return ImageFileFormat.Bmp;
            if (imagePath.Contains("image/webp", StringComparison.OrdinalIgnoreCase))
                return ImageFileFormat.WebP;
            
            return ImageFileFormat.Unknown;
        }

        // Get extension from path
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();

        return extension switch
        {
            ".png" => ImageFileFormat.Png,
            ".jpg" or ".jpeg" => ImageFileFormat.Jpeg,
            ".gif" => ImageFileFormat.Gif,
            ".svg" => ImageFileFormat.Svg,
            ".bmp" => ImageFileFormat.Bmp,
            ".webp" => ImageFileFormat.WebP,
            ".tiff" or ".tif" => ImageFileFormat.Tiff,
            ".ico" => ImageFileFormat.Icon,
            _ => ImageFileFormat.Unknown
        };
    }
}

/// <summary>
/// Supported image file formats for path detection.
/// </summary>
public enum ImageFileFormat
{
    Unknown,
    Png,
    Jpeg,
    Gif,
    Svg,
    Bmp,
    WebP,
    Tiff,
    Icon
}
