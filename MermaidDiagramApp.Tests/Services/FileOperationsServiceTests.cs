using System.Runtime.CompilerServices;
using Moq;
using Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for FileOperationsService.
/// Requirements: 6.4
/// </summary>
public class FileOperationsServiceTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly MermaidTextOptimizer _mermaidTextOptimizer;
    private readonly DiagramFileService _diagramFileService;
    private readonly RecentFilesService _recentFilesService;

    public FileOperationsServiceTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mermaidTextOptimizer = new MermaidTextOptimizer(_mockLogger.Object);
        _diagramFileService = new DiagramFileService();
        // RecentFilesService constructor accesses ApplicationData.Current.LocalFolder
        // which is unavailable in unit tests. Use GetUninitializedObject to bypass the constructor.
        _recentFilesService = (RecentFilesService)RuntimeHelpers.GetUninitializedObject(typeof(RecentFilesService));
    }

    private FileOperationsService CreateService()
    {
        return new FileOperationsService(
            _diagramFileService,
            _recentFilesService,
            _mermaidTextOptimizer,
            _mockLogger.Object);
    }

    #region GetWindowTitle Tests

    [Fact]
    public void GetWindowTitle_WithNullFilePath_ReturnsMermaidDiagramEditor()
    {
        var service = CreateService();
        var result = service.GetWindowTitle(null);
        Assert.Equal("Mermaid Diagram Editor", result);
    }

    [Fact]
    public void GetWindowTitle_WithEmptyFilePath_ReturnsMermaidDiagramEditor()
    {
        var service = CreateService();
        var result = service.GetWindowTitle(string.Empty);
        Assert.Equal("Mermaid Diagram Editor", result);
    }

    [Fact]
    public void GetWindowTitle_WithFullPath_ReturnsFileNameWithAppTitle()
    {
        var service = CreateService();
        var result = service.GetWindowTitle(@"C:\path\to\filename.mmd");
        Assert.Equal("filename.mmd - Mermaid Diagram Editor", result);
    }

    [Fact]
    public void GetWindowTitle_WithFileNameOnly_ReturnsFileNameWithAppTitle()
    {
        var service = CreateService();
        var result = service.GetWindowTitle("mydiagram.mmd");
        Assert.Equal("mydiagram.mmd - Mermaid Diagram Editor", result);
    }

    [Fact]
    public void GetWindowTitle_WithMarkdownFile_ReturnsFileNameWithAppTitle()
    {
        var service = CreateService();
        var result = service.GetWindowTitle(@"C:\docs\readme.md");
        Assert.Equal("readme.md - Mermaid Diagram Editor", result);
    }

    #endregion

    #region OptimizeMermaidContent Tests

    [Fact]
    public void OptimizeMermaidContent_WithLongLabels_AddsLineBreaks()
    {
        var service = CreateService();
        var content = @"graph TB
    node1[""This is a very long node label that should be split into multiple lines""]";

        var result = service.OptimizeMermaidContent(content);

        Assert.Contains("<br/>", result);
    }

    [Fact]
    public void OptimizeMermaidContent_WithShortContent_ReturnsUnchanged()
    {
        var service = CreateService();
        var content = @"graph TB
    A[""Short""]";

        var result = service.OptimizeMermaidContent(content);

        Assert.Equal(content, result);
    }

    [Fact]
    public void OptimizeMermaidContent_WithEmptyContent_ReturnsEmpty()
    {
        var service = CreateService();
        var result = service.OptimizeMermaidContent("");
        Assert.Equal("", result);
    }

    #endregion

    #region NeedsMermaidOptimization Tests

    [Fact]
    public void NeedsMermaidOptimization_WithLongLabels_ReturnsTrue()
    {
        var service = CreateService();
        var content = @"graph TB
    node1[""This is a very long node label that should be optimized for better display""]";

        Assert.True(service.NeedsMermaidOptimization(content));
    }

    [Fact]
    public void NeedsMermaidOptimization_WithShortLabels_ReturnsFalse()
    {
        var service = CreateService();
        var content = @"graph TB
    A[""Short""]
    B[""Also Short""]";

        Assert.False(service.NeedsMermaidOptimization(content));
    }

    [Fact]
    public void NeedsMermaidOptimization_WithAlreadyOptimized_ReturnsFalse()
    {
        var service = CreateService();
        var content = @"graph TB
    node1[""Already<br/>Optimized<br/>Label""]";

        Assert.False(service.NeedsMermaidOptimization(content));
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_WithNullDiagramFileService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new FileOperationsService(null!, _recentFilesService, _mermaidTextOptimizer, _mockLogger.Object));
        Assert.Equal("diagramFileService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRecentFilesService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new FileOperationsService(_diagramFileService, null!, _mermaidTextOptimizer, _mockLogger.Object));
        Assert.Equal("recentFilesService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullMermaidTextOptimizer_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new FileOperationsService(_diagramFileService, _recentFilesService, null!, _mockLogger.Object));
        Assert.Equal("mermaidTextOptimizer", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new FileOperationsService(_diagramFileService, _recentFilesService, _mermaidTextOptimizer, null!));
        Assert.Equal("logger", ex.ParamName);
    }

    #endregion
}
