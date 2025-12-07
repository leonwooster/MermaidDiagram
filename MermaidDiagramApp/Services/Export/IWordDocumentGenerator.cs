using System;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Interface for generating Word documents using Open XML SDK.
/// </summary>
public interface IWordDocumentGenerator : IDisposable
{
    /// <summary>
    /// Creates a new Word document at the specified path.
    /// </summary>
    /// <param name="outputPath">The path where the document will be created.</param>
    void CreateDocument(string outputPath);

    /// <summary>
    /// Adds a heading to the document.
    /// </summary>
    /// <param name="text">The heading text.</param>
    /// <param name="level">The heading level (1-6).</param>
    void AddHeading(string text, int level);

    /// <summary>
    /// Adds a paragraph to the document with optional styling.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <param name="style">The paragraph style to apply.</param>
    void AddParagraph(string text, ParagraphStyle style);

    /// <summary>
    /// Adds an image to the document.
    /// </summary>
    /// <param name="imagePath">The path to the image file.</param>
    /// <param name="options">Options for image sizing and positioning.</param>
    void AddImage(string imagePath, ImageOptions options);

    /// <summary>
    /// Adds a table to the document.
    /// </summary>
    /// <param name="tableData">The table data to add.</param>
    void AddTable(TableData tableData);

    /// <summary>
    /// Adds a list to the document.
    /// </summary>
    /// <param name="listData">The list data to add.</param>
    /// <param name="ordered">True for numbered lists, false for bulleted lists.</param>
    void AddList(ListData listData, bool ordered);

    /// <summary>
    /// Adds a code block to the document.
    /// </summary>
    /// <param name="code">The code content.</param>
    /// <param name="language">The programming language (optional).</param>
    void AddCodeBlock(string code, string language);

    /// <summary>
    /// Adds a blockquote to the document.
    /// </summary>
    /// <param name="text">The blockquote text.</param>
    void AddBlockquote(string text);

    /// <summary>
    /// Saves the document to disk.
    /// </summary>
    void Save();
}
