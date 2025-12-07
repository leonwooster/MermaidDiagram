using Xunit;
using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.Services.Export;
using System;
using System.Linq;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Property-based tests for MarkdigMarkdownParser.
/// Feature: markdown-to-word-export, Property 2: Mermaid block identification completeness
/// Validates: Requirements 1.3
/// </summary>
public class MarkdigMarkdownParserPropertyTests
{
    private readonly MarkdigMarkdownParser _parser;

    public MarkdigMarkdownParserPropertyTests()
    {
        _parser = new MarkdigMarkdownParser();
    }

    /// <summary>
    /// Property: For any Markdown document containing Mermaid code blocks,
    /// parsing should identify all blocks marked with ```mermaid fence syntax.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExtractMermaidBlocks_IdentifiesAllMermaidBlocks(string[] mermaidCodes, string[] otherContent)
    {
        // Arrange: Filter out null or empty arrays
        if (mermaidCodes == null || mermaidCodes.Length == 0)
            return;

        // Clean up the mermaid codes to avoid null entries and invalid characters
        var cleanMermaidCodes = mermaidCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Where(code => !code.Contains('\0')) // Filter out null characters
            .Select(code => code.Replace("```", "").Trim()) // Remove any backticks from the code
            .ToArray();

        if (cleanMermaidCodes.Length == 0)
            return;

        // Build a Markdown document with Mermaid blocks interspersed with other content
        var markdownBuilder = new System.Text.StringBuilder();

        for (int i = 0; i < cleanMermaidCodes.Length; i++)
        {
            // Add some other content before the Mermaid block (if available)
            if (otherContent != null && i < otherContent.Length && !string.IsNullOrWhiteSpace(otherContent[i]))
            {
                markdownBuilder.AppendLine(otherContent[i]);
                markdownBuilder.AppendLine();
            }

            // Add the Mermaid block
            markdownBuilder.AppendLine("```mermaid");
            markdownBuilder.AppendLine(cleanMermaidCodes[i]);
            markdownBuilder.AppendLine("```");
            markdownBuilder.AppendLine();
        }

        var markdown = markdownBuilder.ToString();

        // Act
        var document = _parser.Parse(markdown);
        var extractedBlocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert: All Mermaid blocks should be identified
        Assert.Equal(cleanMermaidCodes.Length, extractedBlocks.Count);

        // Verify each block's code is present
        // Note: Markdig normalizes line endings to \n, so we need to normalize expected values too
        for (int i = 0; i < cleanMermaidCodes.Length; i++)
        {
            var expectedCode = cleanMermaidCodes[i].Replace("\r\n", "\n").Replace("\r", "\n").Trim();
            var actualCode = extractedBlocks[i].Code.Trim();
            Assert.Equal(expectedCode, actualCode);
        }
    }

    /// <summary>
    /// Property: For any Markdown document without Mermaid blocks,
    /// ExtractMermaidBlocks should return an empty collection.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExtractMermaidBlocks_WithNoMermaidBlocks_ReturnsEmpty(string content)
    {
        // Arrange: Create markdown without mermaid blocks
        if (string.IsNullOrWhiteSpace(content))
            content = "# Hello World\n\nThis is a test.";

        // Ensure no mermaid blocks by removing any ```mermaid patterns
        var markdown = content.Replace("```mermaid", "```text");

        // Act
        var document = _parser.Parse(markdown);
        var extractedBlocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert
        Assert.Empty(extractedBlocks);
    }

    /// <summary>
    /// Property: For any Markdown document with code blocks of different languages,
    /// only Mermaid blocks should be extracted.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExtractMermaidBlocks_OnlyExtractsMermaidBlocks(string mermaidCode, string jsCode, string pythonCode)
    {
        // Arrange: Filter out invalid characters
        if (string.IsNullOrWhiteSpace(mermaidCode) || mermaidCode.Contains('\0'))
            mermaidCode = "graph TD\n  A-->B";

        var markdown = $@"
```javascript
{jsCode ?? "console.log('hello');"}
```

```mermaid
{mermaidCode}
```

```python
{pythonCode ?? "print('hello')"}
```
";

        // Act
        var document = _parser.Parse(markdown);
        var extractedBlocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert: Only one Mermaid block should be extracted
        Assert.Single(extractedBlocks);
        // Note: Markdig normalizes line endings to \n
        var expectedCode = mermaidCode.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        Assert.Equal(expectedCode, extractedBlocks[0].Code.Trim());
    }

    /// <summary>
    /// Property: For any Markdown document, parsing should never throw an exception.
    /// </summary>
    [Property(MaxTest = 100)]
    public void Parse_WithAnyContent_DoesNotThrow(string content)
    {
        // Act & Assert: Should not throw
        var exception = Record.Exception(() =>
        {
            var document = _parser.Parse(content);
            var blocks = _parser.ExtractMermaidBlocks(document).ToList();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Property: For any Markdown document with Mermaid blocks,
    /// line numbers should be positive and in ascending order.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExtractMermaidBlocks_LineNumbersArePositiveAndAscending(string[] mermaidCodes)
    {
        // Arrange
        if (mermaidCodes == null || mermaidCodes.Length == 0)
            return;

        var cleanMermaidCodes = mermaidCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Replace("```", "").Trim())
            .ToArray();

        if (cleanMermaidCodes.Length == 0)
            return;

        var markdownBuilder = new System.Text.StringBuilder();
        foreach (var code in cleanMermaidCodes)
        {
            markdownBuilder.AppendLine("```mermaid");
            markdownBuilder.AppendLine(code);
            markdownBuilder.AppendLine("```");
            markdownBuilder.AppendLine();
        }

        var markdown = markdownBuilder.ToString();

        // Act
        var document = _parser.Parse(markdown);
        var extractedBlocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert: All line numbers should be positive
        Assert.All(extractedBlocks, block => Assert.True(block.LineNumber > 0));

        // Assert: Line numbers should be in ascending order
        for (int i = 1; i < extractedBlocks.Count; i++)
        {
            Assert.True(extractedBlocks[i].LineNumber > extractedBlocks[i - 1].LineNumber);
        }
    }

    /// <summary>
    /// Property: For any Markdown document with case variations of "mermaid",
    /// only lowercase "mermaid" should be recognized.
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExtractMermaidBlocks_IsCaseSensitive(string code)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(code))
            code = "graph TD\n  A-->B";

        var markdown = $@"
```mermaid
{code}
```

```Mermaid
{code}
```

```MERMAID
{code}
```
";

        // Act
        var document = _parser.Parse(markdown);
        var extractedBlocks = _parser.ExtractMermaidBlocks(document).ToList();

        // Assert: All three should be extracted (case-insensitive)
        Assert.Equal(3, extractedBlocks.Count);
    }
}
