# Project Structure

## Solution Organization

```
MermaidDiagramApp/              # Main application project
MermaidDiagramApp.Tests/        # Test project
docs/                           # Documentation
.kiro/                          # Kiro configuration and specs
```

## Main Application Structure

### Core Application Files

- **App.xaml / App.xaml.cs** - Application entry point, logging initialization
- **MainWindow.xaml / MainWindow.xaml.cs** - Primary UI and application logic
- **MainWindow.MarkdownToWord.cs** - Partial class for Word export functionality
- **Package.appxmanifest** - MSIX packaging configuration

### Services Layer (`Services/`)

Organized by functional domain:

#### Rendering (`Services/Rendering/`)
- **RenderingOrchestrator.cs** - Coordinates rendering pipeline (Strategy pattern)
- **ContentTypeDetector.cs** - Detects Mermaid vs Markdown content
- **ContentRendererFactory.cs** - Creates renderer instances (Factory pattern)
- **MermaidRenderer.cs** - Mermaid diagram rendering
- **MarkdownRenderer.cs** - Markdown document rendering
- **IContentRenderer.cs** - Renderer interface

#### Export (`Services/Export/`)
- **MarkdownToWordExportService.cs** - Main export orchestrator
- **MarkdigMarkdownParser.cs** - Parses Markdown to structured data
- **OpenXmlWordDocumentGenerator.cs** - Generates Word documents
- **WebView2MermaidImageRenderer.cs** - Renders Mermaid diagrams to images
- **ImagePathResolver.cs** - Resolves relative/absolute image paths
- Supporting models: ExportResult, ExportProgress, ImageReference, etc.

#### AI (`Services/AI/`)
- **AiServiceFactory.cs** - Creates AI service instances
- **OpenAiService.cs** - OpenAI integration
- **OllamaAiService.cs** - Ollama integration
- **DiagramTypeClassifier.cs** - Classifies diagram types
- **AiConfigStorageService.cs** - Persists AI configuration

#### Logging (`Services/Logging/`)
- **LoggingService.cs** - Singleton logging service
- **Logger.cs** - Logger implementation
- **RollingFileLogSink.cs** - File-based log sink with rotation
- **LogEntry.cs**, **LogLevel.cs** - Supporting types

#### Other Services
- **MermaidSyntaxAnalyzer.cs** - Analyzes Mermaid syntax
- **MermaidSyntaxFixer.cs** - Auto-fixes common syntax errors
- **MermaidTextOptimizer.cs** - Optimizes text in diagrams
- **KeyboardShortcutManager.cs** - Manages keyboard shortcuts
- **RecentFilesService.cs** - Tracks recently opened files
- **DiagramFileService.cs** - File I/O operations
- **MarkdownStyleSettingsService.cs** - Persists Markdown style preferences

### ViewModels (`ViewModels/`)

MVVM pattern implementation:
- **DiagramBuilderViewModel.cs** - Visual diagram builder logic
- **DiagramCanvasViewModel.cs** - Canvas interaction logic
- **AiDiagramGeneratorViewModel.cs** - AI generation UI logic
- **MarkdownToWordViewModel.cs** - Export UI logic
- **PropertiesPanelViewModel.cs** - Properties panel logic
- **SyntaxIssuesViewModel.cs** - Syntax error display

### Views (`Views/`)

XAML user controls:
- **DiagramCanvas.xaml** - Visual diagram builder canvas
- **ShapeToolbox.xaml** - Shape palette for builder
- **PropertiesPanel.xaml** - Element properties editor
- **AiDiagramGeneratorPanel.xaml** - AI generation interface
- **FloatingAiPrompt.xaml** - Floating AI prompt dialog
- **AiSettingsDialog.xaml** - AI configuration dialog
- **MarkdownStyleSettingsDialog.xaml** - Markdown style editor

### Models (`Models/`)

Data structures organized by domain:

#### Canvas (`Models/Canvas/`)
- **DiagramBuilderFile.cs** - Diagram file format
- **CanvasNode.cs** - Visual node representation
- **CanvasConnector.cs** - Connection between nodes
- **ShapeTemplate.cs** - Shape definitions
- **DiagramType.cs** - Diagram type enumeration

#### Core Models
- **ContentType.cs** - Content type enumeration
- **RenderingContext.cs** - Rendering metadata
- **RenderingResult.cs** - Rendering output
- **MarkdownStyleSettings.cs** - Style configuration
- **SyntaxIssue.cs** - Syntax error representation
- **KeyboardEventMessage.cs** - Keyboard event data

### Assets (`Assets/`)

Static resources:
- **UnifiedRenderer.html** - WebView2 rendering engine (Mermaid + Markdown)
- **MermaidHost.html** - Legacy Mermaid-only renderer
- **mermaid.min.js** - Mermaid.js library
- **mermaid-version.txt** - Version tracking
- **css/** - Font Awesome CSS
- **webfonts/** - Font Awesome fonts
- **Images/** - Application icons and assets

### Commands (`Commands/`)

- **RelayCommand.cs** - ICommand implementation for MVVM

### Converters (`Converters/`)

XAML value converters:
- **BoolToVisibilityConverter.cs**
- **InverseBooleanConverter.cs**
- **EnumToStringConverter.cs**
- **ZoomLevelConverter.cs**
- etc.

## Test Project Structure (`MermaidDiagramApp.Tests/`)

Mirrors main project structure:

```
Services/
  Export/                       # Export functionality tests
    MarkdownToWordExportServiceTests.cs
    ImagePathResolverTests.cs
    *PropertyTests.cs           # Property-based tests
    EndToEndIntegrationTests.cs
  KeyboardShortcutManagerTests.cs
  MermaidTextOptimizerTests.cs
ViewModels/
  MarkdownToWordViewModelTests.cs
  *PropertyTests.cs
Models/
  KeyboardEventMessageTests.cs
```

### Test Naming Conventions

- **Unit tests**: `[ClassName]Tests.cs`
- **Property-based tests**: `[ClassName]PropertyTests.cs`
- **Integration tests**: `[Feature]IntegrationTests.cs`

## Documentation (`docs/`)

- **SOFTWARE_DESIGN.md** - Architecture and design patterns
- **USER_GUIDE.md** - End-user documentation
- **features/** - Feature-specific documentation
- **design/** - Design documents
- **finding/** - Investigation notes

## Configuration Files

- **.kiro/specs/** - Feature specifications
- **.kiro/steering/** - AI assistant guidance (this file)
- **.kiro/hooks/** - Automation hooks
- **.gitignore** - Git exclusions
- **Package.appxmanifest** - MSIX manifest

## Key Architectural Patterns

### Layered Architecture
```
Presentation (XAML/ViewModels)
    ↓
Application (MainWindow logic)
    ↓
Service (Business logic)
    ↓
Infrastructure (WebView2, File I/O)
```

### SOLID Principles
- Services use interface-based design (IContentRenderer, ILogger, etc.)
- Factory pattern for object creation
- Strategy pattern for rendering
- Single responsibility per service class

### File Naming
- Interfaces: `I[Name].cs`
- Implementations: `[Name].cs`
- ViewModels: `[Feature]ViewModel.cs`
- Views: `[Feature].xaml` + `[Feature].xaml.cs`
- Tests: `[ClassName]Tests.cs`
