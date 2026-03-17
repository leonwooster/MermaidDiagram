using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Xunit;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Services.Rendering;

namespace MermaidDiagramApp.Tests.ViewModels;

/// <summary>
/// Property-based tests for MainWindowViewModel.
/// Feature: mainwindow-refactoring
/// </summary>
public class MainWindowViewModelPropertyTests
{
    /// <summary>
    /// Creates a MainWindowViewModel with all dependencies mocked.
    /// </summary>
    private static MainWindowViewModel CreateViewModelWithMockedDependencies()
    {
        var mockFileOps = new Mock<IFileOperationsService>();
        var mockSearch = new Mock<ISearchService>();
        var mockUpdateService = new Mock<IMermaidUpdateService>();
        var mockExportService = new Mock<IExportService>();
        var mockContentTypeDetector = new Mock<IContentTypeDetector>();
        var mockLogger = new Mock<ILogger>();

        // RenderingOrchestrator requires IContentTypeDetector and ContentRendererFactory
        // ContentRendererFactory has a parameterless constructor
        var contentRendererFactory = new ContentRendererFactory();
        var renderingOrchestrator = new RenderingOrchestrator(
            mockContentTypeDetector.Object,
            contentRendererFactory);

        // MarkdownStyleSettingsService uses ApplicationData.Current.LocalSettings which
        // is not available in unit tests. Use RuntimeHelpers.GetUninitializedObject to
        // create an instance without calling the constructor.
        var styleSettingsService = (MarkdownStyleSettingsService)RuntimeHelpers.GetUninitializedObject(
            typeof(MarkdownStyleSettingsService));

        return new MainWindowViewModel(
            mockFileOps.Object,
            mockSearch.Object,
            mockUpdateService.Object,
            mockExportService.Object,
            renderingOrchestrator,
            mockContentTypeDetector.Object,
            styleSettingsService,
            mockLogger.Object);
    }

    /// <summary>
    /// Property 2: ViewModel exposes non-null commands
    /// For any expected command name in the MainWindowViewModel, the corresponding
    /// ICommand property should be non-null after construction.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    public void AllCommands_AfterConstruction_AreNonNull()
    {
        // Arrange
        var viewModel = CreateViewModelWithMockedDependencies();

        // Act & Assert - Verify all 19 ICommand properties are non-null
        // New diagram commands (8)
        Assert.NotNull(viewModel.NewClassDiagramCommand);
        Assert.NotNull(viewModel.NewSequenceDiagramCommand);
        Assert.NotNull(viewModel.NewStateDiagramCommand);
        Assert.NotNull(viewModel.NewActivityDiagramCommand);
        Assert.NotNull(viewModel.NewFlowchartCommand);
        Assert.NotNull(viewModel.NewGanttChartCommand);
        Assert.NotNull(viewModel.NewPieChartCommand);
        Assert.NotNull(viewModel.NewGitGraphCommand);

        // File operation commands (5)
        Assert.NotNull(viewModel.OpenFileCommand);
        Assert.NotNull(viewModel.SaveFileCommand);
        Assert.NotNull(viewModel.CloseFileCommand);
        Assert.NotNull(viewModel.ExportSvgCommand);
        Assert.NotNull(viewModel.ExportPngCommand);

        // View toggle commands (3)
        Assert.NotNull(viewModel.ToggleFullScreenCommand);
        Assert.NotNull(viewModel.TogglePresentationModeCommand);
        Assert.NotNull(viewModel.ToggleBuilderCommand);

        // Other commands (3)
        Assert.NotNull(viewModel.FindCommand);
        Assert.NotNull(viewModel.CheckSyntaxCommand);
        Assert.NotNull(viewModel.ExitCommand);
    }

    /// <summary>
    /// Property 2 (variant): All commands implement ICommand interface
    /// Verifies that all command properties are properly typed as ICommand.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    public void AllCommands_AfterConstruction_ImplementICommand()
    {
        // Arrange
        var viewModel = CreateViewModelWithMockedDependencies();

        // Act & Assert - Verify all commands are ICommand instances
        var commands = new ICommand[]
        {
            viewModel.NewClassDiagramCommand,
            viewModel.NewSequenceDiagramCommand,
            viewModel.NewStateDiagramCommand,
            viewModel.NewActivityDiagramCommand,
            viewModel.NewFlowchartCommand,
            viewModel.NewGanttChartCommand,
            viewModel.NewPieChartCommand,
            viewModel.NewGitGraphCommand,
            viewModel.OpenFileCommand,
            viewModel.SaveFileCommand,
            viewModel.CloseFileCommand,
            viewModel.ExportSvgCommand,
            viewModel.ExportPngCommand,
            viewModel.ToggleFullScreenCommand,
            viewModel.TogglePresentationModeCommand,
            viewModel.ToggleBuilderCommand,
            viewModel.FindCommand,
            viewModel.CheckSyntaxCommand,
            viewModel.ExitCommand
        };

        Assert.Equal(19, commands.Length);
        foreach (var command in commands)
        {
            Assert.IsAssignableFrom<ICommand>(command);
        }
    }

    /// <summary>
    /// Property 2 (variant): All commands can execute by default
    /// Verifies that all commands return true for CanExecute with null parameter.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    public void AllCommands_AfterConstruction_CanExecuteByDefault()
    {
        // Arrange
        var viewModel = CreateViewModelWithMockedDependencies();

        // Act & Assert - Verify all commands can execute
        Assert.True(viewModel.NewClassDiagramCommand.CanExecute(null));
        Assert.True(viewModel.NewSequenceDiagramCommand.CanExecute(null));
        Assert.True(viewModel.NewStateDiagramCommand.CanExecute(null));
        Assert.True(viewModel.NewActivityDiagramCommand.CanExecute(null));
        Assert.True(viewModel.NewFlowchartCommand.CanExecute(null));
        Assert.True(viewModel.NewGanttChartCommand.CanExecute(null));
        Assert.True(viewModel.NewPieChartCommand.CanExecute(null));
        Assert.True(viewModel.NewGitGraphCommand.CanExecute(null));
        Assert.True(viewModel.OpenFileCommand.CanExecute(null));
        Assert.True(viewModel.SaveFileCommand.CanExecute(null));
        Assert.True(viewModel.CloseFileCommand.CanExecute(null));
        Assert.True(viewModel.ExportSvgCommand.CanExecute(null));
        Assert.True(viewModel.ExportPngCommand.CanExecute(null));
        Assert.True(viewModel.ToggleFullScreenCommand.CanExecute(null));
        Assert.True(viewModel.TogglePresentationModeCommand.CanExecute(null));
        Assert.True(viewModel.ToggleBuilderCommand.CanExecute(null));
        Assert.True(viewModel.FindCommand.CanExecute(null));
        Assert.True(viewModel.CheckSyntaxCommand.CanExecute(null));
        Assert.True(viewModel.ExitCommand.CanExecute(null));
    }

    #region Property 3: ViewModel fires PropertyChanged for all bindable properties

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for CurrentFilePath
    /// Setting CurrentFilePath to a generated value fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void CurrentFilePath_WhenSet_FiresPropertyChanged(NonNull<string> value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        // Ensure the initial value differs from the generated value
        var newValue = value.Get;
        var initialValue = newValue == "initial" ? "other" : "initial";
        viewModel.CurrentFilePath = initialValue;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.CurrentFilePath = newValue;

        Assert.Contains(nameof(MainWindowViewModel.CurrentFilePath), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for CurrentContentType
    /// Setting CurrentContentType to a generated value fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void CurrentContentType_WhenSet_FiresPropertyChanged(ContentType value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        // Set initial to a different value so the change is detected
        var initial = value == ContentType.Unknown ? ContentType.Mermaid : ContentType.Unknown;
        viewModel.CurrentContentType = initial;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.CurrentContentType = value;

        Assert.Contains(nameof(MainWindowViewModel.CurrentContentType), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for IsFullScreen
    /// Toggling IsFullScreen fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void IsFullScreen_WhenSet_FiresPropertyChanged(bool value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        viewModel.IsFullScreen = !value;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.IsFullScreen = value;

        Assert.Contains(nameof(MainWindowViewModel.IsFullScreen), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for IsPresentationMode
    /// Toggling IsPresentationMode fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void IsPresentationMode_WhenSet_FiresPropertyChanged(bool value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        viewModel.IsPresentationMode = !value;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.IsPresentationMode = value;

        Assert.Contains(nameof(MainWindowViewModel.IsPresentationMode), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for IsPanModeEnabled
    /// Toggling IsPanModeEnabled fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void IsPanModeEnabled_WhenSet_FiresPropertyChanged(bool value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        viewModel.IsPanModeEnabled = !value;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.IsPanModeEnabled = value;

        Assert.Contains(nameof(MainWindowViewModel.IsPanModeEnabled), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for IsBuilderVisible
    /// Toggling IsBuilderVisible fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void IsBuilderVisible_WhenSet_FiresPropertyChanged(bool value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        viewModel.IsBuilderVisible = !value;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.IsBuilderVisible = value;

        Assert.Contains(nameof(MainWindowViewModel.IsBuilderVisible), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for CurrentSearchText
    /// Setting CurrentSearchText to a generated value fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void CurrentSearchText_WhenSet_FiresPropertyChanged(NonNull<string> value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        var newValue = value.Get;
        var initialValue = newValue == "initial" ? "other" : "initial";
        viewModel.CurrentSearchText = initialValue;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.CurrentSearchText = newValue;

        Assert.Contains(nameof(MainWindowViewModel.CurrentSearchText), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for LastPreviewedCode
    /// Setting LastPreviewedCode to a generated value fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void LastPreviewedCode_WhenSet_FiresPropertyChanged(NonNull<string> value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        var newValue = value.Get;
        var initialValue = newValue == "initial" ? "other" : "initial";
        viewModel.LastPreviewedCode = initialValue;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.LastPreviewedCode = newValue;

        Assert.Contains(nameof(MainWindowViewModel.LastPreviewedCode), firedProperties);
    }

    /// <summary>
    /// Property 3: ViewModel fires PropertyChanged for IsWebViewReady
    /// Toggling IsWebViewReady fires PropertyChanged with the correct name.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [Property(MaxTest = 10)]
    public void IsWebViewReady_WhenSet_FiresPropertyChanged(bool value)
    {
        var viewModel = CreateViewModelWithMockedDependencies();
        viewModel.IsWebViewReady = !value;

        var firedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        viewModel.IsWebViewReady = value;

        Assert.Contains(nameof(MainWindowViewModel.IsWebViewReady), firedProperties);
    }

    #endregion

    #region Property 4: All injectable types accept mocked dependencies

    /// <summary>
    /// Property 4: MainWindowViewModel accepts mocked dependencies
    /// Constructing MainWindowViewModel with mocked interface dependencies succeeds and produces a non-null instance.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Fact]
    public void MainWindowViewModel_WithMockedDependencies_CreatesNonNullInstance()
    {
        // Arrange & Act
        var viewModel = CreateViewModelWithMockedDependencies();

        // Assert
        Assert.NotNull(viewModel);
    }

    /// <summary>
    /// Property 4: FileOperationsService accepts mocked dependencies
    /// Constructing FileOperationsService with mocked/uninitialized dependencies succeeds and produces a non-null instance.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Fact]
    public void FileOperationsService_WithMockedDependencies_CreatesNonNullInstance()
    {
        // Arrange
        // DiagramFileService, RecentFilesService, MermaidTextOptimizer are concrete classes
        // that require ApplicationData.Current which is unavailable in unit tests.
        // Use RuntimeHelpers.GetUninitializedObject to create instances without calling constructors.
        var diagramFileService = (DiagramFileService)RuntimeHelpers.GetUninitializedObject(typeof(DiagramFileService));
        var recentFilesService = (RecentFilesService)RuntimeHelpers.GetUninitializedObject(typeof(RecentFilesService));
        var mermaidTextOptimizer = (MermaidTextOptimizer)RuntimeHelpers.GetUninitializedObject(typeof(MermaidTextOptimizer));
        var mockLogger = new Mock<ILogger>();

        // Act
        var service = new FileOperationsService(
            diagramFileService,
            recentFilesService,
            mermaidTextOptimizer,
            mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// Property 4: SearchService accepts mocked dependencies
    /// Constructing SearchService produces a non-null instance.
    /// Note: SearchService has a parameterless constructor (no dependencies to inject).
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Fact]
    public void SearchService_WithMockedDependencies_CreatesNonNullInstance()
    {
        // Arrange & Act
        var service = new SearchService();

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// Property 4: MermaidUpdateService accepts mocked dependencies
    /// Constructing MermaidUpdateService with mocked ILogger succeeds and produces a non-null instance.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Fact]
    public void MermaidUpdateService_WithMockedDependencies_CreatesNonNullInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();

        // Act
        var service = new MermaidUpdateService(mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// Property 4: ExportService accepts mocked dependencies
    /// Constructing ExportService with mocked ILogger succeeds and produces a non-null instance.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Fact]
    public void ExportService_WithMockedDependencies_CreatesNonNullInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();

        // Act
        var service = new ExportService(mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// Property 4 (combined): All injectable types accept mocked dependencies
    /// Verifies that all five newly created injectable types can be constructed with mocked dependencies.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Fact]
    public void AllInjectableTypes_WithMockedDependencies_CreateNonNullInstances()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();

        // Act - Create all injectable types
        var mainWindowViewModel = CreateViewModelWithMockedDependencies();

        var diagramFileService = (DiagramFileService)RuntimeHelpers.GetUninitializedObject(typeof(DiagramFileService));
        var recentFilesService = (RecentFilesService)RuntimeHelpers.GetUninitializedObject(typeof(RecentFilesService));
        var mermaidTextOptimizer = (MermaidTextOptimizer)RuntimeHelpers.GetUninitializedObject(typeof(MermaidTextOptimizer));
        var fileOperationsService = new FileOperationsService(
            diagramFileService,
            recentFilesService,
            mermaidTextOptimizer,
            mockLogger.Object);

        var searchService = new SearchService();
        var mermaidUpdateService = new MermaidUpdateService(mockLogger.Object);
        var exportService = new ExportService(mockLogger.Object);

        // Assert - All instances are non-null
        Assert.NotNull(mainWindowViewModel);
        Assert.NotNull(fileOperationsService);
        Assert.NotNull(searchService);
        Assert.NotNull(mermaidUpdateService);
        Assert.NotNull(exportService);
    }

    #endregion
}
