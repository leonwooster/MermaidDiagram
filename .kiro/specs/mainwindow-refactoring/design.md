# Design Document: MainWindow Refactoring

## Overview

This design document outlines the refactoring of the MainWindow.xaml.cs file into multiple partial class files organized by functionality. The current monolithic file contains approximately 2962 lines of code, making it difficult to maintain and navigate. By splitting the code into logical, cohesive modules, we will improve code organization, maintainability, and developer productivity.

The refactoring will use C#'s partial class feature to split the MainWindow class across multiple files while maintaining all existing functionality. Each partial class file will focus on a specific area of functionality, following the Single Responsibility Principle.

## Architecture

### Current Structure

```
MainWindow.xaml.cs (2962 lines)
├── Constructor & Initialization
├── WebView2 Setup & Rendering
├── File Operations (Open/Save/Close)
├── UI Event Handlers (Menu items, dialogs)
├── Keyboard Shortcuts
├── AI Features
├── Visual Builder
├── Mermaid Updates
├── Zoom Controls
├── Full Screen Mode
└── Various Helper Methods
```

### Proposed Structure

```
MainWindow/
├── MainWindow.xaml.cs (Core - ~200 lines)
│   ├── Constructor
│   ├── Core fields and properties
│   └── Basic initialization
│
├── MainWindow.WebView.cs (~500 lines)
│   ├── InitializeWebViewAsync
│   ├── WebView message handling
│   ├── Preview rendering
│   ├── Content type detection
│   └── Zoom controls
│
├── MainWindow.FileOperations.cs (~300 lines)
│   ├── Open/Save/Close operations
│   ├── File watching
│   ├── Window state management
│   └── File path handling
│
├── MainWindow.UI.cs (~600 lines)
│   ├── Menu item click handlers
│   ├── Dialog management
│   ├── Full screen/presentation mode
│   ├── Keyboard shortcuts
│   └── Status bar updates
│
├── MainWindow.AI.cs (~400 lines)
│   ├── AI service initialization
│   ├── AI prompt handling
│   ├── Diagram generation
│   ├── AI settings dialog
│   └── Pop-out window management
│
├── MainWindow.Builder.cs (~400 lines)
│   ├── Builder initialization
│   ├── Canvas operations
│   ├── Shape toolbox integration
│   ├── Properties panel
│   └── Code import/export
│
├── MainWindow.MarkdownToWord.cs (~300 lines) [Already exists]
│   ├── Export initialization
│   ├── File picker dialogs
│   ├── Progress dialog
│   └── Export workflow
│
└── MainWindow.Updates.cs (~200 lines)
    ├── Mermaid.js update checking
    ├── Version comparison
    ├── Download and installation
    └── Update notifications
```

## Components and Interfaces

### 1. MainWindow.xaml.cs (Core)

**Purpose:** Contains the core structure, constructor, and primary initialization.

**Contents:**
- Class declaration and inheritance
- All private fields (logger, services, state variables)
- Public properties (BuilderViewModel)
- Constructor with basic initialization
- MainWindow_Loaded event handler (orchestrates initialization)
- MainWindow_Closed event handler

**Responsibilities:**
- Initialize all services and components
- Set up event handlers
- Coordinate initialization of partial class modules

### 2. MainWindow.WebView.cs

**Purpose:** Manages all WebView2-related functionality.

**Contents:**
- `InitializeWebViewAsync()` - WebView2 setup and configuration
- `PreviewBrowser_NavigationCompleted()` - Navigation event handling
- `UpdatePreview()` - Content rendering orchestration
- `ExecuteRenderingScript()` - JavaScript execution for rendering
- `OnRenderingStateChanged()` - Rendering state event handler
- `UpdateRenderModeIndicator()` - Status bar updates
- `SetupCtrlWheelZoom()` - Zoom gesture setup
- `ApplyPreviewZoom()` - Zoom level application
- Zoom control event handlers (ZoomIn, ZoomOut, ZoomReset)
- Drag mode toggle handlers

**Key Methods:**
```csharp
private async Task InitializeWebViewAsync();
private async Task UpdatePreview();
private async Task ExecuteRenderingScript(string content, ContentType contentType, RenderingContext context);
private void OnRenderingStateChanged(object? sender, RenderingStateChangedEventArgs e);
private void UpdateRenderModeIndicator(ContentType contentType);
```

### 3. MainWindow.FileOperations.cs

**Purpose:** Handles all file I/O operations and window state management.

**Contents:**
- File open/save/close operations
- File watching and auto-reload
- Window state persistence (position, size)
- Recent files management
- File path validation

**Key Methods:**
```csharp
private async Task OpenFileAsync(string filePath);
private async Task SaveFileAsync(string filePath);
private void CloseFile();
private void SetupFileWatcher(string filePath);
private async Task RestoreWindowStateAsync();
private async Task SaveWindowStateAsync();
```

### 4. MainWindow.UI.cs

**Purpose:** Contains all UI event handlers and dialog management.

**Contents:**
- Menu item click handlers (New, Open, Save, Export, etc.)
- Dialog creation and management
- Full screen mode toggle
- Presentation mode toggle
- Keyboard shortcut registration and handling
- Status bar updates
- InfoBar management

**Key Methods:**
```csharp
private void RegisterKeyboardShortcuts();
private void ToggleFullScreen_Click(object sender, RoutedEventArgs e);
private void PresentationMode_Click(object sender, RoutedEventArgs e);
private async void CheckSyntax_Click(object sender, RoutedEventArgs e);
private void ShowKeyboardShortcutTip();
private void DismissKeyboardTip_Click(object sender, RoutedEventArgs e);
```

### 5. MainWindow.AI.cs

**Purpose:** Manages AI-related features and integrations.

**Contents:**
- AI service initialization
- AI configuration management
- Floating AI prompt setup
- AI diagram generation
- AI settings dialog
- Pop-out window management
- Code import to canvas

**Key Methods:**
```csharp
private void InitializeAiServices();
private async Task OpenAiSettingsAndRefreshVmAsync();
private void PopOutFloatingPrompt();
private async Task ImportCodeToCanvasAsync(string code);
private void AiSettings_Click(object sender, RoutedEventArgs e);
```

### 6. MainWindow.Builder.cs

**Purpose:** Handles visual diagram builder functionality.

**Contents:**
- Builder initialization
- Canvas setup and configuration
- Shape toolbox integration
- Properties panel wiring
- Builder visibility management
- Code generation from canvas
- Canvas import/export

**Key Methods:**
```csharp
private void UpdateBuilderVisibility();
private void DiagramBuilderViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e);
private void BuilderTool_Click(object sender, RoutedEventArgs e);
private async Task ImportCodeToCanvasAsync(string code);
```

### 7. MainWindow.Updates.cs

**Purpose:** Manages Mermaid.js version checking and updates.

**Contents:**
- Update checking logic
- Version comparison
- Download and installation
- Update notifications
- InfoBar management for updates

**Key Methods:**
```csharp
private async Task CheckForMermaidUpdatesAsync();
private async Task CheckForNewerVersionAsync(string currentVersionStr);
private async void UpdateMermaid_Click(object sender, RoutedEventArgs e);
```

## Data Models

No new data models are required for this refactoring. All existing models remain unchanged.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

The refactoring focuses on code organization rather than functional changes. The key properties to verify are:

1. **Behavioral Equivalence**: All functionality works identically before and after refactoring
2. **Code Coverage**: All original code is preserved in the refactored files
3. **No Duplication**: No code is duplicated across partial class files
4. **Compilation**: The refactored code compiles without errors

### Correctness Properties

Property 1: Behavioral equivalence preservation
*For any* user interaction or system operation, the behavior after refactoring should be identical to the behavior before refactoring
**Validates: Requirements 3.1, 3.3, 3.4, 3.5**

Property 2: Code completeness
*For any* method in the original MainWindow.xaml.cs, that method should exist in exactly one of the refactored partial class files
**Validates: Requirements 1.1, 2.1, 6.6**

Property 3: File size constraints
*For any* partial class file, the file should contain no more than 500 lines of code
**Validates: Requirements 4.1, 4.2**

Property 4: Functional cohesion
*For any* method in a partial class file, the method should be related to the primary purpose of that file
**Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7**

Property 5: Build success
*For any* build configuration, the refactored code should compile without errors
**Validates: Requirements 3.2**

Property 6: Test compatibility
*For any* existing test, the test should pass after refactoring
**Validates: Requirements 3.3**

## Error Handling

The refactoring should not change any error handling behavior. All existing try-catch blocks, error logging, and error dialogs should be preserved in their respective partial class files.

**Error Handling Strategy:**
- Preserve all existing error handling logic
- Maintain error logging in the same locations
- Keep error dialogs in the UI partial class
- Ensure exception propagation remains unchanged

## Testing Strategy

### Verification Testing

**Manual Testing:**
1. Launch the application and verify it starts correctly
2. Test each major feature area:
   - Open/Save/Close files
   - WebView rendering
   - AI features
   - Visual builder
   - Export functionality
   - Keyboard shortcuts
   - Full screen mode
3. Verify all menu items work
4. Verify all dialogs display correctly

**Automated Testing:**
1. Run all existing unit tests
2. Run all existing integration tests
3. Verify test coverage remains the same
4. Add smoke tests for critical paths

### Regression Testing

**Test Cases:**
1. File operations (open, save, close)
2. Mermaid diagram rendering
3. Markdown rendering
4. AI diagram generation
5. Export to SVG/PNG/Word
6. Keyboard shortcuts
7. Full screen and presentation modes
8. Visual builder operations
9. Mermaid.js updates

### Code Review Checklist

- [ ] All methods moved to appropriate partial class files
- [ ] No duplicate code across files
- [ ] All using statements included in each file
- [ ] All files use correct namespace
- [ ] All files declare partial class correctly
- [ ] No methods orphaned or lost
- [ ] File sizes within limits
- [ ] Clear comments and documentation
- [ ] Build succeeds without errors
- [ ] All tests pass

## Migration Guide

### For Future Development

**Adding New WebView Functionality:**
- Add to `MainWindow.WebView.cs`
- Examples: New rendering modes, WebView settings, JavaScript interop

**Adding New File Operations:**
- Add to `MainWindow.FileOperations.cs`
- Examples: Import/export formats, file validation, recent files

**Adding New UI Elements:**
- Add to `MainWindow.UI.cs`
- Examples: Menu items, dialogs, keyboard shortcuts, status bar items

**Adding New AI Features:**
- Add to `MainWindow.AI.cs`
- Examples: New AI providers, prompt templates, AI settings

**Adding New Builder Features:**
- Add to `MainWindow.Builder.cs`
- Examples: New shapes, canvas operations, builder modes

**Adding New Export Formats:**
- Create new partial class file (e.g., `MainWindow.PdfExport.cs`)
- Follow the pattern established by `MainWindow.MarkdownToWord.cs`

### Refactoring Process

1. **Preparation:**
   - Create backup of MainWindow.xaml.cs
   - Ensure all tests pass before starting
   - Create new partial class files with headers

2. **Method Migration:**
   - Identify methods for each partial class
   - Copy methods to appropriate files
   - Remove methods from original file
   - Verify compilation after each batch

3. **Field and Property Management:**
   - Keep all fields in MainWindow.xaml.cs (core)
   - Ensure all partial classes can access needed fields
   - Update field accessibility if needed

4. **Testing:**
   - Build after each file is created
   - Run tests after each major migration
   - Perform manual testing of affected features

5. **Cleanup:**
   - Remove empty sections from original file
   - Organize using statements
   - Add file headers and documentation
   - Final build and test verification

## Performance Considerations

The refactoring should have no performance impact since:
- Partial classes are compiled into a single class
- No runtime overhead from file organization
- All method calls remain the same
- No additional abstractions or indirection

## Security Considerations

No security implications from this refactoring. All security-related code (file access, WebView security settings, etc.) will be preserved in their respective partial class files.

## Conclusion

This refactoring will significantly improve the maintainability of the MainWindow code by organizing it into logical, cohesive modules. Each partial class file will have a clear purpose and contain related functionality, making it easier for developers to find, understand, and modify code. The refactoring preserves all existing functionality while improving code organization and developer productivity.
