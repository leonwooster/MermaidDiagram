using Xunit;
using MermaidDiagramApp.Services.Export;
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExportListItem = MermaidDiagramApp.Services.Export.ListItem;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Unit tests for OpenXmlWordDocumentGenerator.
/// Tests document creation, all element types, formatting preservation, and image embedding.
/// Requirements: 3.1-3.10, 4.4, 5.4, 5.5, 6.1
/// </summary>
public class OpenXmlWordDocumentGeneratorTests : IDisposable
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

    #region Document Creation Tests

    [Fact]
    public void CreateDocument_WithValidPath_CreatesDocumentFile()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        using var generator = new OpenXmlWordDocumentGenerator();

        // Act
        generator.CreateDocument(outputPath);
        generator.Save();

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void CreateDocument_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        using var generator = new OpenXmlWordDocumentGenerator();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => generator.CreateDocument(null!));
    }

    [Fact]
    public void CreateDocument_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        using var generator = new OpenXmlWordDocumentGenerator();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => generator.CreateDocument(string.Empty));
    }

    [Fact]
    public void CreateDocument_CreatesValidWordDocument()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        using var generator = new OpenXmlWordDocumentGenerator();

        // Act
        generator.CreateDocument(outputPath);
        generator.Save();
        generator.Dispose();

        // Assert - Verify it's a valid Word document
        using var doc = WordprocessingDocument.Open(outputPath, false);
        Assert.NotNull(doc.MainDocumentPart);
        Assert.NotNull(doc.MainDocumentPart.Document);
        Assert.NotNull(doc.MainDocumentPart.Document.Body);
    }

    #endregion

    #region Heading Tests

    [Fact]
    public void AddHeading_WithoutCreatingDocument_ThrowsInvalidOperationException()
    {
        // Arrange
        using var generator = new OpenXmlWordDocumentGenerator();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.AddHeading("Test", 1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void AddHeading_WithValidLevel_CreatesHeadingWithCorrectStyle(int level)
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var headingText = $"Heading Level {level}";
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddHeading(headingText, level);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        
        Assert.Equal($"Heading{level}", styleId);
        var text = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        Assert.Equal(headingText, text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void AddHeading_WithInvalidLevel_ThrowsArgumentOutOfRangeException(int level)
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.AddHeading("Test", level));
    }

    #endregion

    #region Paragraph Tests

    [Fact]
    public void AddParagraph_WithPlainText_CreatesParagraph()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "This is a plain paragraph.";
        var style = new ParagraphStyle();
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddParagraph(text, style);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var actualText = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        Assert.Equal(text, actualText);
    }

    [Fact]
    public void AddParagraph_WithBoldStyle_AppliesBoldFormatting()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "Bold text";
        var style = new ParagraphStyle { IsBold = true };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddParagraph(text, style);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var run = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First().Elements<Run>().First();
        Assert.NotNull(run.RunProperties?.Bold);
    }

    [Fact]
    public void AddParagraph_WithItalicStyle_AppliesItalicFormatting()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "Italic text";
        var style = new ParagraphStyle { IsItalic = true };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddParagraph(text, style);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var run = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First().Elements<Run>().First();
        Assert.NotNull(run.RunProperties?.Italic);
    }

    [Fact]
    public void AddParagraph_WithCodeStyle_AppliesMonospaceAndShading()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "code text";
        var style = new ParagraphStyle { IsCode = true };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddParagraph(text, style);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var run = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First().Elements<Run>().First();
        var fonts = run.RunProperties?.RunFonts;
        var shading = run.RunProperties?.Shading;
        
        Assert.NotNull(fonts);
        Assert.Equal("Courier New", fonts.Ascii?.Value);
        Assert.NotNull(shading);
        Assert.Equal("F0F0F0", shading.Fill?.Value);
    }

    [Fact]
    public void AddParagraph_WithCustomFont_AppliesCustomFont()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "Custom font text";
        var style = new ParagraphStyle { FontFamily = "Arial" };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddParagraph(text, style);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var run = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First().Elements<Run>().First();
        var fonts = run.RunProperties?.RunFonts;
        
        Assert.NotNull(fonts);
        Assert.Equal("Arial", fonts.Ascii?.Value);
    }

    [Fact]
    public void AddParagraph_WithCustomFontSize_AppliesCustomFontSize()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "Custom size text";
        var style = new ParagraphStyle { FontSize = 14 };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddParagraph(text, style);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var run = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First().Elements<Run>().First();
        var fontSize = run.RunProperties?.FontSize;
        
        Assert.NotNull(fontSize);
        Assert.Equal("28", fontSize.Val?.Value); // 14 * 2 = 28 half-points
    }

    #endregion

    #region List Tests

    [Fact]
    public void AddList_WithOrderedList_CreatesNumberedList()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var listData = new ListData
        {
            Items = new List<ExportListItem>
            {
                new ExportListItem { Text = "First item", Level = 0 },
                new ExportListItem { Text = "Second item", Level = 0 },
                new ExportListItem { Text = "Third item", Level = 0 }
            }
        };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddList(listData, ordered: true);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        
        Assert.Equal(3, paragraphs.Count);
        foreach (var paragraph in paragraphs)
        {
            var numberingProps = paragraph.ParagraphProperties?.NumberingProperties;
            Assert.NotNull(numberingProps);
            Assert.NotNull(numberingProps.NumberingId);
        }
    }

    [Fact]
    public void AddList_WithUnorderedList_CreatesBulletedList()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var listData = new ListData
        {
            Items = new List<ExportListItem>
            {
                new ExportListItem { Text = "First item", Level = 0 },
                new ExportListItem { Text = "Second item", Level = 0 }
            }
        };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddList(listData, ordered: false);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        
        Assert.Equal(2, paragraphs.Count);
        foreach (var paragraph in paragraphs)
        {
            var numberingProps = paragraph.ParagraphProperties?.NumberingProperties;
            Assert.NotNull(numberingProps);
        }
    }

    [Fact]
    public void AddList_WithNestedItems_PreservesHierarchy()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var listData = new ListData
        {
            Items = new List<ExportListItem>
            {
                new ExportListItem
                {
                    Text = "Parent item",
                    Level = 0,
                    NestedItems = new List<ExportListItem>
                    {
                        new ExportListItem { Text = "Child item 1", Level = 1 },
                        new ExportListItem { Text = "Child item 2", Level = 1 }
                    }
                }
            }
        };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddList(listData, ordered: false);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        
        Assert.Equal(3, paragraphs.Count); // 1 parent + 2 children
        
        // Verify parent level
        var parentLevel = paragraphs[0].ParagraphProperties?.NumberingProperties?.NumberingLevelReference?.Val?.Value;
        Assert.Equal(0, parentLevel);
        
        // Verify child levels
        var child1Level = paragraphs[1].ParagraphProperties?.NumberingProperties?.NumberingLevelReference?.Val?.Value;
        var child2Level = paragraphs[2].ParagraphProperties?.NumberingProperties?.NumberingLevelReference?.Val?.Value;
        Assert.Equal(1, child1Level);
        Assert.Equal(1, child2Level);
    }

    #endregion

    #region Table Tests

    [Fact]
    public void AddTable_WithHeaderAndRows_CreatesTableStructure()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var tableData = new TableData
        {
            Headers = new List<string> { "Column 1", "Column 2", "Column 3" },
            Rows = new List<List<string>>
            {
                new List<string> { "Row 1 Col 1", "Row 1 Col 2", "Row 1 Col 3" },
                new List<string> { "Row 2 Col 1", "Row 2 Col 2", "Row 2 Col 3" }
            },
            HasHeaderRow = true
        };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddTable(tableData);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var table = doc.MainDocumentPart!.Document.Body!.Elements<Table>().First();
        var rows = table.Elements<TableRow>().ToList();
        
        Assert.Equal(3, rows.Count); // 1 header + 2 data rows
        
        // Verify header row
        var headerCells = rows[0].Elements<TableCell>().ToList();
        Assert.Equal(3, headerCells.Count);
        
        // Verify data rows
        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].Elements<TableCell>().ToList();
            Assert.Equal(3, cells.Count);
        }
    }

    [Fact]
    public void AddTable_HeaderCells_HaveBoldFormatting()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var tableData = new TableData
        {
            Headers = new List<string> { "Header 1", "Header 2" },
            Rows = new List<List<string>>
            {
                new List<string> { "Data 1", "Data 2" }
            },
            HasHeaderRow = true
        };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddTable(tableData);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var table = doc.MainDocumentPart!.Document.Body!.Elements<Table>().First();
        var headerRow = table.Elements<TableRow>().First();
        var headerCells = headerRow.Elements<TableCell>().ToList();
        
        foreach (var cell in headerCells)
        {
            var run = cell.Descendants<Run>().First();
            Assert.NotNull(run.RunProperties?.Bold);
        }
    }

    [Fact]
    public void AddTable_WithBorders_HasTableBorders()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var tableData = new TableData
        {
            Headers = new List<string> { "Col1" },
            Rows = new List<List<string>> { new List<string> { "Data" } },
            HasHeaderRow = true
        };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddTable(tableData);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var table = doc.MainDocumentPart!.Document.Body!.Elements<Table>().First();
        var tableProperties = table.Elements<TableProperties>().FirstOrDefault();
        var tableBorders = tableProperties?.TableBorders;
        
        Assert.NotNull(tableBorders);
        Assert.NotNull(tableBorders.TopBorder);
        Assert.NotNull(tableBorders.BottomBorder);
        Assert.NotNull(tableBorders.LeftBorder);
        Assert.NotNull(tableBorders.RightBorder);
    }

    #endregion

    #region Code Block Tests

    [Fact]
    public void AddCodeBlock_CreatesCodeBlockWithMonospaceFont()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var code = "function test() {\n    return true;\n}";
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddCodeBlock(code, "javascript");
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        
        // Verify shading
        var shading = paragraph.ParagraphProperties?.Shading;
        Assert.NotNull(shading);
        Assert.Equal("F5F5F5", shading.Fill?.Value);
        
        // Verify monospace font
        var runs = paragraph.Elements<Run>().Where(r => r.Elements<Text>().Any()).ToList();
        Assert.NotEmpty(runs);
        foreach (var run in runs)
        {
            var fonts = run.RunProperties?.RunFonts;
            Assert.NotNull(fonts);
            Assert.Equal("Courier New", fonts.Ascii?.Value);
        }
    }

    [Fact]
    public void AddCodeBlock_WithBorders_HasBorders()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var code = "test code";
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddCodeBlock(code, "text");
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var borders = paragraph.ParagraphProperties?.ParagraphBorders;
        
        Assert.NotNull(borders);
        Assert.NotNull(borders.TopBorder);
        Assert.NotNull(borders.BottomBorder);
        Assert.NotNull(borders.LeftBorder);
        Assert.NotNull(borders.RightBorder);
    }

    [Fact]
    public void AddCodeBlock_WithMultipleLines_PreservesLineBreaks()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var code = "line1\nline2\nline3";
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddCodeBlock(code, "text");
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var breaks = paragraph.Descendants<Break>().Count();
        
        // Should have 2 breaks for 3 lines
        Assert.Equal(2, breaks);
    }

    #endregion

    #region Blockquote Tests

    [Fact]
    public void AddBlockquote_AppliesIndentation()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "This is a blockquote.";
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddBlockquote(text);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var indentation = paragraph.ParagraphProperties?.Indentation;
        
        Assert.NotNull(indentation);
        Assert.Equal("720", indentation.Left?.Value);
        Assert.Equal("720", indentation.Right?.Value);
    }

    [Fact]
    public void AddBlockquote_AppliesItalicFormatting()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "Blockquote text";
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddBlockquote(text);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var run = paragraph.Elements<Run>().First();
        
        Assert.NotNull(run.RunProperties?.Italic);
    }

    [Fact]
    public void AddBlockquote_HasLeftBorder()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var text = "Blockquote with border";
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddBlockquote(text);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var borders = paragraph.ParagraphProperties?.ParagraphBorders;
        
        Assert.NotNull(borders);
        Assert.NotNull(borders.LeftBorder);
    }

    #endregion

    #region Image Tests

    [Fact]
    public void AddImage_WithValidImage_EmbedsImage()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        _tempFiles.Add(imagePath);
        
        // Create a test image
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var options = new ImageOptions();
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddImage(imagePath, options);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart!.ImageParts.ToList();
        
        Assert.Single(imageParts);
        Assert.Equal("image/png", imageParts[0].ContentType);
    }

    [Fact]
    public void AddImage_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var imagePath = "nonexistent.png";
        var options = new ImageOptions();
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => generator.AddImage(imagePath, options));
    }

    [Fact]
    public void AddImage_WithJpegFormat_PreservesFormat()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.jpg");
        _tempFiles.Add(imagePath);
        
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        var options = new ImageOptions();
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddImage(imagePath, options);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var imageParts = doc.MainDocumentPart!.ImageParts.ToList();
        
        Assert.Single(imageParts);
        Assert.Equal("image/jpeg", imageParts[0].ContentType);
    }

    [Fact]
    public void AddImage_WithCenterAlignment_AppliesCenterJustification()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        _tempFiles.Add(imagePath);
        
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var options = new ImageOptions { Alignment = HorizontalAlignment.Center };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddImage(imagePath, options);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var justification = paragraph.ParagraphProperties?.Justification;
        
        Assert.NotNull(justification);
        Assert.Equal(JustificationValues.Center, justification.Val?.Value);
    }

    [Fact]
    public void AddImage_WithRightAlignment_AppliesRightJustification()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        _tempFiles.Add(imagePath);
        
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var options = new ImageOptions { Alignment = HorizontalAlignment.Right };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddImage(imagePath, options);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var justification = paragraph.ParagraphProperties?.Justification;
        
        Assert.NotNull(justification);
        Assert.Equal(JustificationValues.Right, justification.Val?.Value);
    }

    [Fact]
    public void AddImage_WithScaling_MaintainsAspectRatio()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        _tempFiles.Add(imagePath);
        
        var originalWidth = 1000;
        var originalHeight = 500;
        var originalAspectRatio = (double)originalWidth / originalHeight;
        
        using (var bitmap = new System.Drawing.Bitmap(originalWidth, originalHeight))
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var options = new ImageOptions
        {
            MaxWidth = 600,
            MaxHeight = 800,
            MaintainAspectRatio = true
        };
        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act
        generator.AddImage(imagePath, options);
        generator.Save();
        generator.Dispose();

        // Assert
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var extent = doc.MainDocumentPart!.Document.Body!
            .Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>().First();
        
        var embeddedWidth = extent.Cx!.Value;
        var embeddedHeight = extent.Cy!.Value;
        var embeddedAspectRatio = (double)embeddedWidth / embeddedHeight;
        
        // Allow small tolerance for rounding
        Assert.True(Math.Abs(originalAspectRatio - embeddedAspectRatio) < 0.01);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteDocument_WithAllElements_CreatesValidDocument()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var imagePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        _tempFiles.Add(imagePath);
        
        using (var bitmap = new System.Drawing.Bitmap(100, 100))
        {
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        using var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);

        // Act - Add all types of elements
        generator.AddHeading("Main Heading", 1);
        generator.AddParagraph("This is a paragraph with bold text.", new ParagraphStyle { IsBold = true });
        generator.AddHeading("Subheading", 2);
        generator.AddParagraph("Normal paragraph.", new ParagraphStyle());
        
        generator.AddList(new ListData
        {
            Items = new List<ExportListItem>
            {
                new ExportListItem { Text = "Item 1", Level = 0 },
                new ExportListItem { Text = "Item 2", Level = 0 }
            }
        }, ordered: true);
        
        generator.AddCodeBlock("var x = 10;", "javascript");
        generator.AddBlockquote("This is a quote.");
        
        generator.AddTable(new TableData
        {
            Headers = new List<string> { "Col1", "Col2" },
            Rows = new List<List<string>>
            {
                new List<string> { "A", "B" },
                new List<string> { "C", "D" }
            }
        });
        
        generator.AddImage(imagePath, new ImageOptions());
        
        generator.Save();
        generator.Dispose();

        // Assert - Verify document is valid and contains all elements
        using var doc = WordprocessingDocument.Open(outputPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        
        Assert.NotEmpty(body.Elements<Paragraph>());
        Assert.NotEmpty(body.Elements<Table>());
        Assert.NotEmpty(doc.MainDocumentPart.ImageParts);
    }

    [Fact]
    public void Save_WithoutCreatingDocument_ThrowsInvalidOperationException()
    {
        // Arrange
        using var generator = new OpenXmlWordDocumentGenerator();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.Save());
    }

    [Fact]
    public void Dispose_ClosesDocument()
    {
        // Arrange
        var outputPath = GetTempDocxPath();
        var generator = new OpenXmlWordDocumentGenerator();
        generator.CreateDocument(outputPath);
        generator.Save();

        // Act
        generator.Dispose();

        // Assert - Should be able to open the file (not locked)
        using var doc = WordprocessingDocument.Open(outputPath, false);
        Assert.NotNull(doc);
    }

    #endregion
}
