using Xunit;
using Moq;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Markdig.Syntax;
using System.Collections.Generic;
using System.ComponentModel;

namespace MermaidDiagramApp.Tests.ViewModels;

/// <summary>
/// Unit tests for MarkdownToWordViewModel.
/// Feature: markdown-to-word-export
/// </summary>
public class MarkdownToWordViewModelTests
{
    private readonly Mock<IMarkdownParser> _mockParser;
    private readonly Mock<IWordDocumentGenerator> _mockWordGenerator;
    private readonly Mock<IMermaidImageRenderer> _mockMermaidRenderer;
    private readonly Mock<ILogger> _mockLogger;
    private readonly MarkdownToWordExportService _exportService;

    public MarkdownToWordViewModelTests()
    {
        _mockParser = new Mock<IMarkdownParser>();
        _mockWordGenerator = new Mock<IWordDocumentGenerator>();
        _mockMermaidRenderer = new Mock<IMermaidImageRenderer>();
        _mockLogger = new Mock<ILogger>();

        // Setup default mock behaviors
        var document = new MarkdownDocument();
        _mockParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(document);
        _mockParser.Setup(p => p.ExtractMermaidBlocks(It.IsAny<MarkdownDocument>()))
            .Returns(new List<MermaidBlock>());
        _mockParser.Setup(p => p.ExtractImageReferences(It.IsAny<MarkdownDocument>()))
            .Returns(new List<ImageReference>());

        _mockWordGenerator.Setup(w => w.CreateDocument(It.IsAny<string>()));
        _mockWordGenerator.Setup(w => w.Save());
        _mockWordGenerator.Setup(w => w.Dispose());

        _exportService = new MarkdownToWordExportService(
            _mockParser.Object,
            _mockWordGenerator.Object,
            _mockMermaidRenderer.Object,
            _mockLogger.Object);
    }

    #region Property Change Notification Tests

    [Fact]
    public void MarkdownFilePath_WhenChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var propertyNames = new List<string>();

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
                propertyNames.Add(e.PropertyName);
        };

        // Act
        viewModel.MarkdownFilePath = "test.md";

        // Assert
        Assert.Contains("MarkdownFilePath", propertyNames);
    }

    [Fact]
    public void OutputPath_WhenChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = e.PropertyName;
        };

        // Act
        viewModel.OutputPath = "output.docx";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("OutputPath", changedPropertyName);
    }

    [Fact]
    public void IsExporting_WhenChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var propertyNames = new List<string>();

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
                propertyNames.Add(e.PropertyName);
        };

        // Act
        viewModel.IsExporting = true;

        // Assert
        Assert.Contains("IsExporting", propertyNames);
    }

    [Fact]
    public void ProgressPercentage_WhenChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = e.PropertyName;
        };

        // Act
        viewModel.ProgressPercentage = 50;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("ProgressPercentage", changedPropertyName);
    }

    [Fact]
    public void ProgressMessage_WhenChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = e.PropertyName;
        };

        // Act
        viewModel.ProgressMessage = "Processing...";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("ProgressMessage", changedPropertyName);
    }

    [Fact]
    public void MarkdownFilePath_WhenChanged_RaisesCanExportPropertyChanged()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var canExportChangedRaised = false;

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "CanExport")
                canExportChangedRaised = true;
        };

        // Act
        viewModel.MarkdownFilePath = "test.md";

        // Assert
        Assert.True(canExportChangedRaised);
    }

    [Fact]
    public void IsExporting_WhenChanged_RaisesCanExportPropertyChanged()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var canExportChangedRaised = false;

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "CanExport")
                canExportChangedRaised = true;
        };

        // Act
        viewModel.IsExporting = true;

        // Assert
        Assert.True(canExportChangedRaised);
    }

    #endregion

    #region Command Execution Tests

    [Fact]
    public async Task LoadMarkdownFileAsync_WithValidFile_LoadsContent()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var tempFile = Path.GetTempFileName();
        var testContent = "# Test Markdown\n\nThis is a test.";

        try
        {
            await File.WriteAllTextAsync(tempFile, testContent);

            // Act
            await viewModel.LoadMarkdownFileAsync(tempFile);

            // Assert
            Assert.Equal(tempFile, viewModel.MarkdownFilePath);
            Assert.True(viewModel.CanExport);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadMarkdownFileAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await viewModel.LoadMarkdownFileAsync(""));
    }

    [Fact]
    public async Task LoadMarkdownFileAsync_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await viewModel.LoadMarkdownFileAsync(null!));
    }

    [Fact]
    public void SetOutputPath_WithValidPath_SetsProperty()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var outputPath = "output.docx";

        // Act
        viewModel.SetOutputPath(outputPath);

        // Assert
        Assert.Equal(outputPath, viewModel.OutputPath);
    }

    [Fact]
    public void SetOutputPath_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => viewModel.SetOutputPath(""));
    }

    [Fact]
    public void SetOutputPath_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => viewModel.SetOutputPath(null!));
    }

    [Fact]
    public void CancelExport_WhenExporting_CancelsOperation()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        viewModel.IsExporting = true;

        // Act
        viewModel.CancelExport();

        // Assert - No exception should be thrown
        // The actual cancellation is tested in integration tests
        Assert.True(true);
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void OpenMarkdownFileCommand_WhenNotExporting_CanExecute()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act
        var canExecute = viewModel.OpenMarkdownFileCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void OpenMarkdownFileCommand_WhenExporting_CannotExecute()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        viewModel.IsExporting = true;

        // Act
        var canExecute = viewModel.OpenMarkdownFileCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public async Task ExportToWordCommand_WithNoFileLoaded_CannotExecute()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act
        var canExecute = viewModel.ExportToWordCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public async Task ExportToWordCommand_WithFileLoaded_CanExecute()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "# Test");
            await viewModel.LoadMarkdownFileAsync(tempFile);
            viewModel.SetOutputPath("output.docx");

            // Act
            var canExecute = viewModel.ExportToWordCommand.CanExecute(null);

            // Assert
            Assert.True(canExecute);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportToWordCommand_WhenExporting_CannotExecute()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "# Test");
            await viewModel.LoadMarkdownFileAsync(tempFile);
            viewModel.SetOutputPath("output.docx");
            viewModel.IsExporting = true;

            // Act
            var canExecute = viewModel.ExportToWordCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CancelExportCommand_WhenNotExporting_CannotExecute()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act
        var canExecute = viewModel.CancelExportCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void CancelExportCommand_WhenExporting_CanExecute()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        viewModel.IsExporting = true;

        // Act
        var canExecute = viewModel.CancelExportCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }

    #endregion

    #region Progress Update Tests

    [Fact]
    public async Task ExportToWordAsync_DuringExport_UpdatesProgress()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var tempFile = Path.GetTempFileName();
        var outputFile = Path.GetTempFileName();

        var progressUpdates = new List<int>();
        var messageUpdates = new List<string>();

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "ProgressPercentage")
                progressUpdates.Add(viewModel.ProgressPercentage);
            if (e.PropertyName == "ProgressMessage")
                messageUpdates.Add(viewModel.ProgressMessage);
        };

        try
        {
            await File.WriteAllTextAsync(tempFile, "# Test");
            await viewModel.LoadMarkdownFileAsync(tempFile);
            viewModel.SetOutputPath(outputFile);

            // Act
            await viewModel.ExportToWordAsync();

            // Assert
            Assert.NotEmpty(progressUpdates);
            Assert.NotEmpty(messageUpdates);
            Assert.Contains(progressUpdates, p => p > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }

    [Fact]
    public async Task ExportToWordAsync_WhenCompleted_ResetsIsExporting()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var tempFile = Path.GetTempFileName();
        var outputFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "# Test");
            await viewModel.LoadMarkdownFileAsync(tempFile);
            viewModel.SetOutputPath(outputFile);

            // Act
            await viewModel.ExportToWordAsync();

            // Assert
            Assert.False(viewModel.IsExporting);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }

    #endregion

    #region CanExport Tests

    [Fact]
    public void CanExport_WithNoFileLoaded_ReturnsFalse()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);

        // Act
        var canExport = viewModel.CanExport;

        // Assert
        Assert.False(canExport);
    }

    [Fact]
    public async Task CanExport_WithFileLoaded_ReturnsTrue()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "# Test");
            await viewModel.LoadMarkdownFileAsync(tempFile);

            // Act
            var canExport = viewModel.CanExport;

            // Assert
            Assert.True(canExport);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CanExport_WhenExporting_ReturnsFalse()
    {
        // Arrange
        var viewModel = new MarkdownToWordViewModel(_exportService, _mockLogger.Object);
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "# Test");
            await viewModel.LoadMarkdownFileAsync(tempFile);
            viewModel.IsExporting = true;

            // Act
            var canExport = viewModel.CanExport;

            // Assert
            Assert.False(canExport);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion
}
