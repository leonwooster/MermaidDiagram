# Requirements Document

## Introduction

This feature comprehensively refactors the MermaidDiagramApp to improve maintainability, testability, and adherence to SOLID principles. The current MainWindow.xaml.cs is a ~2900-line God Object that handles UI lifecycle, file I/O, rendering, export, search, keyboard shortcuts, window state, AI features, visual builder, zoom, fullscreen, updates, and more. Services are manually instantiated with no dependency injection container, and business logic is tightly coupled to UI event handlers. This refactoring introduces a DI container, extracts a MainWindowViewModel following MVVM, creates focused services for remaining business logic, and completes partial class splitting of the code-behind — all without changing any user-facing behavior.

## Glossary

- **MainWindow**: The primary window class (~2900 lines) that currently acts as a God Object handling all application concerns
- **God_Object**: An anti-pattern where a single class has too many responsibilities
- **DI_Container**: A Dependency Injection container (Microsoft.Extensions.DependencyInjection) that manages object creation and lifetime
- **MainWindowViewModel**: A new ViewModel class that holds UI state and commands, extracted from MainWindow code-behind
- **Code_Behind**: The C# file (MainWindow.xaml.cs) associated with a XAML view
- **MVVM**: Model-View-ViewModel architectural pattern separating UI from business logic
- **Service_Registration**: The process of configuring the DI container with interface-to-implementation mappings in App.xaml.cs
- **Partial_Class**: A C# feature allowing a single class definition to be split across multiple source files

## Requirements

### Requirement 1: Introduce Dependency Injection Container

**User Story:** As a developer, I want all services registered in a central DI container, so that dependencies are explicit, testable, and not manually wired in the MainWindow constructor.

#### Acceptance Criteria

1. WHEN the application starts, THE App SHALL configure a DI container (Microsoft.Extensions.DependencyInjection) with all service registrations in App.xaml.cs
2. WHEN the DI container is configured, THE App SHALL register all existing services (RenderingOrchestrator, ContentTypeDetector, ContentRendererFactory, DiagramFileService, RecentFilesService, MarkdownStyleSettingsService, MermaidSyntaxAnalyzer, MermaidSyntaxFixer, MermaidTextOptimizer, KeyboardShortcutManager, FontManager, WindowStateManager, ShortcutPreferencesService, AiConfigStorageService, AiServiceFactory, LoggingService) with appropriate lifetimes
3. WHEN the DI container is configured, THE App SHALL register the MainWindowViewModel as a transient service
4. WHEN MainWindow is created, THE App SHALL resolve MainWindow dependencies from the DI container rather than manually instantiating services in the constructor
5. IF a required service is not registered in the DI container, THEN THE App SHALL fail fast at startup with a descriptive error message

### Requirement 2: Extract MainWindowViewModel

**User Story:** As a developer, I want UI state and commands extracted from MainWindow code-behind into a MainWindowViewModel, so that business logic is separated from the view and can be unit tested independently.

#### Acceptance Criteria

1. WHEN the refactoring is complete, THE MainWindowViewModel SHALL hold all UI state fields currently in MainWindow (including _currentFilePath, _currentContentType, _isFullScreen, _isPresentationMode, _isPanModeEnabled, _isBuilderVisible, _currentSearchText, _lastPreviewedCode)
2. WHEN the refactoring is complete, THE MainWindowViewModel SHALL expose commands (using ICommand/RelayCommand) for user actions including: NewDiagram, OpenFile, SaveFile, CloseFile, ExportSvg, ExportPng, ToggleFullScreen, TogglePresentationMode, ToggleBuilder, Find, CheckSyntax, Exit
3. WHEN the refactoring is complete, THE MainWindowViewModel SHALL implement INotifyPropertyChanged for all bindable properties
4. WHEN a UI action is triggered, THE MainWindow code-behind SHALL delegate to the MainWindowViewModel command rather than containing business logic inline
5. WHEN the MainWindowViewModel state changes, THE MainWindow view SHALL update through data binding rather than direct UI manipulation where feasible

### Requirement 3: Extract Remaining Business Logic into Services

**User Story:** As a developer, I want business logic that is currently embedded in MainWindow event handlers extracted into dedicated service classes, so that each service has a single responsibility and can be tested in isolation.

#### Acceptance Criteria

1. WHEN the refactoring is complete, THE application SHALL have a FileOperationsService that encapsulates file open, save, close, recent files management, and file type detection logic
2. WHEN the refactoring is complete, THE application SHALL have a SearchService that encapsulates find-next, find-previous, and search state management logic
3. WHEN the refactoring is complete, THE application SHALL have a MermaidUpdateService that encapsulates version checking, downloading, and installing Mermaid.js updates
4. WHEN the refactoring is complete, THE application SHALL have an ExportService that encapsulates SVG and PNG export logic currently in MainWindow
5. WHEN a new service is created, THE service SHALL define an interface (e.g., IFileOperationsService, ISearchService) and be registered in the DI container
6. WHEN the refactoring is complete, THE MainWindow code-behind SHALL contain only view-specific wiring (XAML event routing, WebView2 interop, dialog presentation) and no business logic

### Requirement 4: Complete Partial Class Splitting of Code-Behind

**User Story:** As a developer, I want the remaining MainWindow code-behind organized into partial class files by concern, so that I can quickly locate and modify view-specific code.

#### Acceptance Criteria

1. WHEN the refactoring is complete, THE MainWindow.xaml.cs core file SHALL contain only the constructor, field declarations, and initialization orchestration
2. WHEN the refactoring is complete, THE MainWindow.WebView.cs SHALL contain all WebView2 initialization, message handling, and JavaScript interop code
3. WHEN the refactoring is complete, THE MainWindow.UI.cs SHALL contain UI event routing, dialog presentation, fullscreen/presentation mode toggling, and keyboard shortcut wiring
4. WHEN the refactoring is complete, THE MainWindow.Builder.cs SHALL contain visual builder panel visibility management and canvas wiring code
5. WHEN the refactoring is complete, THE MainWindow.Search.cs SHALL contain search panel UI wiring and CodeEditor search integration
6. WHEN the refactoring is complete, THE MainWindow.ScrollSync.cs SHALL contain synchronized scrolling initialization and scroll-to-line logic
7. WHEN examining any single partial class file, THE file SHALL contain no more than 500 lines of code

### Requirement 5: Preserve All Existing Behavior

**User Story:** As a user, I want all existing features to work identically after the refactoring, so that the refactoring does not introduce regressions.

#### Acceptance Criteria

1. WHEN the refactored application is built, THE build SHALL succeed without errors on all target platforms (x86, x64, ARM64)
2. WHEN existing tests are run after refactoring, THE test suite SHALL pass with zero failures
3. WHEN the application is launched after refactoring, THE application SHALL start, display the editor and preview pane, and render diagrams identically to the pre-refactoring version
4. WHEN any feature is used (file operations, rendering, export, AI, builder, search, keyboard shortcuts, fullscreen, presentation mode, Mermaid updates), THE feature SHALL behave identically to the pre-refactoring version

### Requirement 6: Improve Testability of Extracted Components

**User Story:** As a developer, I want the newly extracted ViewModel and services to be unit-testable with mocked dependencies, so that I can verify correctness without requiring a running UI.

#### Acceptance Criteria

1. WHEN the MainWindowViewModel is instantiated in a test, THE ViewModel SHALL accept all dependencies through constructor injection
2. WHEN a new service (FileOperationsService, SearchService, MermaidUpdateService, ExportService) is created, THE service SHALL accept dependencies through constructor injection using interfaces
3. WHEN the refactoring is complete, THE test project SHALL contain unit tests for the MainWindowViewModel verifying command execution and state transitions
4. WHEN the refactoring is complete, THE test project SHALL contain unit tests for each newly created service
