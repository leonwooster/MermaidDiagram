using Xunit;
using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.Services.Export;
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExportListItem = MermaidDiagramApp.Services.Export.ListItem;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Property-based tests for OpenXmlWordDocumentGenerator.
/// </summary>
public class OpenXmlWordDocumentGeneratorPropertyTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        // Clean up temporary files
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                try { File.Delete(file); } catch { }
            }
        }
    }

    private string GetTempDocxPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.docx");
        _tempFiles.Add(path);
        return path;
    }

    /// <summary>
    /// Sanitizes a string to remove invalid XML characters and normalize line endings.
    /// XML 1.0 only allows: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
    /// OpenXML normalizes line endings to \n, so we do the same for test comparison.
    /// </summary>
    private string SanitizeXmlString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sanitized = new string(input.Where(c =>
            c == 0x9 || c == 0xA || c == 0xD ||
            (c >= 0x20 && c <= 0xD7FF) ||
            (c >= 0xE000 && c <= 0xFFFD)).ToArray());
        
        // Normalize line endings to match OpenXML behavior
        return sanitized.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 7: Heading level preservation
    /// Validates: Requirements 3.1
    /// 
    /// Property: For any Markdown document with headings (H1-H6),
    /// the generated Word document should contain corresponding Word heading styles at the same levels.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddHeading_PreservesHeadingLevel(PositiveInt levelInt, NonEmptyString text)
    {
        // Arrange: Constrain level to 1-6
        var level = (levelInt.Get % 6) + 1;
        var headingText = SanitizeXmlString(text.Get);
        if (string.IsNullOrWhiteSpace(headingText))
            return; // Skip invalid inputs
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddHeading(headingText, level);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify heading
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var paragraphs = body.Elements<Paragraph>().ToList();
        Assert.NotEmpty(paragraphs);

        var paragraph = paragraphs.First();
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        
        // Verify the heading style matches the level
        Assert.Equal($"Heading{level}", styleId);
        
        // Verify the text content
        var textContent = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        Assert.Equal(headingText, textContent);
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 8: Text formatting preservation
    /// Validates: Requirements 3.2, 3.3, 3.8
    /// 
    /// Property: For any Markdown text with inline formatting (bold, italic, code),
    /// the generated Word document should preserve all formatting with correct Word styles.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddParagraph_PreservesTextFormatting(NonEmptyString text, bool isBold, bool isItalic, bool isCode)
    {
        // Arrange
        var paragraphText = SanitizeXmlString(text.Get);
        if (string.IsNullOrWhiteSpace(paragraphText))
            return; // Skip invalid inputs
        var outputPath = GetTempDocxPath();
        var style = new ParagraphStyle
        {
            IsBold = isBold,
            IsItalic = isItalic,
            IsCode = isCode
        };

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddParagraph(paragraphText, style);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify formatting
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var paragraph = body.Elements<Paragraph>().First();
        var run = paragraph.Elements<Run>().First();
        var runProperties = run.RunProperties;

        // Verify text content
        var textContent = string.Join("", run.Descendants<Text>().Select(t => t.Text));
        Assert.Equal(paragraphText, textContent);

        // Verify bold formatting
        if (isBold)
        {
            Assert.NotNull(runProperties?.Bold);
        }

        // Verify italic formatting
        if (isItalic)
        {
            Assert.NotNull(runProperties?.Italic);
        }

        // Verify code formatting (monospace font and shading)
        if (isCode)
        {
            var fonts = runProperties?.RunFonts;
            Assert.NotNull(fonts);
            Assert.Equal("Courier New", fonts.Ascii?.Value);
            
            var shading = runProperties?.Shading;
            Assert.NotNull(shading);
        }
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 9: List structure conversion
    /// Validates: Requirements 3.4, 3.5
    /// 
    /// Property: For any Markdown document with lists (ordered or unordered),
    /// the generated Word document should contain corresponding Word lists with the same structure.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddList_PreservesListStructure(NonEmptyArray<NonEmptyString> items, bool ordered)
    {
        // Arrange
        var listItems = items.Get
            .Select(s => SanitizeXmlString(s.Get))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => new ExportListItem { Text = s, Level = 0 })
            .ToList();
        
        if (listItems.Count == 0)
            return; // Skip if no valid items
            
        var listData = new ListData { Items = listItems };
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddList(listData, ordered);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify list
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var paragraphs = body.Elements<Paragraph>().ToList();
        Assert.Equal(listItems.Count, paragraphs.Count);

        // Verify each list item has numbering properties
        foreach (var paragraph in paragraphs)
        {
            var numberingProps = paragraph.ParagraphProperties?.NumberingProperties;
            Assert.NotNull(numberingProps);
            Assert.NotNull(numberingProps.NumberingId);
        }

        // Verify text content
        for (int i = 0; i < listItems.Count; i++)
        {
            var textContent = string.Join("", paragraphs[i].Descendants<Text>().Select(t => t.Text));
            Assert.Equal(listItems[i].Text, textContent);
        }
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 10: Nested list hierarchy preservation
    /// Validates: Requirements 3.6
    /// 
    /// Property: For any Markdown document with nested lists,
    /// the generated Word document should preserve the complete nesting hierarchy at all levels.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddList_PreservesNestedListHierarchy(NonEmptyString parentText, NonEmptyArray<NonEmptyString> childTexts)
    {
        // Arrange: Create a parent item with nested children
        var sanitizedParent = SanitizeXmlString(parentText.Get);
        if (string.IsNullOrWhiteSpace(sanitizedParent))
            return; // Skip invalid inputs
            
        var sanitizedChildren = childTexts.Get
            .Select(s => SanitizeXmlString(s.Get))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => new ExportListItem { Text = s, Level = 1 })
            .ToList();
            
        if (sanitizedChildren.Count == 0)
            return; // Skip if no valid children
            
        var parent = new ExportListItem
        {
            Text = sanitizedParent,
            Level = 0,
            NestedItems = sanitizedChildren
        };
        var listData = new ListData { Items = new List<ExportListItem> { parent } };
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddList(listData, false);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify nesting
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var paragraphs = body.Elements<Paragraph>().ToList();
        // Expect 1 parent + number of valid sanitized children
        Assert.Equal(1 + sanitizedChildren.Count, paragraphs.Count);

        // Verify parent has level 0
        var parentParagraph = paragraphs[0];
        var parentLevel = parentParagraph.ParagraphProperties?.NumberingProperties?.NumberingLevelReference?.Val?.Value;
        Assert.Equal(0, parentLevel);

        // Verify children have level 1
        for (int i = 1; i < paragraphs.Count; i++)
        {
            var childLevel = paragraphs[i].ParagraphProperties?.NumberingProperties?.NumberingLevelReference?.Val?.Value;
            Assert.Equal(1, childLevel);
        }
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 11: Code block formatting
    /// Validates: Requirements 3.7
    /// 
    /// Property: For any Markdown code block (non-Mermaid),
    /// the generated Word document should format it with monospace font and background shading.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddCodeBlock_AppliesMonospaceAndShading(NonEmptyString code, string language)
    {
        // Arrange
        var codeText = SanitizeXmlString(code.Get);
        if (string.IsNullOrWhiteSpace(codeText))
            return; // Skip invalid inputs
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddCodeBlock(codeText, language ?? "");
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify code block formatting
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var paragraph = body.Elements<Paragraph>().First();
        
        // Verify paragraph has shading
        var shading = paragraph.ParagraphProperties?.Shading;
        Assert.NotNull(shading);
        Assert.Equal("F5F5F5", shading.Fill?.Value);

        // Verify paragraph has borders
        var borders = paragraph.ParagraphProperties?.ParagraphBorders;
        Assert.NotNull(borders);

        // Verify runs have monospace font
        var runs = paragraph.Elements<Run>().ToList();
        Assert.NotEmpty(runs);
        
        foreach (var run in runs.Where(r => r.Elements<Text>().Any()))
        {
            var fonts = run.RunProperties?.RunFonts;
            Assert.NotNull(fonts);
            Assert.Equal("Courier New", fonts.Ascii?.Value);
        }
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 13: Table structure preservation
    /// Validates: Requirements 3.10
    /// 
    /// Property: For any Markdown table,
    /// the generated Word document should contain a Word table with the same number of rows, columns, and cell alignment.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddTable_PreservesTableStructure(NonEmptyArray<NonEmptyString> headers, NonEmptyArray<NonEmptyArray<NonEmptyString>> rows)
    {
        // Arrange: Ensure all rows have the same number of columns as headers
        var headerList = headers.Get
            .Select(s => SanitizeXmlString(s.Get))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
            
        if (headerList.Count == 0)
            return; // Skip if no valid headers
            
        var columnCount = headerList.Count;
        
        var rowList = rows.Get
            .Select(row => row.Get
                .Take(columnCount)
                .Select(s => SanitizeXmlString(s.Get))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList())
            .Where(row => row.Count == columnCount)
            .ToList();

        if (rowList.Count == 0)
            return; // Skip if no valid rows

        var tableData = new TableData
        {
            Headers = headerList,
            Rows = rowList,
            HasHeaderRow = true
        };
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddTable(tableData);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify table structure
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var table = body.Elements<Table>().First();
        var tableRows = table.Elements<TableRow>().ToList();
        
        // Verify row count (header + data rows)
        Assert.Equal(1 + rowList.Count, tableRows.Count);

        // Verify column count in header row
        var headerRow = tableRows[0];
        var headerCells = headerRow.Elements<TableCell>().ToList();
        Assert.Equal(columnCount, headerCells.Count);

        // Verify column count in data rows
        for (int i = 1; i < tableRows.Count; i++)
        {
            var cells = tableRows[i].Elements<TableCell>().ToList();
            Assert.Equal(columnCount, cells.Count);
        }

        // Verify header text content
        for (int i = 0; i < headerList.Count; i++)
        {
            var cellText = string.Join("", headerCells[i].Descendants<Text>().Select(t => t.Text));
            Assert.Equal(headerList[i], cellText);
        }
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 12: Blockquote styling
    /// Validates: Requirements 3.9
    /// 
    /// Property: For any Markdown blockquote,
    /// the generated Word document should apply indentation and distinctive styling.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddBlockquote_AppliesIndentationAndStyling(NonEmptyString text)
    {
        // Arrange
        var blockquoteText = SanitizeXmlString(text.Get);
        if (string.IsNullOrWhiteSpace(blockquoteText))
            return; // Skip invalid inputs
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddBlockquote(blockquoteText);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify blockquote styling
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var paragraph = body.Elements<Paragraph>().First();
        var paragraphProperties = paragraph.ParagraphProperties;
        Assert.NotNull(paragraphProperties);

        // Verify indentation
        var indentation = paragraphProperties.Indentation;
        Assert.NotNull(indentation);
        Assert.Equal("720", indentation.Left?.Value);
        Assert.Equal("720", indentation.Right?.Value);

        // Verify left border for visual distinction
        var borders = paragraphProperties.ParagraphBorders;
        Assert.NotNull(borders);
        Assert.NotNull(borders.LeftBorder);

        // Verify text content and italic styling
        var run = paragraph.Elements<Run>().First();
        var runProperties = run.RunProperties;
        Assert.NotNull(runProperties);
        Assert.NotNull(runProperties.Italic);

        var textContent = string.Join("", run.Descendants<Text>().Select(t => t.Text));
        Assert.Equal(blockquoteText, textContent);
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 16: Diagram scaling maintains aspect ratio
    /// Validates: Requirements 4.4
    /// 
    /// Property: For any embedded diagram image, if scaling is applied to fit page margins,
    /// the aspect ratio should remain unchanged.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddImage_MaintainsAspectRatio(PositiveInt widthInt, PositiveInt heightInt)
    {
        // Arrange: Create a test image
        var width = (widthInt.Get % 2000) + 100; // 100-2100 pixels
        var height = (heightInt.Get % 2000) + 100;
        var originalAspectRatio = (double)width / height;

        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        _tempFiles.Add(imagePath);
        
        // Create a simple PNG image
        using (var bitmap = new System.Drawing.Bitmap(width, height))
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var options = new ImageOptions
        {
            MaxWidth = 600,
            MaxHeight = 800,
            MaintainAspectRatio = true
        };
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddImage(imagePath, options);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify aspect ratio
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var drawing = body.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>().First();
        var embeddedWidth = drawing.Cx!.Value;
        var embeddedHeight = drawing.Cy!.Value;
        
        var embeddedAspectRatio = (double)embeddedWidth / embeddedHeight;
        
        // Allow small tolerance for rounding errors
        Assert.True(Math.Abs(originalAspectRatio - embeddedAspectRatio) < 0.01,
            $"Aspect ratio changed: original={originalAspectRatio:F3}, embedded={embeddedAspectRatio:F3}");
    }

    /// <summary>
    /// Feature: markdown-to-word-export, Property 19: Image format preservation
    /// Validates: Requirements 5.4
    /// 
    /// Property: For any embedded image (PNG, JPG, GIF),
    /// the generated Word document should preserve the original image format.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AddImage_PreservesImageFormat(PositiveInt formatSelector)
    {
        // Arrange: Select a format to test
        var formats = new[]
        {
            (System.Drawing.Imaging.ImageFormat.Png, ".png", "image/png"),
            (System.Drawing.Imaging.ImageFormat.Jpeg, ".jpg", "image/jpeg"),
            (System.Drawing.Imaging.ImageFormat.Gif, ".gif", "image/gif")
        };
        
        var selectedFormat = formats[formatSelector.Get % formats.Length];
        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}{selectedFormat.Item2}");
        _tempFiles.Add(imagePath);
        
        // Create a test image in the selected format
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        {
            bitmap.Save(imagePath, selectedFormat.Item1);
        }

        var options = new ImageOptions();
        var outputPath = GetTempDocxPath();

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddImage(imagePath, options);
        generator.Save();
        generator.Dispose();

        // Assert: Open the document and verify the image part type
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var mainPart = doc.MainDocumentPart;
        Assert.NotNull(mainPart);

        var imageParts = mainPart.ImageParts.ToList();
        Assert.Single(imageParts);

        var imagePart = imageParts.First();
        Assert.Equal(selectedFormat.Item3, imagePart.ContentType);
    }
}
