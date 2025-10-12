# Software Design Document

This document details the software architecture, components, and design decisions for the Mermaid Diagram App.

## 1. Introduction

The Mermaid Diagram App is a desktop application for Windows built using WinUI 3. It provides a simple, efficient environment for creating, viewing, and managing Mermaid diagrams and Markdown documentation. The core functionality includes a text editor for writing Mermaid syntax or Markdown content and a live preview panel that renders the content in real-time.

## 2. Architecture

The application follows a layered architecture with clear separation of concerns, adhering to SOLID principles and leveraging design patterns for extensibility and maintainability.

### 2.1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  (MainWindow.xaml, Dialogs, UI Controls)                    │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                   Application Layer                          │
│  (ViewModels, Commands, UI Logic)                           │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                    Service Layer                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         Rendering Pipeline (Strategy Pattern)         │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │        RenderingOrchestrator                    │  │  │
│  │  │  - Coordinates rendering workflow               │  │  │
│  │  │  - Manages content type detection               │  │  │
│  │  │  - Delegates to appropriate renderer            │  │  │
│  │  └──────────────┬─────────────────────────────────┘  │  │
│  │                 │                                      │  │
│  │     ┌───────────┴───────────┐                         │  │
│  │     │                       │                         │  │
│  │  ┌──▼──────────┐    ┌──────▼────────┐               │  │
│  │  │ Mermaid     │    │  Markdown     │               │  │
│  │  │ Renderer    │    │  Renderer     │               │  │
│  │  │ (IContent   │    │  (IContent    │               │  │
│  │  │  Renderer)  │    │   Renderer)   │               │  │
│  │  └─────────────┘    └───────────────┘               │  │
│  │                                                        │  │
│  │  ContentTypeDetector | ContentRendererFactory        │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  Other Services: FontManager, SyntaxAnalyzer, Logger, etc. │
└──────────────────────────┬───────────────────────────────────┘
                           │
┌──────────────────────────┴───────────────────────────────────┐
│                   Infrastructure Layer                        │
│  (WebView2, File System, External Libraries)                 │
└──────────────────────────────────────────────────────────────┘
```

### 2.2. SOLID Principles Applied

- **Single Responsibility Principle (S)**: Each renderer class has one reason to change - its specific rendering algorithm. `ContentTypeDetector` only handles detection logic. `RenderingOrchestrator` only coordinates the pipeline.

- **Open/Closed Principle (O)**: New content types (e.g., AsciiDoc, reStructuredText) can be added by implementing `IContentRenderer` without modifying existing code. The orchestrator and factory remain unchanged.

- **Liskov Substitution Principle (L)**: Any `IContentRenderer` implementation can be used interchangeably. The orchestrator treats all renderers uniformly through the interface contract.

- **Interface Segregation Principle (I)**: Focused interfaces (`IContentRenderer`, `IContentTypeDetector`, `IRenderingContext`) ensure clients only depend on methods they use.

- **Dependency Inversion Principle (D)**: High-level modules (`MainWindow`, `RenderingOrchestrator`) depend on abstractions (`IContentRenderer`) rather than concrete implementations.

### 2.3. Design Patterns

#### Strategy Pattern
The rendering system uses the Strategy pattern where `IContentRenderer` defines the strategy interface, and `MermaidRenderer` and `MarkdownRenderer` are concrete strategies. The `RenderingOrchestrator` acts as the context, selecting the appropriate strategy at runtime.

#### Factory Pattern
`ContentRendererFactory` encapsulates the creation logic for renderer instances, providing a centralized point for instantiation and configuration.

#### Template Method Pattern
The abstract `ContentRenderer` base class defines the rendering workflow:
1. Validate input
2. Preprocess content
3. Render content
4. Postprocess output

Concrete renderers override specific steps while the overall algorithm remains consistent.

#### Observer Pattern
Rendering mode changes trigger events that notify UI components (status indicators, menu items) to update their state accordingly.

## 3. Components

### 3.1. Rendering System

#### 3.1.1. IContentRenderer Interface
```csharp
public interface IContentRenderer
{
    ContentType SupportedType { get; }
    bool CanRender(ContentType type);
    Task<RenderingResult> RenderAsync(string content, IRenderingContext context);
    IReadOnlyList<string> GetSupportedFeatures();
}
```

Defines the contract for all content renderers. Implementations must handle their specific content type and return consistent results.

#### 3.1.2. MermaidRenderer
Encapsulates Mermaid diagram rendering logic. Communicates with WebView2 to execute Mermaid.js rendering, handles syntax errors, and provides diagram-specific features (pan, zoom, export).

**Responsibilities:**
- Validate Mermaid syntax
- Escape content for JavaScript interop
- Execute `renderDiagram()` in WebView2
- Handle Mermaid.js errors and provide user feedback
- Support diagram-specific operations

#### 3.1.3. MarkdownRenderer
Handles Markdown document rendering using markdown-it.js. Supports GitHub Flavored Markdown and can render embedded Mermaid diagrams within code blocks.

**Responsibilities:**
- Convert Markdown to HTML using markdown-it.js
- Apply styling and theming
- Detect and render Mermaid code blocks (` ```mermaid `)
- Handle syntax highlighting for code blocks
- Support GFM features (tables, task lists, etc.)

#### 3.1.4. ContentTypeDetector
Analyzes file content and extension to determine the appropriate content type.

**Detection Algorithm:**
1. Check file extension (`.mmd` → Mermaid, `.md` → analyze further)
2. For `.md` files, scan first 10 lines for Mermaid keywords
3. If Mermaid diagram detected at start, classify as Mermaid
4. Otherwise, classify as Markdown
5. Cache result to avoid repeated analysis

#### 3.1.5. RenderingOrchestrator
Coordinates the entire rendering pipeline, acting as the facade for the rendering system.

**Workflow:**
1. Receive content from `MainWindow`
2. Detect content type using `ContentTypeDetector`
3. Obtain appropriate renderer from `ContentRendererFactory`
4. Create `RenderingContext` with metadata
5. Delegate rendering to selected renderer
6. Handle errors and provide fallback
7. Notify UI of rendering state changes

#### 3.1.6. ContentRendererFactory
Creates and configures renderer instances based on content type.

```csharp
public class ContentRendererFactory
{
    public IContentRenderer CreateRenderer(ContentType type)
    {
        return type switch
        {
            ContentType.Mermaid => new MermaidRenderer(_webView, _logger),
            ContentType.Markdown => new MarkdownRenderer(_webView, _logger),
            _ => throw new NotSupportedException($"Content type {type} not supported")
        };
    }
}
```

### 3.2. MainWindow.xaml / MainWindow.xaml.cs

This is the primary component of the application.

- **`MainWindow.xaml`**: Defines the user interface structure, including:
  - `MenuBar`: For application commands (File, New, View, etc.)
  - `TextControlBox` (`CodeEditor`): A third-party control for text editing with syntax highlighting and line numbers
  - `WebView2` (`PreviewBrowser`): Hosts the unified rendering engine
  - `GridSplitter`: Allows the user to resize the editor and preview panes
  - Status indicators for rendering mode

- **`MainWindow.xaml.cs`**: Contains the application logic:
  - **Initialization**: Sets up the `WebView2` control, loads `UnifiedRenderer.html`, initializes `RenderingOrchestrator`, and starts a `DispatcherTimer` to trigger live updates
  - **Event Handlers**: Manages clicks for menu items like `Open`, `Save`, `Export`, and creating new diagrams from templates
  - **Live Preview Logic**: The `Timer_Tick` event checks for changes in the `CodeEditor` and calls the `UpdatePreview` method
  - **`UpdatePreview`**: Delegates to `RenderingOrchestrator.RenderAsync()` which handles content detection and rendering

**Simplified UpdatePreview:**
```csharp
private async Task UpdatePreview()
{
    try
    {
        var code = CodeEditor.Text;
        if (code == _lastPreviewedCode) return;
        
        var context = new RenderingContext
        {
            FileExtension = _currentFileExtension,
            Theme = GetCurrentTheme(),
            EnableMermaidInMarkdown = true
        };
        
        var result = await _renderingOrchestrator.RenderAsync(code, context);
        
        if (result.Success)
        {
            _lastPreviewedCode = code;
            UpdateRenderingModeIndicator(result.ContentType);
        }
        else
        {
            DisplayRenderingError(result.Error);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error in UpdatePreview: {ex.Message}", ex);
    }
}
```

### 3.3. Assets/UnifiedRenderer.html

This is a self-contained HTML file that acts as a bridge between the C# application and both Mermaid.js and markdown-it.js libraries.

- **Structure**: Contains a single `div` (`preview`) to hold the rendered content
- **Scripts**:
  - Includes `mermaid.min.js` for diagram rendering
  - Includes `markdown-it.min.js` for Markdown parsing
  - Includes `highlight.min.js` for code syntax highlighting
  - `renderContent(code, mode)`: A unified JavaScript function called from C# that routes to the appropriate renderer based on mode ('mermaid' or 'markdown')
  - `renderMermaid(content, container)`: Renders Mermaid diagrams
  - `renderMarkdown(content, container)`: Converts Markdown to HTML and renders embedded Mermaid diagrams
  - Error handling to display parsing errors from either library

**JavaScript API:**
```javascript
window.renderContent = async function(content, mode) {
    const preview = document.getElementById('preview');
    
    try {
        if (mode === 'mermaid') {
            await renderMermaid(content, preview);
        } else if (mode === 'markdown') {
            await renderMarkdown(content, preview);
        }
        
        window.chrome.webview.postMessage({
            type: 'renderComplete',
            mode: mode
        });
    } catch (error) {
        displayError(error, preview);
        window.chrome.webview.postMessage({
            type: 'renderError',
            error: error.message
        });
    }
};
```

### 3.4. File Picker Interoperability

Because the application is configured as **unpackaged**, it cannot use the standard `FileOpenPicker` and `FileSavePicker` APIs directly. These APIs require a window handle to be associated with them.

- **Solution**: A static helper class (`WinRT_InterOp`) and a COM interface (`IInitializeWithWindow`) are defined in `MainWindow.xaml.cs`.
- **`WinRT_InterOp.InitializeWithWindow()`**: This method retrieves the main window's handle (`HWND`) using `WinRT.Interop.WindowNative.GetWindowHandle()` and passes it to the picker instance by casting the picker to the `IInitializeWithWindow` interface. This is done for every `Open`, `Save`, and `Export` operation.

## 4. Data Flow: Content Rendering

### 4.1. Mermaid Diagram Rendering Flow

1. The user types Mermaid syntax into the `CodeEditor`
2. The `DispatcherTimer` fires every 500ms
3. The `Timer_Tick` handler detects a change in the text
4. `UpdatePreview()` is called, which passes content to `RenderingOrchestrator`
5. `ContentTypeDetector` identifies content as Mermaid diagram
6. `ContentRendererFactory` creates a `MermaidRenderer` instance
7. `MermaidRenderer.RenderAsync()` serializes the code and calls JavaScript interop
8. `ExecuteScriptAsync` invokes `renderContent(code, 'mermaid')` in WebView2
9. `renderMermaid()` in JavaScript calls `mermaid.render()`
10. Mermaid.js parses the code and generates an SVG string
11. The SVG is injected into the `preview` div, making it visible to the user
12. Success message sent back to C# via WebView2 messaging

### 4.2. Markdown Document Rendering Flow

1. The user opens a `.md` file or types Markdown content
2. The `DispatcherTimer` fires every 500ms
3. The `Timer_Tick` handler detects a change in the text
4. `UpdatePreview()` is called, which passes content to `RenderingOrchestrator`
5. `ContentTypeDetector` identifies content as Markdown document
6. `ContentRendererFactory` creates a `MarkdownRenderer` instance
7. `MarkdownRenderer.RenderAsync()` serializes the code and calls JavaScript interop
8. `ExecuteScriptAsync` invokes `renderContent(code, 'markdown')` in WebView2
9. `renderMarkdown()` in JavaScript:
   - Calls `md.render()` to convert Markdown to HTML
   - Scans for ` ```mermaid ` code blocks
   - Replaces code blocks with Mermaid diagram divs
   - Calls `mermaid.run()` to render embedded diagrams
10. The HTML with rendered diagrams is injected into the `preview` div
11. Success message sent back to C# via WebView2 messaging

### 4.3. Content Type Detection Flow

```
File Opened/Content Changed
         ↓
ContentTypeDetector.DetectContentType()
         ↓
Check file extension
         ↓
    .mmd? ──Yes──→ Return ContentType.Mermaid
         ↓
        No
         ↓
    .md? ──No──→ Return ContentType.Mermaid (default)
         ↓
       Yes
         ↓
Scan first 10 lines for keywords
         ↓
Mermaid keywords found? ──Yes──→ Return ContentType.Mermaid
         ↓
        No
         ↓
Return ContentType.Markdown
```

## 5. Dependencies

- **`Microsoft.WindowsAppSDK`**: The core SDK for building WinUI 3 applications.
- **`TextControlBox.WinUI.JuliusKirsch`**: A third-party code editor control.
- **`CommunityToolkit.WinUI.UI.Controls`**: Provides the `GridSplitter` control.
- **`Svg.Skia`**: Used for converting the generated SVG to a PNG file during the export process.

## 6. Conclusion

The Mermaid Diagram App's new architecture provides a robust, scalable, and maintainable foundation for rendering Mermaid diagrams and Markdown documentation. By applying SOLID principles and design patterns, the application ensures a clear separation of concerns, extensibility, and flexibility. The unified rendering engine, powered by WebView2 and JavaScript interop, enables seamless rendering of both Mermaid diagrams and Markdown content.

## 7. Future Work

- **Improve Performance**: Optimize rendering performance by leveraging caching, parallel processing, and lazy loading.
- **Enhance Markdown Support**: Add support for additional Markdown features, such as tables, task lists, and syntax highlighting.
- **Integrate with Other Tools**: Integrate the Mermaid Diagram App with popular development tools, such as Visual Studio Code and GitHub.
- **Provide Customization Options**: Offer users customization options for the rendering engine, such as theme selection and font sizes.
