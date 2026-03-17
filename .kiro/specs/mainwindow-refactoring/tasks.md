# Implementation Plan: MainWindow Comprehensive Refactoring

## Overview

This plan refactors the MermaidDiagramApp incrementally across 4 phases: DI Container → Service Extraction → ViewModel Extraction → Partial Class Splitting. Each phase produces a compilable, testable application. All existing tests must pass after every task.

## Tasks

- [x] 1. Set up DI container infrastructure
  - [x] 1.1 Add Microsoft.Extensions.DependencyInjection NuGet package to MermaidDiagramApp.csproj
    - Add `Microsoft.Extensions.DependencyInjection` package reference to `MermaidDiagramApp/MermaidDiagramApp.csproj`
    - Verify the solution builds after adding the package
    - _Requirements: 1.1_

  - [x] 1.2 Create DI container configuration in App.xaml.cs
    - Add a static `IServiceProvider Services` property to the `App` class in `MermaidDiagramApp/App.xaml.cs`
    - Create a `ConfigureServices()` method that builds a `ServiceCollection` and registers all existing services with appropriate lifetimes:
      - Singletons: LoggingService/ILogger, ContentTypeDetector (as IContentTypeDetector), ContentRendererFactory, RenderingOrchestrator, DiagramFileService, RecentFilesService, MarkdownStyleSettingsService, ShortcutPreferencesService, WindowStateManager, MermaidLinter, AiConfigStorageService, AiServiceFactory, KeyboardShortcutManager, FontManager
      - Transient: MermaidSyntaxAnalyzer, MermaidSyntaxFixer, MermaidTextOptimizer
    - Call `ConfigureServices()` in the App constructor and assign to `Services`
    - _Requirements: 1.1, 1.2_

  - [x] 1.3 Update MainWindow constructor to receive services from DI container
    - Modify `MainWindow` constructor to accept injected services as parameters (at minimum: RenderingOrchestrator, IContentTypeDetector, ContentRendererFactory, DiagramFileService, RecentFilesService, MarkdownStyleSettingsService, MermaidLinter, ILogger)
    - Update `App.OnLaunched` to resolve MainWindow dependencies from the container and pass them to the constructor
    - Remove manual `new` instantiation of services from the MainWindow constructor body
    - Verify the application builds and all existing tests pass
    - _Requirements: 1.4, 5.1, 5.2_

  - [x] 1.4 Write property test for DI container service resolution
    - **Property 1: DI container resolves all registered services**
    - Build the container using the same `ConfigureServices()` method, then for each registered service type verify `GetRequiredService` returns a non-null instance
    - Test file: `MermaidDiagramApp.Tests/Services/DiContainerPropertyTests.cs`
    - **Validates: Requirements 1.1, 1.2, 3.5**

- [x] 2. Checkpoint - Verify DI container integration
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Extract new service interfaces and implementations
  - [x] 3.1 Create IFileOperationsService and FileOperationsService
    - Create `MermaidDiagramApp/Services/IFileOperationsService.cs` interface with methods: ReadFileAsync, SaveFileAsync, LoadDiagramAsync, SaveDiagramAsync, OptimizeMermaidContent, NeedsMermaidOptimization, AddRecentFile, GetRecentFiles, RemoveRecentFile, ClearRecentFiles, GetWindowTitle
    - Create `MermaidDiagramApp/Services/FileOperationsService.cs` implementation that wraps existing DiagramFileService, RecentFilesService, and MermaidTextOptimizer via constructor injection
    - Extract file open/save/close and recent files logic from MainWindow event handlers into the service
    - Register IFileOperationsService → FileOperationsService as singleton in the DI container
    - _Requirements: 3.1, 3.5_

  - [x] 3.2 Create ISearchService and SearchService
    - Create `MermaidDiagramApp/Services/ISearchService.cs` interface with: SetSearchText, FindNext, FindPrevious, Reset, CurrentSearchText propertyIrene Azuela
    - Create `MermaidDiagramApp/Services/SearchService.cs` implementation
    - Create `MermaidDiagramApp/Models/SearchResult.cs` record: `SearchResult(bool Found, int MatchIndex, int MatchLength, string StatusMessage)`
    - Extract search state management logic from MainWindow search-related methods
    - Register ISearchService → SearchService as singleton in the DI container
    - _Requirements: 3.2, 3.5_

  - [x] 3.3 Create IMermaidUpdateService and MermaidUpdateService
    - Create `MermaidDiagramApp/Services/IMermaidUpdateService.cs` interface with: CheckForUpdatesAsync, DownloadAndInstallUpdateAsync, GetCurrentVersion
    - Create `MermaidDiagramApp/Services/MermaidUpdateService.cs` implementation
    - Create `MermaidDiagramApp/Models/MermaidVersionInfo.cs` record: `MermaidVersionInfo(string CurrentVersion, string LatestVersion, bool UpdateAvailable)`
    - Extract Mermaid version checking, comparison, and download logic from MainWindow (CheckForMermaidUpdatesAsync, CheckForNewerVersionAsync, UpdateMermaid_Click)
    - Register IMermaidUpdateService → MermaidUpdateService as singleton in the DI container
    - _Requirements: 3.3, 3.5_

  - [x] 3.4 Create IExportService and ExportService
    - Create `MermaidDiagramApp/Services/IExportService.cs` interface with: AddBackgroundToSvg, ScaleImageAsync, SaveSvgAsync, SavePngAsync
    - Create `MermaidDiagramApp/Services/ExportService.cs` implementation
    - Extract SVG background insertion and PNG scaling logic from MainWindow (ExportSvg_Click, ExportPng_Click, ExportPngFallback, AddBackgroundToSvg)
    - Register IExportService → ExportService as singleton in the DI container
    - _Requirements: 3.4, 3.5_

  - [x] 3.5 Wire new services into MainWindow and remove extracted logic
    - Update MainWindow constructor to accept the four new services via DI
    - Replace inline business logic in event handlers with calls to the new services
    - Ensure MainWindow code-behind retains only UI-specific wiring (dialog presentation, file pickers, WebView2 interop)
    - Verify the application builds and all existing tests pass
    - _Requirements: 3.6, 5.1, 5.2_

  - [x] 3.6 Write unit tests for FileOperationsService
    - Test file: `MermaidDiagramApp.Tests/Services/FileOperationsServiceTests.cs`
    - Test GetWindowTitle with various file paths (full path, null, empty)
    - Test OptimizeMermaidContent and NeedsMermaidOptimization with mocked MermaidTextOptimizer
    - Test AddRecentFile and GetRecentFiles with mocked RecentFilesService
    - _Requirements: 6.4_

  - [x] 3.7 Write unit tests for SearchService
    - Test file: `MermaidDiagramApp.Tests/Services/SearchServiceTests.cs`
    - Test FindNext and FindPrevious with known text and search terms
    - Test Reset clears CurrentSearchText and state
    - Test SetSearchText updates CurrentSearchText
    - _Requirements: 6.4_

  - [x] 3.8 Write unit tests for MermaidUpdateService
    - Test file: `MermaidDiagramApp.Tests/Services/MermaidUpdateServiceTests.cs`
    - Test GetCurrentVersion returns expected format
    - Test CheckForUpdatesAsync handles HTTP failures gracefully (returns UpdateAvailable=false)
    - _Requirements: 6.4_

  - [x] 3.9 Write unit tests for ExportService
    - Test file: `MermaidDiagramApp.Tests/Services/ExportServiceTests.cs`
    - Test AddBackgroundToSvg with valid SVG, malformed SVG, and empty input
    - Test SaveSvgAsync writes correct content to file
    - _Requirements: 6.4_

- [x] 4. Checkpoint - Verify service extraction
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Extract MainWindowViewModel
  - [x] 5.1 Create MainWindowViewModel class with UI state properties
    - Create `MermaidDiagramApp/ViewModels/MainWindowViewModel.cs`
    - Move UI state fields from MainWindow into ViewModel properties: CurrentFilePath, CurrentContentType, IsFullScreen, IsPresentationMode, IsPanModeEnabled, IsBuilderVisible, CurrentSearchText, LastPreviewedCode, IsWebViewReady
    - Implement INotifyPropertyChanged for all bindable properties using a shared `SetProperty<T>` helper
    - Accept services via constructor injection: IFileOperationsService, ISearchService, IMermaidUpdateService, IExportService, RenderingOrchestrator, IContentTypeDetector, MarkdownStyleSettingsService, ILogger
    - _Requirements: 2.1, 2.3, 6.1_

  - [x] 5.2 Add ICommand properties to MainWindowViewModel
    - Create commands using the existing RelayCommand class for: NewClassDiagramCommand, NewSequenceDiagramCommand, NewStateDiagramCommand, NewActivityDiagramCommand, NewFlowchartCommand, NewGanttChartCommand, NewPieChartCommand, NewGitGraphCommand, OpenFileCommand, SaveFileCommand, CloseFileCommand, ExportSvgCommand, ExportPngCommand, ToggleFullScreenCommand, TogglePresentationModeCommand, ToggleBuilderCommand, FindCommand, CheckSyntaxCommand, ExitCommand
    - Implement command logic that delegates to injected services
    - Use callback delegates (Action/Func) for operations requiring UI interaction (file pickers, dialogs, WebView2)
    - _Requirements: 2.2, 2.4_

  - [x] 5.3 Wire MainWindowViewModel into MainWindow
    - Register MainWindowViewModel as transient in the DI container in App.xaml.cs
    - Update MainWindow constructor to accept MainWindowViewModel
    - Set `DataContext = viewModel` for data binding
    - Replace direct state field access in code-behind with ViewModel property access
    - Route UI event handlers to ViewModel commands (e.g., menu clicks call `ViewModel.OpenFileCommand.Execute`)
    - Verify the application builds and all existing tests pass
    - _Requirements: 1.3, 2.4, 2.5, 5.1, 5.2_

  - [x] 5.4 Write property test for ViewModel commands non-nullity
    - **Property 2: ViewModel exposes non-null commands**
    - Test file: `MermaidDiagramApp.Tests/ViewModels/MainWindowViewModelPropertyTests.cs`
    - Construct MainWindowViewModel with mocked dependencies, then verify every ICommand property is non-null
    - **Validates: Requirements 2.2**

  - [x] 5.5 Write property test for ViewModel PropertyChanged notifications
    - **Property 3: ViewModel fires PropertyChanged for all bindable properties**
    - Test file: `MermaidDiagramApp.Tests/ViewModels/MainWindowViewModelPropertyTests.cs`
    - For each bindable property, set it to a generated value and verify PropertyChanged fires with the correct property name
    - **Validates: Requirements 2.3**

  - [x] 5.6 Write property test for constructor injection with mocks
    - **Property 4: All injectable types accept mocked dependencies**
    - Test file: `MermaidDiagramApp.Tests/ViewModels/MainWindowViewModelPropertyTests.cs`
    - For each newly created injectable type (MainWindowViewModel, FileOperationsService, SearchService, MermaidUpdateService, ExportService), construct with mocked interface dependencies and verify non-null instance
    - **Validates: Requirements 6.1, 6.2**

- [x] 6. Checkpoint - Verify ViewModel extraction
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Split MainWindow code-behind into partial class files
  - [x] 7.1 Create MainWindow.WebView.cs partial class
    - Create `MermaidDiagramApp/MainWindow.WebView.cs` with partial class declaration
    - Move WebView2 initialization (InitializeWebViewAsync), message handling (PreviewBrowser_WebMessageReceived), UpdatePreview, ExecuteRenderingScript, OnRenderingStateChanged, UpdateRenderModeIndicator, UpdateContentTypeIndicator, render mode override handlers, zoom control methods, UpdatePanMode, CopyAssetsToLocalFolder, CopyDirectory, PreviewBrowser_NavigationCompleted, Timer_Tick
    - Add necessary using statements and file header comment
    - _Requirements: 4.2_

  - [x] 7.2 Create MainWindow.UI.cs partial class
    - Create `MermaidDiagramApp/MainWindow.UI.cs` with partial class declaration
    - Move menu click handlers (NewClassDiagram_Click, NewSequenceDiagram_Click, etc.), Open_Click, Save_Click, Close_Click, ExportSvg_Click, ExportPng_Click, ExportPngFallback, Exit_Click, About_Click, OpenLogFile_Click, OpenLogFolder_Click, MarkdownStyleSettings_Click, CheckSyntax_Click, ToggleFullScreen_Click, PresentationMode_Click, MainWindow_KeyDown, PreviewBrowser_KeyDown, DragModeToggle_Click, ShowMessageAsync, ShowUnsavedChangesDialog, PopulateRecentFilesMenu, RecentFile_Click, OpenRecentFile, ClearRecentFiles_Click, UpdateWindowTitle, LoadMarkdownFileForExport, FileExistsAsync
    - Add necessary using statements and file header comment
    - _Requirements: 4.3_

  - [x] 7.3 Create MainWindow.Builder.cs partial class
    - Create `MermaidDiagramApp/MainWindow.Builder.cs` with partial class declaration
    - Move BuilderTool_Click, UpdateBuilderVisibility, DiagramBuilderViewModel_PropertyChanged, and builder panel wiring from MainWindow_Loaded
    - Add necessary using statements and file header comment
    - _Requirements: 4.4_

  - [x] 7.4 Create MainWindow.Search.cs partial class
    - Create `MermaidDiagramApp/MainWindow.Search.cs` with partial class declaration
    - Move Find_Click, CloseSearch_Click, SearchTextBox_TextChanged, SearchTextBox_KeyDown, FindNext_Click, FindPrevious_Click, PerformSearch
    - Add necessary using statements and file header comment
    - _Requirements: 4.5_

  - [x] 7.5 Create MainWindow.ScrollSync.cs partial class
    - Create `MermaidDiagramApp/MainWindow.ScrollSync.cs` with partial class declaration
    - Move InitializeSynchronizedScrolling, CodeEditor_PointerPressed, SyncPreviewToLine, SetupScrollSynchronization, and related fields (_codeParser, _currentElements)
    - Add necessary using statements and file header comment
    - _Requirements: 4.6_

  - [x] 7.6 Slim down MainWindow.xaml.cs core file
    - Remove all migrated methods from MainWindow.xaml.cs
    - Keep only: constructor, field/property declarations, MainWindow_Loaded (orchestration only), MainWindow_Closed, GetAppWindowForCurrentWindow, RestoreWindowStateAsync, WinRT_InterOp helper class
    - Add file header comment describing the core file's purpose
    - Verify no single partial class file exceeds 500 lines
    - Verify the application builds and all existing tests pass
    - _Requirements: 4.1, 4.7, 5.1, 5.2_

- [x] 8. Final checkpoint - Full verification
  - Build solution in Debug and Release configurations: `dotnet build MermaidDiagramApp/MermaidDiagramApp.sln -c Debug` and `dotnet build MermaidDiagramApp/MermaidDiagramApp.sln -c Release`
  - Run full test suite: `dotnet test MermaidDiagramApp.Tests/MermaidDiagramApp.Tests.csproj`
  - Verify no partial class file exceeds 500 lines
  - Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

## Notes

- All tasks are required (comprehensive testing from the start)
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each phase
- Property tests use FsCheck.Xunit 3.3 (already in test project)
- Unit tests use xUnit 2.9 with Moq 4.20 for mocking (already in test project)
- The refactoring is purely structural — no user-facing behavior changes
