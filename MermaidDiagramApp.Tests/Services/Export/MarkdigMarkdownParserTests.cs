using Xunit;
using MermaidDiagramApp.Services.Export;
using System.Linq;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Unit tests for MarkdigMarkdownParser.
/// Validates: Requirements 1.3
/// </summary>
public class MarkdigMarkdownParserTests
{
    private readonly MarkdigMarkdownParser _parser;

    public MarkdigMarkdownParserTests()
    {
        _parser = new MarkdigMarkdownParser();
    }

    #region Parse Tests

    [Fact]
    public void Parse_WithValidMarkdown_ReturnsDocument()
    {
        // Arrange
        var markdown = "# Hello World\n\nThis is a test.";

        // Act
        var document = _parser.Parse(markdown);

        // Assert
        Assert.NotNull(document);
        Assert.NotEmpty(document);
    }

    [Fact]
    public void Parse_WithEmptyString_ReturnsEmptyDocument()
    {
        // Arrange
        var markdown = string.Empty;

        // Act
        var document = _parser.Parse(markdown);

        // Assert
        Assert.NotNull(document);
        Assert.Empty(document);
    }

    [Fact]
    public void Parse_WithNull_ReturnsEmptyDocument()
    {
        // Arrange
        string? markdown = null;

        // Act
        var document = _parser.Parse(markdown!);

        // Assert
        Assert.NotNull(document);
        Assert.Empty(document);
    }

    [Fact]
    public void Parse_WithHeadings_ParsesCorrectly()
    {
        // Arrange
        var markdown = @"
# Heading 1
## Heading 2
### Heading 3
";

        // Act
        var document = _parser.Parse(markdown);

        // Assert
        Assert.NotNull(document);
        Assert.NotEmpty(document);
    }

    [Fact]
    public void Parse_WithLists_ParsesCorrectly()
    {
        // Arrange
        var markdown = @"
- Item 1
- Item 2
  - Nested item
- Item 3

1. First
2. Second
3. Third
";

        // Act
        var document = _parser.Parse(markdown);

        // Assert
        Assert.NotNull(document);
        Assert.NotEmpty(document);
    }

    [Fact]
    public void Parse_WithTable_ParsesCorrectly()
    {
        // Arrange
        var markdown = @"
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |
| Cell 3   | Cell 4   |
";

        // Act
        var document = _parser.Parse(markdown);

        // Assert
        Assert.NotNull(document);
        Assert.NotEmpty(document);
    }

    [Fact]
    public void Parse_WithCodeBlock_ParsesCorrectly()
    {
        // Arrange
        var markdown = @"
```javascript
console.log('hello');
```
";

        // Act
        var document = _parser.Parse(markdown);

        // Assert
        Assert.NotNull(document);
        Assert.NotEmpty(document);
    }

    #endregion

    #region ExtractMermaidBlocks Tests

    [Fact]
    public void ExtractMermaidBlocks_WithSingleMermaidBlock_ReturnsOneBlock()
    {
        // Arrange
        var markdown = @"
# Test Document

```mermaid
graph TD
    A-->B
```
";
        var document = _parser.Parse(markdown);

        // Act
        var blocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert
        Assert.Single(blocks);
        Assert.Contains("graph TD", blocks[0].Code);
        Assert.Contains("A-->B", blocks[0].Code);
    }

    [Fact]
    public void ExtractMermaidBlocks_WithMultipleMermaidBlocks_ReturnsAllBlocks()
    {
        // Arrange
        var markdown = @"
```mermaid
graph TD
    A-->B
```

Some text in between.

```mermaid
sequenceDiagram
    Alice->>Bob: Hello
```
";
        var document = _parser.Parse(markdown);

        // Act
        var blocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert
        Assert.Equal(2, blocks.Count);
        Assert.Contains("graph TD", blocks[0].Code);
        Assert.Contains("sequenceDiagram", blocks[1].Code);
    }

    [Fact]
    public void ExtractMermaidBlocks_WithNoMermaidBlocks_ReturnsEmpty()
    {
        // Arrange
        var markdown = @"
# Test Document

This is just regular text.

```javascript
console.log('hello');
```
";
        var document = _parser.Parse(markdown);

        // Act
        var blocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert
        Assert.Empty(blocks);
    }

    [Fact]
    public void ExtractMermaidBlocks_WithMixedCodeBlocks_ReturnsOnlyMermaid()
    {
        // Arrange
        var markdown = @"
```javascript
console.log('hello');
```

```mermaid
graph LR
    Start-->End
```

```python
print('world')
```
";
        var document = _parser.Parse(markdown);

        // Act
        var blocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert
        Assert.Single(blocks);
        Assert.Contains("graph LR", blocks[0].Code);
    }

    [Fact]
    public void ExtractMermaidBlocks_WithEmptyMermaidBlock_ReturnsBlockWithEmptyCode()
    {
        // Arrange
        var markdown = @"
```mermaid
```
";
        var document = _parser.Parse(markdown);

        // Act
        var blocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert
        Assert.Single(blocks);
        Assert.Equal(string.Empty, blocks[0].Code.Trim());
    }

    [Fact]
    public void ExtractMermaidBlocks_WithNullDocument_ReturnsEmpty()
    {
        // Act
        var blocks = _parser.ExtractMermaidBlocks(null!).ToList();

        // Assert
        Assert.Empty(blocks);
    }

    [Fact]
    public void ExtractMermaidBlocks_SetsLineNumbers()
    {
        // Arrange
        var markdown = @"
Line 1
Line 2
```mermaid
graph TD
    A-->B
```
Line 8
";
        var document = _parser.Parse(markdown);

        // Act
        var blocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert
        Assert.Single(blocks);
        Assert.True(blocks[0].LineNumber > 0);
    }

    #endregion

    #region ExtractImageReferences Tests

    [Fact]
    public void ExtractImageReferences_WithSingleImage_ReturnsOneReference()
    {
        // Arrange
        var markdown = "![Alt text](image.png)";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("image.png", images[0].OriginalPath);
        Assert.Equal("Alt text", images[0].AltText);
    }

    [Fact]
    public void ExtractImageReferences_WithMultipleImages_ReturnsAllReferences()
    {
        // Arrange
        var markdown = @"
![Image 1](image1.png)

Some text.

![Image 2](image2.jpg)
";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Equal(2, images.Count);
        Assert.Equal("image1.png", images[0].OriginalPath);
        Assert.Equal("image2.jpg", images[1].OriginalPath);
    }

    [Fact]
    public void ExtractImageReferences_WithNoImages_ReturnsEmpty()
    {
        // Arrange
        var markdown = @"
# Test Document

This is just text with [a link](http://example.com) but no images.
";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Empty(images);
    }

    [Fact]
    public void ExtractImageReferences_WithRelativePath_ExtractsCorrectly()
    {
        // Arrange
        var markdown = "![Test](./images/test.png)";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("./images/test.png", images[0].OriginalPath);
    }

    [Fact]
    public void ExtractImageReferences_WithAbsolutePath_ExtractsCorrectly()
    {
        // Arrange
        var markdown = @"![Test](C:\images\test.png)";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Single(images);
        Assert.Contains("images", images[0].OriginalPath);
    }

    [Fact]
    public void ExtractImageReferences_WithUrl_ExtractsCorrectly()
    {
        // Arrange
        var markdown = "![Test](https://example.com/image.png)";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal("https://example.com/image.png", images[0].OriginalPath);
    }

    [Fact]
    public void ExtractImageReferences_WithEmptyAltText_ExtractsWithEmptyAlt()
    {
        // Arrange
        var markdown = "![](image.png)";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Single(images);
        Assert.Equal(string.Empty, images[0].AltText);
    }

    [Fact]
    public void ExtractImageReferences_WithNullDocument_ReturnsEmpty()
    {
        // Act
        var images = _parser.ExtractImageReferences(null!).ToList();

        // Assert
        Assert.Empty(images);
    }

    [Fact]
    public void ExtractImageReferences_SetsLineNumbers()
    {
        // Arrange
        var markdown = @"
Line 1
Line 2
![Test](image.png)
Line 4
";
        var document = _parser.Parse(markdown);

        // Act
        var images = _parser.ExtractImageReferences(document).ToList();

        // Assert
        Assert.Single(images);
        Assert.True(images[0].LineNumber > 0);
    }

    #endregion

    #region Malformed Markdown Tests

    [Fact]
    public void Parse_WithMalformedMarkdown_DoesNotThrow()
    {
        // Arrange
        var markdown = @"
# Unclosed heading
**Bold without closing
[Link without closing
```
Code block without language or closing
";

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            var document = _parser.Parse(markdown);
            var blocks = _parser.ExtractMermaidBlocks(document).ToList();
            var images = _parser.ExtractImageReferences(document).ToList();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void ExtractMermaidBlocks_WithMalformedCodeBlock_HandlesGracefully()
    {
        // Arrange
        var markdown = @"
```mermaid
graph TD
    A-->B
    Missing closing backticks
";
        var document = _parser.Parse(markdown);

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            var blocks = _parser.ExtractMermaidBlocks(document).ToList();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void ExtractImageReferences_WithMalformedImage_HandlesGracefully()
    {
        // Arrange
        var markdown = @"
![Unclosed alt text(image.png)
![](
![Alt text]
";
        var document = _parser.Parse(markdown);

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            var images = _parser.ExtractImageReferences(document).ToList();
        });

        Assert.Null(exception);
    }

    #endregion
}
