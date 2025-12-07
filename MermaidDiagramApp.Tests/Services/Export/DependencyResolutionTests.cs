using Xunit;
using FsCheck;
using FsCheck.Xunit;
using System.Reflection;
using Markdig;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.IO;
using System.Linq;

namespace MermaidDiagramApp.Tests.Services.Export;

/// <summary>
/// Tests to verify that required NuGet packages are properly installed and resolvable.
/// Feature: markdown-to-word-export, Property 1: Package references are resolvable
/// Validates: Requirements 8.1, 8.2
/// </summary>
public class DependencyResolutionTests
{
    [Fact]
    public void Markdig_Package_IsResolvable()
    {
        // Arrange & Act
        var markdigAssembly = typeof(Markdown).Assembly;

        // Assert
        Assert.NotNull(markdigAssembly);
        Assert.Contains("Markdig", markdigAssembly.FullName);
    }

    [Fact]
    public void DocumentFormatOpenXml_Package_IsResolvable()
    {
        // Arrange & Act
        var openXmlAssembly = typeof(OpenXmlElement).Assembly;

        // Assert
        Assert.NotNull(openXmlAssembly);
        Assert.Contains("DocumentFormat.OpenXml", openXmlAssembly.FullName);
    }

    [Fact]
    public void DocumentFormatOpenXml_Packaging_IsResolvable()
    {
        // Arrange & Act
        var packagingAssembly = typeof(WordprocessingDocument).Assembly;

        // Assert
        Assert.NotNull(packagingAssembly);
        Assert.Contains("DocumentFormat.OpenXml", packagingAssembly.FullName);
    }

    [Fact]
    public void Export_Interfaces_AreResolvable()
    {
        // Arrange & Act
        var exportNamespace = "MermaidDiagramApp.Services.Export";
        var assembly = Assembly.Load("MermaidDiagramApp");
        var exportTypes = assembly.GetTypes()
            .Where(t => t.Namespace == exportNamespace)
            .ToList();

        // Assert
        Assert.NotEmpty(exportTypes);
        Assert.Contains(exportTypes, t => t.Name == "IMarkdownParser");
        Assert.Contains(exportTypes, t => t.Name == "IWordDocumentGenerator");
        Assert.Contains(exportTypes, t => t.Name == "IMermaidImageRenderer");
    }

    /// <summary>
    /// Property: For any valid Markdown string, the Markdig parser should be able to parse it without throwing exceptions.
    /// This verifies that Markdig is properly installed and functional.
    /// </summary>
    [Property(MaxTest = 100)]
    public void Markdig_CanParseAnyValidMarkdownString(string markdownContent)
    {
        // Arrange
        var content = markdownContent ?? string.Empty;

        // Act & Assert - Should not throw
        var exception = Record.Exception(() =>
        {
            var document = Markdown.Parse(content);
            Assert.NotNull(document);
        });

        // Markdig should handle any string gracefully
        Assert.Null(exception);
    }

    /// <summary>
    /// Property: We should be able to create a WordprocessingDocument without the packages throwing resolution errors.
    /// This verifies that DocumentFormat.OpenXml is properly installed.
    /// </summary>
    [Property(MaxTest = 100)]
    public void OpenXml_CanCreateDocumentWithoutResolutionErrors(int randomSeed)
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.docx");

        try
        {
            // Act - Create a document to verify the package is resolvable
            using (var doc = WordprocessingDocument.Create(tempPath, WordprocessingDocumentType.Document))
            {
                // Add minimal structure
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            }

            // Assert
            Assert.True(File.Exists(tempPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }
}
