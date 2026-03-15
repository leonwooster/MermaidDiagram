# Project Structure

```
MermaidDiagramApp/                  # Main WinUI 3 application
├── App.xaml(.cs)                   # Application entry point, logging init
├── MainWindow.xaml(.cs)            # Primary window — split into partial classes:
│   ├── MainWindow.xaml.cs          # Core: WebView2 init, preview updates, timer
│   ├── MainWindow.FileOperations.cs    # Open/Save/New/Recent files
│   ├── MainWindow.ExportImage.cs       # PNG/SVG export
│   ├── MainWindow.SearchAndSync.cs     # Find, scroll sync
│   ├── MainWindow.Diagnostics.cs       # Mermaid.js version check
│   └── MainWindow.MarkdownToWord.cs    # Word export integration
├── Commands/
│   └── RelayCommand.cs             # ICommand implementation
├── Converters/                     # XAML value converters
├── Models/
│   ├── ContentType.cs              # Mermaid | Markdown | MarkdownWithMermaid
│   ├── FlowchartNode.cs / FlowchartEdge.cs  # Visual builder models
│   ├── Canvas/                     # Diagram builder canvas models
│   └── ...                         # Rendering, keyboard, syntax models
├── Services/
│   ├── Rendering/                  # Content rendering pipeline
│   │   ├── RenderingOrchestrator.cs    # Detects type → selects renderer → renders
│   │   ├── ContentTypeDetector.cs      # Content-first detection with caching
│   │   ├── ContentRendererFactory.cs   # Factory for Mermaid/Markdown renderers
│   │   ├── IContentRenderer.cs         # Renderer interface
│   │   ├── MermaidRenderer.cs          # Generates JS for Mermaid rendering
│   │   └── MarkdownRenderer.cs         # Generates JS for Markdown rendering
│   ├── Export/                     # Markdown-to-Word export pipeline
│   │   ├── MarkdownToWordExportService.cs  # Orchestrates full export
│   │   ├── MarkdigMarkdownParser.cs        # Parses MD to structured blocks
│   │   ├── OpenXmlWordDocumentGenerator.cs # Generates .docx via OpenXml
│   │   └── WebView2MermaidImageRenderer.cs # Renders Mermaid to images
│   ├── AI/                         # AI diagram generation (Ollama, OpenAI)
│   ├── Logging/                    # Rolling file logger
│   └── ...                         # Linter, syntax fixer, font manager, etc.
├── ViewModels/                     # MVVM ViewModels
│   ├── DiagramBuilderViewModel.cs  # Visual flowchart builder
│   ├── MarkdownToWordViewModel.cs  # Export-to-Word state
│   └── ...
├── Views/                          # XAML UserControls
│   ├── DiagramCanvas.xaml(.cs)     # Visual builder canvas
│   ├── AiDiagramGeneratorPanel.xaml(.cs)
│   └── ...
└── Assets/                         # Bundled web assets
    ├── UnifiedRenderer.html        # WebView2 host page
    ├── mermaid.min.js              # Bundled Mermaid.js
    ├── css/ & webfonts/            # FontAwesome icons

MermaidDiagramApp.Tests/            # xUnit test project
├── Models/                         # Model unit tests
├── Services/
│   ├── Export/                     # Export pipeline tests (unit + property-based)
│   └── ...                         # Service tests
└── ViewModels/                     # ViewModel tests (unit + property-based)
```

## Architecture Patterns
- **Partial classes**: `MainWindow` is split across 6 files by concern (core, file ops, export, search, diagnostics, word export)
- **Rendering pipeline**: `RenderingOrchestrator` → `ContentTypeDetector` → `ContentRendererFactory` → `IContentRenderer`. Content type is detected content-first (not extension-first)
- **MVVM**: ViewModels use `INotifyPropertyChanged` with `RelayCommand`. No DI container — services are instantiated directly
- **Export pipeline**: `MarkdownToWordExportService` coordinates `IMarkdownParser`, `IMermaidImageRenderer`, and `IWordDocumentGenerator` via interfaces
- **Logging**: Singleton `LoggingService` with rolling file sink, accessed via `LoggingService.Instance.GetLogger<T>()`
- **WebView2 communication**: JSON messages via `postMessage` / `WebMessageReceived` for bidirectional C#↔JS communication
