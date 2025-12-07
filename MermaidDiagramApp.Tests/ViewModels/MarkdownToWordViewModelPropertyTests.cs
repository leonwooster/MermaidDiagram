using Xunit;
using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Markdig.Syntax;
using System.Collections.Generic;

namespace MermaidDiagramApp.Tests.ViewModels;

/// <summary>
/// Property-based tests for MarkdownToWordViewModel.
/// Feature: markdown-to-word-export
/// </summary>
public class MarkdownToWordViewModelPropertyTests
{
    /// <summary>
    /// Property 3: Command state reflects file loading
    /// For any application state, the "Export to Word" command should be enabled
    /// if and only if a Markdown file is successfully loaded.
    /// Validates: Requirements 1.5, 9.2
    /// </summary>
    [Property(MaxTest = 100)]
    public async Task ExportToWordCommand_CanExecute_ReflectsFileLoadingState(string markdownContent)
    {
        // Arrange: Filter out invalid inputs
        if (string.IsNullOrWhiteSpace(markdownContent))
            markdownContent = "# Test Content";

        // Create mocks
        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();
        var mockLogger = new Mock<ILogger>();

        // Setup parser
        var document = new MarkdownDocument();
        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var exportService = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            mockLogger.Object);

        var viewModel = new MarkdownToWordViewModel(exportService, mockLogger.Object);

        // Create a temporary file with the markdown content
        var tempFilePath = Path.GetTempFileName();
        
        try
        {
            await File.WriteAllTextAsync(tempFilePath, markdownContent);

            // Act & Assert: Before loading, CanExport should be false
            Assert.False(viewModel.CanExport, "CanExport should be false before loading a file");
            Assert.False(viewModel.ExportToWordCommand.CanExecute(null), 
                "ExportToWordCommand should not be executable before loading a file");

            // Load the file
            await viewModel.LoadMarkdownFileAsync(tempFilePath);

            // After loading, CanExport should be true
            Assert.True(viewModel.CanExport, "CanExport should be true after loading a file");
            
            // Set output path to enable export
            viewModel.SetOutputPath(Path.GetTempFileName());
            
            Assert.True(viewModel.ExportToWordCommand.CanExecute(null), 
                "ExportToWordCommand should be executable after loading a file");

            // Clear the file path
            viewModel.MarkdownFilePath = null;

            // After clearing, CanExport should be false again
            Assert.False(viewModel.CanExport, "CanExport should be false after clearing file path");
            Assert.False(viewModel.ExportToWordCommand.CanExecute(null), 
                "ExportToWordCommand should not be executable after clearing file path");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    /// <summary>
    /// Property 3 (variant): Command state during export
    /// For any application state where export is in progress, the "Export to Word" command
    /// should be disabled.
    /// Validates: Requirements 1.5, 9.2
    /// </summary>
    [Property(MaxTest = 100)]
    public void ExportToWordCommand_CanExecute_DisabledDuringExport(string markdownContent)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(markdownContent))
            markdownContent = "# Test Content";

        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();
        var mockLogger = new Mock<ILogger>();

        var document = new MarkdownDocument();
        mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        var exportService = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            mockLogger.Object);

        var viewModel = new MarkdownToWordViewModel(exportService, mockLogger.Object);

        // Act: Simulate export in progress
        viewModel.IsExporting = true;

        // Assert: Commands should be disabled during export
        Assert.False(viewModel.CanExport, "CanExport should be false during export");
        Assert.False(viewModel.ExportToWordCommand.CanExecute(null), 
            "ExportToWordCommand should not be executable during export");
        Assert.False(viewModel.OpenMarkdownFileCommand.CanExecute(null), 
            "OpenMarkdownFileCommand should not be executable during export");
        Assert.True(viewModel.CancelExportCommand.CanExecute(null), 
            "CancelExportCommand should be executable during export");

        // Act: Simulate export completion
        viewModel.IsExporting = false;

        // Assert: Cancel command should be disabled when not exporting
        Assert.False(viewModel.CancelExportCommand.CanExecute(null), 
            "CancelExportCommand should not be executable when not exporting");
    }

    /// <summary>
    /// Property 3 (variant): CanExport requires both file path and content
    /// For any application state, CanExport should only be true when both
    /// MarkdownFilePath is set and content is loaded.
    /// Validates: Requirements 1.5, 9.2
    /// </summary>
    [Property(MaxTest = 100)]
    public void CanExport_RequiresBothFilePathAndContent(string filePath)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(filePath))
            filePath = "test.md";

        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();
        var mockLogger = new Mock<ILogger>();

        var exportService = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            mockLogger.Object);

        var viewModel = new MarkdownToWordViewModel(exportService, mockLogger.Object);

        // Act & Assert: With no file path, CanExport should be false
        Assert.False(viewModel.CanExport, "CanExport should be false with no file path");

        // Set file path but don't load content
        viewModel.MarkdownFilePath = filePath;

        // CanExport should still be false (no content loaded)
        Assert.False(viewModel.CanExport, "CanExport should be false with file path but no content");

        // Clear file path
        viewModel.MarkdownFilePath = null;

        // CanExport should remain false
        Assert.False(viewModel.CanExport, "CanExport should be false after clearing file path");
    }

    /// <summary>
    /// Property 3 (variant): Property change notifications
    /// For any property change that affects CanExport, PropertyChanged event should be raised.
    /// Validates: Requirements 1.5, 9.2
    /// </summary>
    [Property(MaxTest = 100)]
    public void PropertyChanges_RaisePropertyChangedEvent(string filePath, bool isExporting)
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(filePath))
            filePath = "test.md";

        var mockParser = new Mock<IMarkdownParser>();
        var mockWordGenerator = new Mock<IWordDocumentGenerator>();
        var mockMermaidRenderer = new Mock<IMermaidImageRenderer>();
        var mockLogger = new Mock<ILogger>();

        var exportService = new MarkdownToWordExportService(
            mockParser.Object,
            mockWordGenerator.Object,
            mockMermaidRenderer.Object,
            mockLogger.Object);

        var viewModel = new MarkdownToWordViewModel(exportService, mockLogger.Object);

        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
                propertyChangedEvents.Add(e.PropertyName);
        };

        // Act: Change MarkdownFilePath
        viewModel.MarkdownFilePath = filePath;

        // Assert: PropertyChanged should be raised for MarkdownFilePath and CanExport
        Assert.Contains("MarkdownFilePath", propertyChangedEvents);
        Assert.Contains("CanExport", propertyChangedEvents);

        propertyChangedEvents.Clear();

        // Act: Change IsExporting (only if it's different from current value)
        // Set to opposite of the test value first to ensure a change occurs
        viewModel.IsExporting = !isExporting;
        propertyChangedEvents.Clear(); // Clear events from the first change
        
        viewModel.IsExporting = isExporting;

        // Assert: PropertyChanged should be raised for IsExporting and CanExport
        Assert.Contains("IsExporting", propertyChangedEvents);
        Assert.Contains("CanExport", propertyChangedEvents);

        propertyChangedEvents.Clear();

        // Act: Change OutputPath
        viewModel.OutputPath = "output.docx";

        // Assert: PropertyChanged should be raised for OutputPath
        Assert.Contains("OutputPath", propertyChangedEvents);
    }
}
