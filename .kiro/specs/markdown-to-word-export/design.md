# Design Document: Markdown to Word Export

## Overview

This feature extends the MermaidDiagramApp to support exporting Markdown files to Microsoft Word (DOCX) format. The system will parse Markdown content, render embedded Mermaid diagrams as images, resolve local image paths, and generate a properly formatted Word document that preserves all content structure and formatting.

The implementation leverages existing infrastructure including the rendering pipeline, WebView2-based Mermaid rendering, and MVVM architecture. New components will be added following SOLID principles and the established design patterns in the application.

## Architecture

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  MainWindow.xaml - New Menu Items & Commands                │
│  - "Open Markdown File" (File Menu)                         │
│  - "Export to Word" (File Menu)                             │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                   Application Layer                          │
│  MarkdownToWordViewModel (New)                              │
│  - Manages export workflow state                            │
│  - Progress tracking                                         │
│  - Command bindings                                          │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                    Service Layer                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │     MarkdownToWordExportService (New)                │  │
│  │  - Orchestrates export workflow                      │  │
│  │  - Coordinates parsing, rendering, and generation    │  │
│  └──────────────┬───────────────────────────────────────┘  │
│                 │                                            │
│     ┌───────────┴───────────┬──────────────┐               │
│     │                       │              │               │
│  ┌──▼──────────┐    ┌──────▼────────┐  ┌─▼────────────┐  │
│  │ Markdown    │    │  Mermaid      │  │ Word         │  │
│  │ Parser      │    │  Image        │  │ Document     │  │
│  │ (Markdig)   │    │  Renderer     │  │ Generator    │  │
│  │             │    │  (Existing)   │  │ (OpenXML)    │  │
│  └─────────────┘    └───────────────┘  └──────────────┘  │
│                                                              │
│  Existing Services: RenderingOrchestrator, MermaidRenderer  │
└──────────────────────────────────────────────────────────────┘
```

### Integration with Existing Architecture

The new feature integrates seamlessly with the existing rendering pipeline:

1. **Reuses RenderingOrchestrator**: For content type detection
2. **Reuses MermaidRenderer**: For rendering Mermaid diagrams to images
3. **Follows MVVM Pattern**: New ViewModel for export workflow
4. **Extends Service Layer**: New services follow existing patterns
5. **Uses Existing Models**: ContentType, RenderingContext, etc.

## Components and Interfaces

### 3.1. MarkdownToWordExportService

**Location:** `Services/Export/MarkdownToWordExportService.cs`

Primary service that orchestrates the entire export workflow.

```csharp
public class MarkdownToWordExportService
{
    private readonly IMarkdownParser _markdownParser;
    private readonly IWordDocumentGenerator _wordGenerator;
    private readonly IMermaidImageRenderer _mermaidRenderer;
    private readonly ILogger _logger;
    
    public async Task<ExportResult> ExportToWordAsync(
        string markdownContent, 
        string markdownFilePath,
        string outputPath,
        IProgress<ExportProgress> progress,
        CancellationToken cancellationToken);
}
```

**Responsibilities:**
- Validate input Markdown content and file paths
- Parse Markdown using Markdig library
- Identify Mermaid code blocks in the parsed AST
- Render Mermaid diagrams to PNG images
- Resolve local image paths relative to Markdown file
- Generate Word document with all content
- Report progress and handle cancellation
- Clean up temporary files

**Workflow:**
1. Validate inputs (content, paths)
2. Parse Markdown to AST using Markdig
3. Walk AST to identify:
   - Mermaid code blocks (` ```mermaid `)
   - Image references
   - All other Markdown elements
4. For each Mermaid block:
   - Render to PNG using existing MermaidRenderer
   - Store temporary image file
5. For each image reference:
   - Resolve path (relative/absolute)
   - Validate file exists
6. Generate Word document:
   - Create document structure
   - Add content in order
   - Embed images
   - Apply formatting
7. Save document to output path
8. Clean up temporary files

### 3.2. IMarkdownParser Interface

**Location:** `Services/Export/IMarkdownParser.cs`

Abstraction for Markdown parsing to enable testability and potential parser swapping.

```csharp
public interface IMarkdownParser
{
    MarkdownDocument Parse(string markdownContent);
    IEnumerable<MermaidBlock> ExtractMermaidBlocks(MarkdownDocument document);
    IEnumerable<ImageReference> ExtractImageReferences(MarkdownDocument document);
}
```

**Implementation:** `MarkdigMarkdownParser`

Uses the Markdig library to parse Markdown and extract specific elements.

### 3.3. IWordDocumentGenerator Interface

**Location:** `Services/Export/IWordDocumentGenerator.cs`

Abstraction for Word document generation using Open XML SDK.

```csharp
public interface IWordDocumentGenerator
{
    void CreateDocument(string outputPath);
    void AddHeading(string text, int level);
    void AddParagraph(string text, ParagraphStyle style);
    void AddImage(string imagePath, ImageOptions options);
    void AddTable(TableData tableData);
    void AddList(ListData listData, bool ordered);
    void AddCodeBlock(string code, string language);
    void Save();
    void Dispose();
}
```

**Implementation:** `OpenXmlWordDocumentGenerator`

Uses DocumentFormat.OpenXml library to create and manipulate Word documents.

**Key Features:**
- Heading styles (H1-H6) mapped to Word heading styles
- Text formatting (bold, italic, code) preserved
- Lists (ordered/unordered) with proper nesting
- Tables with cell alignment
- Code blocks with monospace font and shading
- Image embedding with size constraints
- Hyperlinks (internal and external)

### 3.4. IMermaidImageRenderer Interface

**Location:** `Services/Export/IMermaidImageRenderer.cs`

Abstraction for rendering Mermaid diagrams to image files.

```csharp
public interface IMermaidImageRenderer
{
    Task<string> RenderToImageAsync(
        string mermaidCode, 
        string outputPath,
        ImageFormat format,
        CancellationToken cancellationToken);
}
```

**Implementation:** `WebView2MermaidImageRenderer`

Leverages the existing MermaidRenderer and WebView2 infrastructure to render diagrams.

**Process:**
1. Use existing MermaidRenderer to render diagram in WebView2
2. Execute JavaScript to get SVG content
3. Convert SVG to PNG using existing Svg.Skia infrastructure
4. Save PNG to temporary file
5. Return file path

### 3.5. ImagePathResolver

**Location:** `Services/Export/ImagePathResolver.cs`

Utility class for resolving image paths in Markdown documents.

```csharp
public class ImagePathResolver
{
    public string ResolveImagePath(string imagePath, string markdownFilePath);
    public bool IsValidImagePath(string resolvedPath);
    public ImageFormat DetectImageFormat(string imagePath);
}
```

**Resolution Logic:**
- HTTP/HTTPS URLs: Pass through unchanged
- Data URIs: Pass through unchanged
- Relative paths: Resolve relative to Markdown file directory
- Absolute Windows paths: Validate and use directly
- Validate file exists before returning

### 3.6. MarkdownToWordViewModel

**Location:** `ViewModels/MarkdownToWordViewModel.cs`

ViewModel for managing export workflow state and UI bindings.

```csharp
public class MarkdownToWordViewModel : INotifyPropertyChanged
{
    public string MarkdownFilePath { get; set; }
    public string OutputPath { get; set; }
    public bool IsExporting { get; set; }
    public int ProgressPercentage { get; set; }
    public string ProgressMessage { get; set; }
    public bool CanExport { get; }
    
    public ICommand OpenMarkdownFileCommand { get; }
    public ICommand ExportToWordCommand { get; }
    public ICommand CancelExportCommand { get; }
}
```

**Responsibilities:**
- Manage file paths
- Track export progress
- Enable/disable commands based on state
- Handle user interactions
- Report errors to UI

## Data Models

### ExportResult

```csharp
public class ExportResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; }
    public string ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public ExportStatistics Statistics { get; set; }
}
```

### ExportStatistics

```csharp
public class ExportStatistics
{
    public int TotalElements { get; set; }
    public int MermaidDiagramsRendered { get; set; }
    public int ImagesEmbedded { get; set; }
    public int TablesProcessed { get; set; }
    public long OutputFileSize { get; set; }
}
```

### ExportProgress

```csharp
public class ExportProgress
{
    public int PercentComplete { get; set; }
    public string CurrentOperation { get; set; }
    public ExportStage Stage { get; set; }
}

public enum ExportStage
{
    Parsing,
    RenderingDiagrams,
    ResolvingImages,
    GeneratingDocument,
    Complete
}
```

### MermaidBlock

```csharp
public class MermaidBlock
{
    public string Code { get; set; }
    public int LineNumber { get; set; }
    public string RenderedImagePath { get; set; }
}
```

### ImageReference

```csharp
public class ImageReference
{
    public string OriginalPath { get; set; }
    public string ResolvedPath { get; set; }
    public string AltText { get; set; }
    public int LineNumber { get; set; }
}
```

### ParagraphStyle

```csharp
public class ParagraphStyle
{
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsCode { get; set; }
    public string FontFamily { get; set; }
    public int FontSize { get; set; }
}
```

### ImageOptions

```csharp
public class ImageOptions
{
    public int MaxWidth { get; set; } = 600;
    public int MaxHeight { get; set; } = 800;
    public bool MaintainAspectRatio { get; set; } = true;
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


### Property Reflection

After analyzing all acceptance criteria, several properties can be consolidated to eliminate redundancy:

- Text formatting properties (bold, italic) can be combined into a single comprehensive property
- List conversion properties (ordered, unordered) can be combined as they test the same conversion mechanism
- Image path resolution properties (relative, absolute) can be combined as they test the same resolution logic
- Hyperlink properties can be combined into a comprehensive link conversion property

This consolidation ensures each property provides unique validation value without overlap.

### Correctness Properties

Property 1: Markdown file loading preserves content
*For any* valid Markdown file, loading the file into memory should result in content that exactly matches the file's text content
**Validates: Requirements 1.2**

Property 2: Mermaid block identification completeness
*For any* Markdown document containing Mermaid code blocks, parsing should identify all blocks marked with ` ```mermaid ` fence syntax
**Validates: Requirements 1.3**

Property 3: Command state reflects file loading
*For any* application state, the "Export to Word" command should be enabled if and only if a Markdown file is successfully loaded
**Validates: Requirements 1.5, 9.2**

Property 4: Export creates file at specified path
*For any* valid output path, successful export should result in a DOCX file existing at that exact path
**Validates: Requirements 2.2**

Property 5: Successful export shows notification
*For any* successful export operation, the application should display a success notification to the user
**Validates: Requirements 2.3**

Property 6: Export progress indicator visibility
*For any* export operation in progress, a progress indicator should be visible to the user
**Validates: Requirements 2.5**

Property 7: Heading level preservation
*For any* Markdown document with headings (H1-H6), the generated Word document should contain corresponding Word heading styles at the same levels
**Validates: Requirements 3.1**

Property 8: Text formatting preservation
*For any* Markdown text with inline formatting (bold, italic, code), the generated Word document should preserve all formatting with correct Word styles
**Validates: Requirements 3.2, 3.3, 3.8**

Property 9: List structure conversion
*For any* Markdown document with lists (ordered or unordered), the generated Word document should contain corresponding Word lists with the same structure
**Validates: Requirements 3.4, 3.5**

Property 10: Nested list hierarchy preservation
*For any* Markdown document with nested lists, the generated Word document should preserve the complete nesting hierarchy at all levels
**Validates: Requirements 3.6**

Property 11: Code block formatting
*For any* Markdown code block (non-Mermaid), the generated Word document should format it with monospace font and background shading
**Validates: Requirements 3.7**

Property 12: Blockquote styling
*For any* Markdown blockquote, the generated Word document should apply indentation and distinctive styling
**Validates: Requirements 3.9**

Property 13: Table structure preservation
*For any* Markdown table, the generated Word document should contain a Word table with the same number of rows, columns, and cell alignment
**Validates: Requirements 3.10**

Property 14: Mermaid diagram rendering
*For any* valid Mermaid code block in Markdown, the export process should render it using WebView2 and embed the result as an image
**Validates: Requirements 4.1**

Property 15: Diagram image format
*For any* rendered Mermaid diagram, the embedded image should be in PNG format with transparent background
**Validates: Requirements 4.2**

Property 16: Diagram scaling maintains aspect ratio
*For any* embedded diagram image, if scaling is applied to fit page margins, the aspect ratio should remain unchanged
**Validates: Requirements 4.4**

Property 17: Multiple diagram position preservation
*For any* Markdown document with multiple Mermaid diagrams, the generated Word document should contain all diagrams in the same sequential order
**Validates: Requirements 4.5**

Property 18: Image path resolution
*For any* image reference in Markdown (relative or absolute path), the system should resolve it correctly relative to the Markdown file location or use the absolute path directly
**Validates: Requirements 5.1, 5.2**

Property 19: Image format preservation
*For any* embedded image (PNG, JPG, GIF), the generated Word document should preserve the original image format
**Validates: Requirements 5.4**

Property 20: SVG to PNG conversion
*For any* SVG image reference in Markdown, the generated Word document should contain a PNG conversion of that image
**Validates: Requirements 5.5**

Property 21: Hyperlink conversion
*For any* hyperlink in Markdown (inline, reference-style, or internal), the generated Word document should contain a clickable hyperlink with the correct target
**Validates: Requirements 6.1, 6.2, 6.3**

Property 22: Cancellation stops processing
*For any* export operation, if cancellation is requested, the operation should stop and not complete the export
**Validates: Requirements 7.3**

Property 23: Cancellation cleanup
*For any* cancelled export operation, all temporary files created during the process should be deleted
**Validates: Requirements 7.4**

Property 24: Window title reflects loaded file
*For any* loaded Markdown file, the application window title should display the file name
**Validates: Requirements 9.3**

## Error Handling

### Error Categories

**File System Errors:**
- File not found
- Access denied
- Disk full
- Invalid path

**Parsing Errors:**
- Invalid UTF-8 encoding
- Malformed Markdown syntax (graceful degradation)

**Rendering Errors:**
- Mermaid syntax errors
- WebView2 rendering failures
- Image conversion failures

**Generation Errors:**
- Word document creation failures
- Image embedding failures
- OpenXML exceptions

### Error Handling Strategy

**Principle:** Fail gracefully with informative error messages. Never crash the application.

**Implementation:**
1. **Try-Catch Blocks:** Wrap all external operations (file I/O, rendering, generation)
2. **Validation:** Validate inputs before processing
3. **Logging:** Log all errors with context for debugging
4. **User Feedback:** Display user-friendly error messages with actionable guidance
5. **Partial Success:** When possible, complete export with placeholders for failed elements
6. **Cleanup:** Always clean up temporary files, even on error

**Error Message Format:**
```
Title: [Operation] Failed
Message: [User-friendly description]
Details: [Technical details for advanced users]
Suggestion: [What the user can do to resolve]
```

**Example Error Scenarios:**

1. **Missing Image File:**
   - Insert placeholder text: "[Image not found: {path}]"
   - Log warning with full path
   - Continue export

2. **Mermaid Syntax Error:**
   - Insert error message in document: "[Mermaid diagram error: {error}]"
   - Include original code in a code block
   - Continue export

3. **File Access Denied:**
   - Show error dialog
   - Suggest checking permissions or choosing different location
   - Abort export

4. **WebView2 Not Available:**
   - Show error dialog explaining WebView2 requirement
   - Provide link to download
   - Abort export

## Testing Strategy

### Unit Testing

**Framework:** MSTest or xUnit

**Test Coverage:**

1. **MarkdownParser Tests:**
   - Parse various Markdown elements
   - Extract Mermaid blocks correctly
   - Extract image references correctly
   - Handle malformed Markdown gracefully

2. **ImagePathResolver Tests:**
   - Resolve relative paths correctly
   - Handle absolute paths correctly
   - Detect image formats correctly
   - Validate paths correctly

3. **WordDocumentGenerator Tests:**
   - Create document structure
   - Add all Markdown elements with correct formatting
   - Embed images with correct sizing
   - Generate valid DOCX files

4. **MarkdownToWordExportService Tests:**
   - Orchestrate workflow correctly
   - Handle cancellation
   - Clean up temporary files
   - Report progress accurately

**Mocking Strategy:**
- Mock file system operations for deterministic tests
- Mock WebView2 rendering for isolated tests
- Mock Word document generation to test orchestration

### Property-Based Testing

**Framework:** FsCheck or CsCheck

**Property Tests:**

Property-based tests will generate random inputs to verify universal properties hold across all valid inputs. Each test will run a minimum of 100 iterations with randomly generated data.

**Test Generators:**
- Markdown content generator (headings, lists, tables, code blocks, etc.)
- Mermaid diagram generator (valid syntax)
- File path generator (relative, absolute, Windows paths)
- Image reference generator (various formats and paths)

**Key Properties to Test:**
- Content preservation (input Markdown elements appear in output)
- Structure preservation (order, nesting, hierarchy maintained)
- Format preservation (bold, italic, code formatting maintained)
- Path resolution (all path types resolve correctly)
- Error handling (invalid inputs don't crash, produce error messages)

### Integration Testing

**Scenarios:**

1. **End-to-End Export:**
   - Load real Markdown file
   - Export to Word
   - Verify Word document opens correctly
   - Verify all content present

2. **Mermaid Rendering Integration:**
   - Export Markdown with Mermaid diagrams
   - Verify diagrams rendered as images
   - Verify images embedded correctly

3. **Image Resolution Integration:**
   - Export Markdown with various image references
   - Verify all images resolved and embedded
   - Verify missing images handled gracefully

4. **Large File Handling:**
   - Export large Markdown files (>1MB)
   - Verify performance acceptable
   - Verify memory usage reasonable

### Manual Testing

**Test Cases:**

1. Export various Markdown files from real projects
2. Verify formatting in Microsoft Word
3. Test with different Word versions (2016, 2019, 365)
4. Test with LibreOffice Writer for compatibility
5. Verify images display correctly
6. Verify hyperlinks work correctly
7. Test cancellation during long exports
8. Test error scenarios (missing files, invalid paths)

## Dependencies

### NuGet Packages

**New Dependencies:**

1. **Markdig** (Latest stable version)
   - Purpose: Markdown parsing
   - License: BSD-2-Clause
   - Reason: Industry-standard, extensible, well-maintained

2. **DocumentFormat.OpenXml** (Latest stable version)
   - Purpose: Word document generation
   - License: MIT
   - Reason: Official Microsoft library, comprehensive API

**Existing Dependencies (Reused):**

1. **Microsoft.Web.WebView2** - For Mermaid rendering
2. **Svg.Skia** - For SVG to PNG conversion
3. **SkiaSharp** - Image processing

### System Requirements

- Windows 10 version 1809 or later (for WebView2)
- .NET 6.0 or later
- Microsoft Word 2016 or later (for viewing exported documents)

## Performance Considerations

### Optimization Strategies

1. **Async/Await:** All I/O operations are asynchronous
2. **Streaming:** Process Markdown in chunks for large files
3. **Caching:** Cache rendered Mermaid diagrams if same code appears multiple times
4. **Parallel Processing:** Render multiple Mermaid diagrams in parallel
5. **Memory Management:** Dispose resources promptly, use `using` statements
6. **Progress Reporting:** Update UI every 100ms to avoid excessive updates

### Performance Targets

- Small files (<100KB): Export in <2 seconds
- Medium files (100KB-1MB): Export in <10 seconds
- Large files (>1MB): Export in <30 seconds
- Mermaid rendering: <1 second per diagram
- Memory usage: <500MB for typical files

### Scalability

The system should handle:
- Files up to 10MB
- Up to 50 Mermaid diagrams per file
- Up to 100 images per file
- Tables with up to 100 rows

## Security Considerations

### Input Validation

1. **File Paths:** Validate all file paths to prevent directory traversal attacks
2. **Markdown Content:** Sanitize content to prevent injection attacks
3. **Image Paths:** Validate image paths before loading
4. **File Size:** Limit maximum file size to prevent DoS

### Temporary Files

1. **Secure Location:** Use system temp directory with proper permissions
2. **Unique Names:** Generate unique file names to prevent conflicts
3. **Cleanup:** Always delete temporary files, even on error
4. **Permissions:** Set restrictive permissions on temporary files

### External Resources

1. **Image Loading:** Only load images from local file system, not URLs (to prevent SSRF)
2. **Mermaid Rendering:** Sanitize Mermaid code before rendering
3. **Word Generation:** Use OpenXML library safely, avoid XML injection

## Future Enhancements

### Potential Features

1. **Batch Export:** Export multiple Markdown files at once
2. **Template Support:** Allow custom Word templates for styling
3. **PDF Export:** Add PDF export option
4. **Style Customization:** Allow users to customize Word styles
5. **Table of Contents:** Auto-generate TOC for documents with headings
6. **Footnotes:** Support Markdown footnotes
7. **Math Equations:** Support LaTeX math equations
8. **Syntax Highlighting:** Preserve code syntax highlighting in Word
9. **Export Presets:** Save export settings as presets
10. **Cloud Integration:** Export directly to OneDrive/SharePoint

### Architecture Extensibility

The design supports future extensions:

- **New Export Formats:** Implement `IDocumentGenerator` for PDF, HTML, etc.
- **New Parsers:** Implement `IMarkdownParser` for other markup languages
- **Custom Renderers:** Extend rendering pipeline for new diagram types
- **Plugins:** Plugin architecture for custom export processors

## Conclusion

This design provides a robust, maintainable solution for exporting Markdown files to Word documents. By leveraging existing infrastructure (rendering pipeline, WebView2, MVVM architecture) and following SOLID principles, the implementation will integrate seamlessly with the MermaidDiagramApp while maintaining code quality and extensibility.

The use of well-established libraries (Markdig, OpenXML) ensures reliability and reduces maintenance burden. Comprehensive error handling and testing strategies ensure a high-quality user experience. The architecture supports future enhancements without requiring significant refactoring.
