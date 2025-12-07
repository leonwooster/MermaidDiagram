using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.IO;
using System.Linq;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Generates Word documents using the Open XML SDK.
/// </summary>
public class OpenXmlWordDocumentGenerator : IWordDocumentGenerator
{
    private WordprocessingDocument? _document;
    private Body? _body;
    private string? _outputPath;
    private int _imageCounter = 0;
    private int _listCounter = 0;

    /// <summary>
    /// Creates a new Word document at the specified path.
    /// </summary>
    public void CreateDocument(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        _outputPath = outputPath;

        try
        {
            // Create the document
            _document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);

            // Add main document part
            var mainPart = _document.AddMainDocumentPart();
            mainPart.Document = new Document();
            _body = mainPart.Document.AppendChild(new Body());
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied when creating document at '{outputPath}'. The file may be open in another program or you may not have write permissions.", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"I/O error when creating document at '{outputPath}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create Word document at '{outputPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adds a heading to the document.
    /// </summary>
    public void AddHeading(string text, int level)
    {
        if (_body == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        var paragraph = new Paragraph();
        var paragraphProperties = new ParagraphProperties();
        var paragraphStyleId = new ParagraphStyleId { Val = $"Heading{level}" };
        paragraphProperties.AppendChild(paragraphStyleId);
        paragraph.AppendChild(paragraphProperties);

        var run = new Run();
        var runProperties = new RunProperties();
        runProperties.AppendChild(new Bold());
        runProperties.AppendChild(new FontSize { Val = GetHeadingFontSize(level).ToString() });
        run.AppendChild(runProperties);
        run.AppendChild(new Text(text));

        paragraph.AppendChild(run);
        _body.AppendChild(paragraph);
    }

    /// <summary>
    /// Adds a paragraph to the document with optional styling.
    /// </summary>
    public void AddParagraph(string text, ParagraphStyle style)
    {
        if (_body == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        var paragraph = new Paragraph();
        var run = new Run();
        var runProperties = new RunProperties();

        if (style.IsBold)
            runProperties.AppendChild(new Bold());

        if (style.IsItalic)
            runProperties.AppendChild(new Italic());

        if (style.IsCode)
        {
            runProperties.AppendChild(new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" });
            runProperties.AppendChild(new Shading
            {
                Val = ShadingPatternValues.Clear,
                Color = "auto",
                Fill = "F0F0F0"
            });
        }
        else if (!string.IsNullOrEmpty(style.FontFamily))
        {
            runProperties.AppendChild(new RunFonts { Ascii = style.FontFamily, HighAnsi = style.FontFamily });
        }

        if (style.FontSize > 0)
            runProperties.AppendChild(new FontSize { Val = (style.FontSize * 2).ToString() }); // Half-points

        run.AppendChild(runProperties);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        paragraph.AppendChild(run);
        _body.AppendChild(paragraph);
    }

    /// <summary>
    /// Adds an image to the document.
    /// </summary>
    public void AddImage(string imagePath, ImageOptions options)
    {
        if (_body == null || _document == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path cannot be null or empty.", nameof(imagePath));

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}", imagePath);

        try
        {
            var mainPart = _document.MainDocumentPart!;

            // Determine image part type
            var imagePartType = GetImagePartType(imagePath);
            var imagePart = mainPart.AddImagePart(imagePartType);

            // Load image data
            try
            {
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    imagePart.FeedData(stream);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when reading image file: {imagePath}", ex);
            }
            catch (IOException ex)
            {
                throw new IOException($"I/O error when reading image file: {imagePath}", ex);
            }

            // Get image dimensions
            var (width, height) = GetImageDimensions(imagePath);

            // Validate dimensions
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException($"Invalid image dimensions ({width}x{height}) for file: {imagePath}");
            }

            // Scale image if needed
            if (options.MaintainAspectRatio)
            {
                var scale = Math.Min(
                    (double)options.MaxWidth / width,
                    (double)options.MaxHeight / height
                );

                if (scale < 1.0)
                {
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }
            }
            else
            {
                width = Math.Min(width, options.MaxWidth);
                height = Math.Min(height, options.MaxHeight);
            }

            // Convert pixels to EMUs (English Metric Units)
            var widthEmus = width * 9525;
            var heightEmus = height * 9525;

            _imageCounter++;
            var relationshipId = mainPart.GetIdOfPart(imagePart);

            // Create the image element
            var element = CreateImageElement(relationshipId, widthEmus, heightEmus, $"Image{_imageCounter}");

            var paragraph = new Paragraph(new Run(element));

            // Apply alignment
            if (options.Alignment != HorizontalAlignment.Left)
            {
                var paragraphProperties = new ParagraphProperties();
                var justification = new Justification
                {
                    Val = options.Alignment == HorizontalAlignment.Center
                        ? JustificationValues.Center
                        : JustificationValues.Right
                };
                paragraphProperties.AppendChild(justification);
                paragraph.InsertAt(paragraphProperties, 0);
            }

            _body.AppendChild(paragraph);
        }
        catch (FileNotFoundException)
        {
            // Re-throw FileNotFoundException as-is
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw UnauthorizedAccessException as-is
            throw;
        }
        catch (IOException)
        {
            // Re-throw IOException as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to add image to document: {imagePath}. {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adds a table to the document.
    /// </summary>
    public void AddTable(TableData tableData)
    {
        if (_body == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        var table = new Table();

        // Table properties
        var tableProperties = new TableProperties();
        var tableBorders = new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4 },
            new BottomBorder { Val = BorderValues.Single, Size = 4 },
            new LeftBorder { Val = BorderValues.Single, Size = 4 },
            new RightBorder { Val = BorderValues.Single, Size = 4 },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
        );
        tableProperties.AppendChild(tableBorders);
        table.AppendChild(tableProperties);

        // Add header row if present
        if (tableData.HasHeaderRow && tableData.Headers.Count > 0)
        {
            var headerRow = new TableRow();
            foreach (var header in tableData.Headers)
            {
                var cell = CreateTableCell(header, true);
                headerRow.AppendChild(cell);
            }
            table.AppendChild(headerRow);
        }

        // Add data rows
        foreach (var row in tableData.Rows)
        {
            var tableRow = new TableRow();
            foreach (var cellText in row)
            {
                var cell = CreateTableCell(cellText, false);
                tableRow.AppendChild(cell);
            }
            table.AppendChild(tableRow);
        }

        _body.AppendChild(table);
    }

    /// <summary>
    /// Adds a list to the document.
    /// </summary>
    public void AddList(ListData listData, bool ordered)
    {
        if (_body == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        _listCounter++;
        var abstractNumId = _listCounter;
        var numId = _listCounter;

        // Create numbering definitions if they don't exist
        EnsureNumberingDefinitions(abstractNumId, numId, ordered);

        // Add list items
        foreach (var item in listData.Items)
        {
            AddListItem(item, numId, ordered);
        }
    }

    /// <summary>
    /// Adds a code block to the document.
    /// </summary>
    public void AddCodeBlock(string code, string language)
    {
        if (_body == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        var paragraph = new Paragraph();
        var paragraphProperties = new ParagraphProperties();

        // Add shading for code block background
        var shading = new Shading
        {
            Val = ShadingPatternValues.Clear,
            Color = "auto",
            Fill = "F5F5F5"
        };
        paragraphProperties.AppendChild(shading);

        // Add borders
        var paragraphBorders = new ParagraphBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new RightBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" }
        );
        paragraphProperties.AppendChild(paragraphBorders);

        // Add spacing
        var spacingBetweenLines = new SpacingBetweenLines { Before = "100", After = "100" };
        paragraphProperties.AppendChild(spacingBetweenLines);

        paragraph.AppendChild(paragraphProperties);

        // Split code into lines and add each line
        var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            var run = new Run();
            var runProperties = new RunProperties();
            runProperties.AppendChild(new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" });
            runProperties.AppendChild(new FontSize { Val = "20" }); // 10pt
            run.AppendChild(runProperties);
            run.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });

            paragraph.AppendChild(run);

            // Add line break except for the last line
            if (i < lines.Length - 1)
            {
                paragraph.AppendChild(new Run(new Break()));
            }
        }

        _body.AppendChild(paragraph);
    }

    /// <summary>
    /// Adds a blockquote to the document.
    /// </summary>
    public void AddBlockquote(string text)
    {
        if (_body == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        var paragraph = new Paragraph();
        var paragraphProperties = new ParagraphProperties();

        // Add left indentation for blockquote
        var indentation = new Indentation { Left = "720", Right = "720" }; // 0.5 inch on each side
        paragraphProperties.AppendChild(indentation);

        // Add left border for visual distinction
        var paragraphBorders = new ParagraphBorders(
            new LeftBorder { Val = BorderValues.Single, Size = 12, Color = "CCCCCC" }
        );
        paragraphProperties.AppendChild(paragraphBorders);

        // Add spacing
        var spacingBetweenLines = new SpacingBetweenLines { Before = "100", After = "100" };
        paragraphProperties.AppendChild(spacingBetweenLines);

        paragraph.AppendChild(paragraphProperties);

        // Add the text with italic styling
        var run = new Run();
        var runProperties = new RunProperties();
        runProperties.AppendChild(new Italic());
        runProperties.AppendChild(new Color { Val = "666666" }); // Gray text
        run.AppendChild(runProperties);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        paragraph.AppendChild(run);
        _body.AppendChild(paragraph);
    }

    /// <summary>
    /// Saves the document to disk.
    /// </summary>
    public void Save()
    {
        if (_document == null)
            throw new InvalidOperationException("Document not created. Call CreateDocument first.");

        try
        {
            _document.Save();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied when saving document to '{_outputPath}'. The file may be open in another program.", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"I/O error when saving document to '{_outputPath}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save Word document to '{_outputPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the document and releases resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _document?.Dispose();
        }
        catch (Exception)
        {
            // Suppress exceptions during disposal to prevent masking original errors
        }
        finally
        {
            _document = null;
            _body = null;
        }
    }

    // Helper methods

    private string GetHeadingFontSize(int level)
    {
        return level switch
        {
            1 => "32", // 16pt
            2 => "26", // 13pt
            3 => "24", // 12pt
            4 => "22", // 11pt
            5 => "22", // 11pt
            6 => "22", // 11pt
            _ => "22"
        };
    }

    private PartTypeInfo GetImagePartType(string imagePath)
    {
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => ImagePartType.Png,
            ".jpg" or ".jpeg" => ImagePartType.Jpeg,
            ".gif" => ImagePartType.Gif,
            ".bmp" => ImagePartType.Bmp,
            ".tif" or ".tiff" => ImagePartType.Tiff,
            _ => ImagePartType.Png
        };
    }

    private (int width, int height) GetImageDimensions(string imagePath)
    {
        try
        {
            using var image = System.Drawing.Image.FromFile(imagePath);
            return (image.Width, image.Height);
        }
        catch
        {
            // Default dimensions if we can't read the image
            return (600, 400);
        }
    }

    private Drawing CreateImageElement(string relationshipId, long widthEmus, long heightEmus, string imageName)
    {
        var element = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmus, Cy = heightEmus },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = (uint)_imageCounter, Name = imageName },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = imageName },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))
                    )
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
            )
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });

        return element;
    }

    private TableCell CreateTableCell(string text, bool isHeader)
    {
        var cell = new TableCell();

        var paragraph = new Paragraph();
        var run = new Run();
        var runProperties = new RunProperties();

        if (isHeader)
        {
            runProperties.AppendChild(new Bold());
        }

        run.AppendChild(runProperties);
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        cell.AppendChild(paragraph);

        // Cell properties
        var cellProperties = new TableCellProperties();
        var cellShading = new Shading
        {
            Val = ShadingPatternValues.Clear,
            Color = "auto",
            Fill = isHeader ? "D9D9D9" : "FFFFFF"
        };
        cellProperties.AppendChild(cellShading);
        cell.AppendChild(cellProperties);

        return cell;
    }

    private void AddListItem(ListItem item, int numId, bool ordered)
    {
        var paragraph = new Paragraph();
        var paragraphProperties = new ParagraphProperties();

        var numberingProperties = new NumberingProperties();
        numberingProperties.AppendChild(new NumberingLevelReference { Val = item.Level });
        numberingProperties.AppendChild(new NumberingId { Val = numId });
        paragraphProperties.AppendChild(numberingProperties);

        // Add indentation based on level
        var indentation = new Indentation { Left = (item.Level * 720).ToString() }; // 720 = 0.5 inch
        paragraphProperties.AppendChild(indentation);

        paragraph.AppendChild(paragraphProperties);

        var run = new Run(new Text(item.Text));
        paragraph.AppendChild(run);

        _body!.AppendChild(paragraph);

        // Add nested items
        foreach (var nestedItem in item.NestedItems)
        {
            AddListItem(nestedItem, numId, ordered);
        }
    }

    private void EnsureNumberingDefinitions(int abstractNumId, int numId, bool ordered)
    {
        if (_document?.MainDocumentPart == null)
            return;

        var numberingPart = _document.MainDocumentPart.NumberingDefinitionsPart;
        if (numberingPart == null)
        {
            numberingPart = _document.MainDocumentPart.AddNewPart<NumberingDefinitionsPart>();
            numberingPart.Numbering = new Numbering();
        }

        var numbering = numberingPart.Numbering;

        // Create abstract numbering definition
        var abstractNum = new AbstractNum { AbstractNumberId = abstractNumId };

        for (int i = 0; i < 9; i++)
        {
            var level = new Level { LevelIndex = i };
            level.AppendChild(new StartNumberingValue { Val = 1 });

            if (ordered)
            {
                level.AppendChild(new NumberingFormat { Val = NumberFormatValues.Decimal });
                level.AppendChild(new LevelText { Val = $"%{i + 1}." });
            }
            else
            {
                level.AppendChild(new NumberingFormat { Val = NumberFormatValues.Bullet });
                level.AppendChild(new LevelText { Val = "●" });
            }

            level.AppendChild(new LevelJustification { Val = LevelJustificationValues.Left });

            var previousParagraphProperties = new PreviousParagraphProperties();
            previousParagraphProperties.AppendChild(new Indentation
            {
                Left = (i * 720).ToString(),
                Hanging = "360"
            });
            level.AppendChild(previousParagraphProperties);

            abstractNum.AppendChild(level);
        }

        numbering.AppendChild(abstractNum);

        // Create numbering instance
        var numberingInstance = new NumberingInstance { NumberID = numId };
        numberingInstance.AppendChild(new AbstractNumId { Val = abstractNumId });
        numbering.AppendChild(numberingInstance);
    }
}
