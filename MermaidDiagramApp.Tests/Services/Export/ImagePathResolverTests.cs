using Xunit;
using MermaidDiagramApp.Services.Export;
using System;
using System.IO;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Unit tests for ImagePathResolver.
/// Tests path resolution for relative paths, absolute paths, URLs, and invalid paths.
/// Requirements: 5.1, 5.2
/// </summary>
public class ImagePathResolverTests
{
    private readonly ImagePathResolver _resolver;

    public ImagePathResolverTests()
    {
        _resolver = new ImagePathResolver();
    }

    #region ResolveImagePath Tests

    [Fact]
    public void ResolveImagePath_WithRelativePath_ResolvesCorrectly()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        var imagePath = "images/diagram.png";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal(@"C:\Documents\images\diagram.png", result);
    }

    [Fact]
    public void ResolveImagePath_WithRelativeParentPath_ResolvesCorrectly()
    {
        // Arrange
        var markdownPath = @"C:\Documents\subfolder\readme.md";
        var imagePath = "../images/diagram.png";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal(@"C:\Documents\images\diagram.png", result);
    }

    [Fact]
    public void ResolveImagePath_WithAbsolutePath_ReturnsAbsolutePath()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        var imagePath = @"D:\Images\diagram.png";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal(@"D:\Images\diagram.png", result);
    }

    [Fact]
    public void ResolveImagePath_WithHttpUrl_PassesThrough()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        var imagePath = "http://example.com/image.png";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal("http://example.com/image.png", result);
    }

    [Fact]
    public void ResolveImagePath_WithHttpsUrl_PassesThrough()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        var imagePath = "https://example.com/image.png";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal("https://example.com/image.png", result);
    }

    [Fact]
    public void ResolveImagePath_WithDataUri_PassesThrough()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        var imagePath = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal(imagePath, result);
    }

    [Fact]
    public void ResolveImagePath_WithEmptyPath_ReturnsEmpty()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        var imagePath = "";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ResolveImagePath_WithNullPath_ReturnsEmpty()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        string? imagePath = null;

        // Act
        var result = _resolver.ResolveImagePath(imagePath!, markdownPath);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ResolveImagePath_WithWhitespacePath_ReturnsWhitespace()
    {
        // Arrange
        var markdownPath = @"C:\Documents\readme.md";
        var imagePath = "   ";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void ResolveImagePath_WithEmptyMarkdownPath_ReturnsImagePathAsIs()
    {
        // Arrange
        var markdownPath = "";
        var imagePath = "images/diagram.png";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal("images/diagram.png", result);
    }

    [Fact]
    public void ResolveImagePath_WithUncPath_ResolvesCorrectly()
    {
        // Arrange
        var markdownPath = @"\\server\share\readme.md";
        var imagePath = "images/diagram.png";

        // Act
        var result = _resolver.ResolveImagePath(imagePath, markdownPath);

        // Assert
        Assert.Equal(@"\\server\share\images\diagram.png", result);
    }

    #endregion

    #region IsValidImagePath Tests

    [Fact]
    public void IsValidImagePath_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            var result = _resolver.IsValidImagePath(tempFile);

            // Assert
            Assert.True(result);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void IsValidImagePath_WithNonExistingFile_ReturnsFalse()
    {
        // Arrange
        var nonExistingPath = @"C:\NonExisting\Path\image.png";

        // Act
        var result = _resolver.IsValidImagePath(nonExistingPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidImagePath_WithHttpUrl_ReturnsTrue()
    {
        // Arrange
        var url = "http://example.com/image.png";

        // Act
        var result = _resolver.IsValidImagePath(url);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidImagePath_WithHttpsUrl_ReturnsTrue()
    {
        // Arrange
        var url = "https://example.com/image.png";

        // Act
        var result = _resolver.IsValidImagePath(url);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidImagePath_WithDataUri_ReturnsTrue()
    {
        // Arrange
        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = _resolver.IsValidImagePath(dataUri);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidImagePath_WithEmptyPath_ReturnsFalse()
    {
        // Arrange
        var path = "";

        // Act
        var result = _resolver.IsValidImagePath(path);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidImagePath_WithNullPath_ReturnsFalse()
    {
        // Arrange
        string? path = null;

        // Act
        var result = _resolver.IsValidImagePath(path!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidImagePath_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange
        var path = @"C:\Invalid<>Path\image.png";

        // Act
        var result = _resolver.IsValidImagePath(path);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DetectImageFormat Tests

    [Theory]
    [InlineData("image.png", ImageFileFormat.Png)]
    [InlineData("image.PNG", ImageFileFormat.Png)]
    [InlineData("image.jpg", ImageFileFormat.Jpeg)]
    [InlineData("image.jpeg", ImageFileFormat.Jpeg)]
    [InlineData("image.JPG", ImageFileFormat.Jpeg)]
    [InlineData("image.gif", ImageFileFormat.Gif)]
    [InlineData("image.svg", ImageFileFormat.Svg)]
    [InlineData("image.bmp", ImageFileFormat.Bmp)]
    [InlineData("image.webp", ImageFileFormat.WebP)]
    [InlineData("image.tiff", ImageFileFormat.Tiff)]
    [InlineData("image.tif", ImageFileFormat.Tiff)]
    [InlineData("image.ico", ImageFileFormat.Icon)]
    public void DetectImageFormat_WithKnownExtension_ReturnsCorrectFormat(string imagePath, ImageFileFormat expectedFormat)
    {
        // Act
        var result = _resolver.DetectImageFormat(imagePath);

        // Assert
        Assert.Equal(expectedFormat, result);
    }

    [Fact]
    public void DetectImageFormat_WithUnknownExtension_ReturnsUnknown()
    {
        // Arrange
        var imagePath = "image.xyz";

        // Act
        var result = _resolver.DetectImageFormat(imagePath);

        // Assert
        Assert.Equal(ImageFileFormat.Unknown, result);
    }

    [Fact]
    public void DetectImageFormat_WithNoExtension_ReturnsUnknown()
    {
        // Arrange
        var imagePath = "image";

        // Act
        var result = _resolver.DetectImageFormat(imagePath);

        // Assert
        Assert.Equal(ImageFileFormat.Unknown, result);
    }

    [Fact]
    public void DetectImageFormat_WithEmptyPath_ReturnsUnknown()
    {
        // Arrange
        var imagePath = "";

        // Act
        var result = _resolver.DetectImageFormat(imagePath);

        // Assert
        Assert.Equal(ImageFileFormat.Unknown, result);
    }

    [Fact]
    public void DetectImageFormat_WithDataUriPng_ReturnsPng()
    {
        // Arrange
        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = _resolver.DetectImageFormat(dataUri);

        // Assert
        Assert.Equal(ImageFileFormat.Png, result);
    }

    [Fact]
    public void DetectImageFormat_WithDataUriJpeg_ReturnsJpeg()
    {
        // Arrange
        var dataUri = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCwAA8A/9k=";

        // Act
        var result = _resolver.DetectImageFormat(dataUri);

        // Assert
        Assert.Equal(ImageFileFormat.Jpeg, result);
    }

    [Fact]
    public void DetectImageFormat_WithDataUriSvg_ReturnsSvg()
    {
        // Arrange
        var dataUri = "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTAwIiBoZWlnaHQ9IjEwMCI+PC9zdmc+";

        // Act
        var result = _resolver.DetectImageFormat(dataUri);

        // Assert
        Assert.Equal(ImageFileFormat.Svg, result);
    }

    [Fact]
    public void DetectImageFormat_WithFullPath_ReturnsCorrectFormat()
    {
        // Arrange
        var imagePath = @"C:\Documents\images\diagram.png";

        // Act
        var result = _resolver.DetectImageFormat(imagePath);

        // Assert
        Assert.Equal(ImageFileFormat.Png, result);
    }

    [Fact]
    public void DetectImageFormat_WithUrl_ReturnsCorrectFormat()
    {
        // Arrange
        var imagePath = "https://example.com/images/diagram.jpg";

        // Act
        var result = _resolver.DetectImageFormat(imagePath);

        // Assert
        Assert.Equal(ImageFileFormat.Jpeg, result);
    }

    #endregion
}
