using Xunit;
using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.Services.Export;
using System;
using System.IO;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Property-based tests for ImagePathResolver.
/// Feature: markdown-to-word-export, Property 18: Image path resolution
/// Validates: Requirements 5.1, 5.2
/// </summary>
public class ImagePathResolverPropertyTests
{
    private readonly ImagePathResolver _resolver;

    public ImagePathResolverPropertyTests()
    {
        _resolver = new ImagePathResolver();
    }

    /// <summary>
    /// Property: For any image reference in Markdown (relative or absolute path),
    /// the system should resolve it correctly relative to the Markdown file location
    /// or use the absolute path directly.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ResolveImagePath_WithRelativePath_ProducesAbsolutePath(string relativePath, string markdownPath)
    {
        // Arrange: Filter out invalid inputs
        if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrWhiteSpace(markdownPath))
            return;

        // Skip URLs and data URIs
        if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        // Skip absolute paths for this test
        if (Path.IsPathRooted(relativePath))
            return;

        // Ensure markdown path is absolute and valid
        if (!Path.IsPathRooted(markdownPath))
        {
            // Clean up the path and make it absolute
            var cleanPath = markdownPath.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
            markdownPath = Path.Combine(@"C:\Documents", cleanPath);
        }

        // Ensure the markdown path has a filename
        if (!markdownPath.Contains('.'))
            markdownPath = Path.Combine(markdownPath, "readme.md");

        try
        {
            // Act
            var result = _resolver.ResolveImagePath(relativePath, markdownPath);

            // Assert: Result should be an absolute path
            Assert.True(Path.IsPathRooted(result), "Result should be an absolute path");
        }
        catch (ArgumentException)
        {
            // Invalid path characters - acceptable to skip
        }
        catch (NotSupportedException)
        {
            // Invalid path format - acceptable to skip
        }
    }

    /// <summary>
    /// Property: For any absolute path, ResolveImagePath should return an absolute path.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ResolveImagePath_WithAbsolutePath_ReturnsAbsolutePath(string absolutePath)
    {
        // Arrange: Create a valid absolute path
        if (string.IsNullOrWhiteSpace(absolutePath))
            return;

        // Make it absolute if it isn't
        if (!Path.IsPathRooted(absolutePath))
            absolutePath = @"C:\" + absolutePath.TrimStart('\\', '/');

        var markdownPath = @"C:\Documents\readme.md";

        try
        {
            // Act
            var result = _resolver.ResolveImagePath(absolutePath, markdownPath);

            // Assert: Result should be an absolute path
            Assert.True(Path.IsPathRooted(result), "Result should be an absolute path");
        }
        catch (ArgumentException)
        {
            // Invalid path characters - acceptable to skip
        }
    }

    /// <summary>
    /// Property: For any HTTP/HTTPS URL, ResolveImagePath should pass it through unchanged.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ResolveImagePath_WithUrl_PassesThrough(string domain, string path)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(path))
            return;

        var url = $"https://{domain}/{path}";
        var markdownPath = @"C:\Documents\readme.md";

        // Act
        var result = _resolver.ResolveImagePath(url, markdownPath);

        // Assert: Result should be unchanged
        Assert.Equal(url, result);
    }

    /// <summary>
    /// Property: For any data URI, ResolveImagePath should pass it through unchanged.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ResolveImagePath_WithDataUri_PassesThrough(string mimeType, string data)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(mimeType) || string.IsNullOrWhiteSpace(data))
            return;

        var dataUri = $"data:{mimeType};base64,{data}";
        var markdownPath = @"C:\Documents\readme.md";

        // Act
        var result = _resolver.ResolveImagePath(dataUri, markdownPath);

        // Assert: Result should be unchanged
        Assert.Equal(dataUri, result);
    }

    /// <summary>
    /// Property: For any valid file path, IsValidImagePath should return true if the file exists.
    /// </summary>
    [Property(MaxTest = 100)]
    public void IsValidImagePath_WithExistingFile_ReturnsTrue(string content)
    {
        // Arrange: Create a temporary file
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, content ?? string.Empty);

            // Act
            var result = _resolver.IsValidImagePath(tempFile);

            // Assert
            Assert.True(result, "Existing file should be valid");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Property: For any URL or data URI, IsValidImagePath should return true
    /// (we can't verify their existence, so we trust them).
    /// </summary>
    [Property(MaxTest = 100)]
    public void IsValidImagePath_WithUrl_ReturnsTrue(string domain, string path)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(path))
            return;

        var url = $"https://{domain}/{path}";

        // Act
        var result = _resolver.IsValidImagePath(url);

        // Assert
        Assert.True(result, "URL should be considered valid");
    }

    /// <summary>
    /// Property: For any image path with .png extension, DetectImageFormat should return Png.
    /// </summary>
    [Property(MaxTest = 100)]
    public void DetectImageFormat_WithPngExtension_ReturnsPng(string filename)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(filename))
            filename = "image";

        var path = $"{filename}.png";

        // Act
        var result = _resolver.DetectImageFormat(path);

        // Assert
        Assert.Equal(ImageFileFormat.Png, result);
    }

    /// <summary>
    /// Property: For any image path with .jpg extension, DetectImageFormat should return Jpeg.
    /// </summary>
    [Property(MaxTest = 100)]
    public void DetectImageFormat_WithJpgExtension_ReturnsJpeg(string filename)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(filename))
            filename = "image";

        var path = $"{filename}.jpg";

        // Act
        var result = _resolver.DetectImageFormat(path);

        // Assert
        Assert.Equal(ImageFileFormat.Jpeg, result);
    }

    /// <summary>
    /// Property: For any data URI with image/png MIME type, DetectImageFormat should return Png.
    /// </summary>
    [Property(MaxTest = 100)]
    public void DetectImageFormat_WithPngDataUri_ReturnsPng(string data)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(data))
            data = "iVBORw0KGgo";

        var dataUri = $"data:image/png;base64,{data}";

        // Act
        var result = _resolver.DetectImageFormat(dataUri);

        // Assert
        Assert.Equal(ImageFileFormat.Png, result);
    }

    /// <summary>
    /// Property: Resolving a path should be idempotent - resolving an already resolved path
    /// should return the same result.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ResolveImagePath_IsIdempotent(string relativePath)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        // Skip URLs and data URIs
        if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        // Skip absolute paths
        if (Path.IsPathRooted(relativePath))
            return;

        var markdownPath = @"C:\Documents\readme.md";

        try
        {
            // Act
            var firstResolve = _resolver.ResolveImagePath(relativePath, markdownPath);
            var secondResolve = _resolver.ResolveImagePath(firstResolve, markdownPath);

            // Assert: Second resolve should return the same as first (idempotent)
            Assert.Equal(firstResolve, secondResolve);
        }
        catch (ArgumentException)
        {
            // Invalid path characters - acceptable to skip
        }
    }
}
