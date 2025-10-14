# Epic: Visual Diagram Builder with Drag-and-Drop Canvas

## Overview
Transform the diagram creation experience by providing a professional drag-and-drop visual editor. Users can drag shapes from a categorized toolbox onto a canvas, connect them visually, and have the application automatically generate the corresponding Mermaid syntax. This eliminates the need to learn Mermaid syntax for basic diagram creation while maintaining the power of code-based editing for advanced users.

## Status: **PLANNED**
**Completed Stories:** 0 of 17 (0%)  
**Target Implementation Date:** TBD  
**Current State:** Basic flowchart builder exists but is disabled due to sync issues

## Architecture & Design Principles
* __Single Responsibility (S)__ — Separate concerns: canvas rendering (`DiagramCanvas`), shape management (`ShapeToolbox`), properties editing (`PropertiesPanel`), and code synchronization (`MermaidCodeGenerator`, `MermaidCodeParser`).
* __Open/Closed (O)__ — Allow new diagram types and shapes to be added through extensible shape libraries without modifying core canvas logic.
* __Liskov Substitution (L)__ — All shape types implement `ICanvasElement` interface and can be treated uniformly by the canvas renderer.
* __Interface Segregation (I)__ — Provide focused interfaces: `ICanvasElement` for drawable elements, `IConnectable` for nodes that support connections, `IDraggable` for movable elements.
* __Dependency Inversion (D)__ — Canvas depends on `ICanvasElement` abstractions rather than concrete shape implementations. Use factory pattern for shape creation.

## Design Patterns
* __Factory Pattern__ — `ShapeFactory` creates appropriate shape instances based on template selection.
* __Command Pattern__ — All canvas operations (add, move, delete, connect) implement `ICanvasCommand` for undo/redo support.
* __Observer Pattern__ — Canvas state changes notify code generator to update Mermaid syntax.
* __Strategy Pattern__ — Different diagram types use different code generation strategies (`FlowchartGenerator`, `ClassDiagramGenerator`, etc.).
* __Memento Pattern__ — Canvas state snapshots for undo/redo functionality.

## User Stories

### Story 1: Three-Panel Layout with Canvas
**As a** user  
**I want** a visual canvas in the center with toolbox on the left and properties on the right  
**So that** I can build diagrams visually without writing code

**Acceptance Criteria:**
- [ ] Main window layout changes to three-panel design when Diagram Builder is enabled
- [ ] Left panel shows collapsible shape toolbox with categories (General, Flowchart, UML, etc.)
- [ ] Center panel displays canvas with grid background and zoom controls
- [ ] Right panel shows properties for selected elements
- [ ] Panels are resizable with splitters
- [ ] Layout persists across sessions
- [ ] Toggle between builder mode and code-only mode via View menu

**Estimated Effort:** 13 story points

### Story 2: Shape Toolbox with Categories
**As a** user  
**I want** organized shape libraries by diagram type  
**So that** I can quickly find the shapes I need

**Acceptance Criteria:**
- [ ] Toolbox displays shape categories: General, Flowchart, UML, Entity-Relation, Arrows
- [ ] Each category is collapsible/expandable
- [ ] Shapes show visual preview icons
- [ ] Hover shows shape name and description
- [ ] Search/filter functionality to find shapes quickly
- [ ] Shapes are organized by frequency of use within categories
- [ ] Custom category for user-defined shapes (future)

**Estimated Effort:** 8 story points

### Story 3: Drag-and-Drop from Toolbox to Canvas
**As a** user  
**I want** to drag shapes from the toolbox onto the canvas  
**So that** I can quickly add elements to my diagram

**Acceptance Criteria:**
- [ ] Dragging a shape from toolbox shows visual preview during drag
- [ ] Dropping on canvas creates new shape at drop location
- [ ] Snap-to-grid option for precise alignment
- [ ] Shape appears with default size and styling
- [ ] Unique ID auto-generated for each shape
- [ ] Undo/redo support for shape creation
- [ ] Canvas auto-scrolls when dragging near edges

**Estimated Effort:** 13 story points

### Story 4: Canvas Node Manipulation
**As a** user  
**I want** to select, move, and resize shapes on the canvas  
**So that** I can arrange my diagram layout

**Acceptance Criteria:**
- [ ] Click to select a shape (shows selection handles)
- [ ] Drag to move selected shape(s)
- [ ] Resize handles on corners and edges
- [ ] Multi-select with Ctrl+Click or rubber-band selection
- [ ] Arrow keys move selected shapes (Shift for larger steps)
- [ ] Delete key removes selected shapes
- [ ] Alignment guides appear when aligning with other shapes
- [ ] Properties panel updates when selection changes

**Estimated Effort:** 13 story points

### Story 5: Visual Connection Drawing
**As a** user  
**I want** to draw connections between shapes by clicking and dragging  
**So that** I can show relationships visually

**Acceptance Criteria:**
- [ ] Click on shape shows connection points (top, bottom, left, right, center)
- [ ] Drag from connection point to another shape creates connection
- [ ] Connection line follows cursor during drag with visual feedback
- [ ] Connection snaps to target shape's connection points
- [ ] Different arrow styles available (solid, dashed, dotted, thick)
- [ ] Arrow heads configurable (none, single, double, diamond, circle)
- [ ] Connection labels can be added by double-clicking line
- [ ] Connections auto-route to avoid overlapping shapes (orthogonal routing)

**Estimated Effort:** 21 story points

### Story 6: Properties Panel for Styling
**As a** user  
**I want** to customize shape appearance through a properties panel  
**So that** I can style my diagram without writing code

**Acceptance Criteria:**
- [ ] Properties panel shows diagram-level settings when nothing selected
- [ ] Shows node properties when shape(s) selected: position, size, text, font, colors
- [ ] Shows connection properties when line selected: style, arrows, label, color
- [ ] Changes apply immediately to canvas
- [ ] Undo/redo support for property changes
- [ ] Multi-select shows common properties only
- [ ] Color picker for fill and stroke colors
- [ ] Font selector with preview

**Estimated Effort:** 13 story points

### Story 7: Canvas to Mermaid Code Generation
**As a** developer  
**I want** the canvas state to automatically generate Mermaid code  
**So that** I can see the code representation of my visual diagram

**Acceptance Criteria:**
- [ ] Canvas changes trigger code generation with 300ms debounce
- [ ] Generated code appears in code editor panel
- [ ] Code is properly formatted and indented
- [ ] Node positions preserved as comments or metadata (for re-import)
- [ ] Supports all Mermaid diagram types (flowchart, class, sequence, etc.)
- [ ] Code generation is incremental (only updates changed parts)
- [ ] Generated code is valid and renders correctly in preview

**Estimated Effort:** 13 story points

### Story 8: Mermaid Code to Canvas Parsing
**As a** user  
**I want** to import existing Mermaid code into the visual builder  
**So that** I can edit existing diagrams visually

**Acceptance Criteria:**
- [ ] "Import to Canvas" button parses current code editor content
- [ ] Extracts nodes, connections, and styling from Mermaid syntax
- [ ] Attempts to preserve positions from metadata/comments
- [ ] Auto-layouts diagram if no position data available
- [ ] Shows warning if code contains unsupported features
- [ ] Handles syntax errors gracefully with error messages
- [ ] Supports flowchart, class diagram, and ER diagram initially

**Estimated Effort:** 21 story points

### Story 9: Zoom and Pan Controls
**As a** user  
**I want** to zoom and pan the canvas  
**So that** I can work with large diagrams efficiently

**Acceptance Criteria:**
- [ ] Mouse wheel zooms in/out (Ctrl+Wheel for finer control)
- [ ] Zoom levels: 25%, 50%, 75%, 100%, 125%, 150%, 200%, 400%
- [ ] Zoom controls in toolbar and status bar
- [ ] "Fit to Window" button auto-scales diagram to visible area
- [ ] "Actual Size" button resets to 100%
- [ ] Space+Drag or Middle-click+Drag pans canvas
- [ ] Mini-map in corner shows overview of large diagrams
- [ ] Zoom level persists per diagram

**Estimated Effort:** 8 story points

### Story 10: Undo/Redo System
**As a** user  
**I want** undo and redo functionality for all canvas operations  
**So that** I can experiment without fear of losing work

**Acceptance Criteria:**
- [ ] Ctrl+Z undoes last action
- [ ] Ctrl+Y or Ctrl+Shift+Z redoes action
- [ ] Undo/Redo buttons in toolbar show enabled/disabled state
- [ ] Supports all operations: add, delete, move, resize, connect, style changes
- [ ] Undo history limit of 50 actions (configurable)
- [ ] Undo stack clears when switching diagrams
- [ ] Status bar shows current action description
- [ ] Undo/redo works across canvas and code editor

**Estimated Effort:** 13 story points

### Story 11: Alignment and Distribution Tools
**As a** user  
**I want** tools to align and distribute multiple shapes  
**So that** I can create professional-looking layouts

**Acceptance Criteria:**
- [ ] Toolbar buttons for alignment: Left, Center, Right, Top, Middle, Bottom
- [ ] Distribution tools: Horizontal, Vertical
- [ ] "Bring to Front" and "Send to Back" for layering
- [ ] Alignment works on multi-selected shapes
- [ ] Alignment guides show during manual dragging
- [ ] Smart spacing suggestions
- [ ] Keyboard shortcuts for common alignments

**Estimated Effort:** 8 story points

### Story 12: Diagram Type Switcher
**As a** user  
**I want** to switch between different diagram types  
**So that** I can create various diagram styles with appropriate shapes

**Acceptance Criteria:**
- [ ] Diagram type dropdown in toolbar: Flowchart, Class Diagram, Sequence, State, ER
- [ ] Switching type changes available shapes in toolbox
- [ ] Switching type changes code generation strategy
- [ ] Warning shown if current canvas elements incompatible with new type
- [ ] Each type has appropriate default grid size and styling
- [ ] Type is saved with diagram file
- [ ] Type detection from existing Mermaid code

**Estimated Effort:** 13 story points

### Story 13: Context Menus and Shortcuts
**As a** user  
**I want** context menus and keyboard shortcuts  
**So that** I can work efficiently

**Acceptance Criteria:**
- [ ] Right-click on shape: Edit Text, Duplicate, Delete, Properties, Bring to Front, Send to Back
- [ ] Right-click on connection: Edit Label, Change Style, Delete
- [ ] Right-click on canvas: Paste, Select All, Grid Settings, Background
- [ ] Keyboard shortcuts: Ctrl+C/V/X (copy/paste/cut), Ctrl+D (duplicate), Delete, Ctrl+A (select all)
- [ ] Arrow keys move selected shapes (1px), Shift+Arrow (10px)
- [ ] Tab key cycles through shapes
- [ ] Double-click shape to edit text inline

**Estimated Effort:** 8 story points

### Story 14: Grid and Snap Settings
**As a** user  
**I want** to configure grid display and snapping behavior  
**So that** I can control precision and visual guides

**Acceptance Criteria:**
- [ ] Toggle grid visibility on/off
- [ ] Toggle snap-to-grid on/off
- [ ] Configurable grid size (5px, 10px, 20px, 50px)
- [ ] Grid color and opacity settings
- [ ] Snap tolerance setting
- [ ] Snap to other shapes (alignment guides)
- [ ] Settings accessible via canvas right-click menu
- [ ] Settings persist per user preference

**Estimated Effort:** 5 story points

### Story 15: Canvas State Persistence
**As a** developer  
**I want** canvas state to be saved with diagram files  
**So that** visual layouts are preserved

**Acceptance Criteria:**
- [ ] Canvas state (positions, sizes, styles) saved as metadata in .mmd file
- [ ] Metadata stored as Mermaid comments or separate JSON section
- [ ] Opening file restores canvas state if available
- [ ] Graceful fallback to auto-layout if metadata missing or corrupted
- [ ] Export option to save canvas state separately
- [ ] Version compatibility handling for future format changes

**Estimated Effort:** 8 story points

### Story 16: Image and Animated Image Support
**As a** user  
**I want** to add static images and animated images (GIFs) to the canvas  
**So that** I can enrich my diagrams with visual content and illustrations

**Acceptance Criteria:**
- [ ] "Insert Image" button in toolbar or toolbox
- [ ] File picker supports common formats: PNG, JPG, JPEG, BMP, SVG, GIF, WEBP
- [ ] Drag-and-drop image files directly onto canvas
- [ ] Images can be moved, resized, and deleted like other nodes
- [ ] Animated GIFs play automatically on canvas
- [ ] Image properties panel shows: source path, size, opacity, rotation
- [ ] Images maintain aspect ratio by default (with option to unlock)
- [ ] Images can be layered (bring to front/send to back)
- [ ] Image paths stored as relative or absolute in diagram metadata
- [ ] Images export correctly when diagram is saved/exported
- [ ] Placeholder shown if image file not found
- [ ] Copy/paste images between diagrams
- [ ] Performance: Handle 20+ images without lag

**Estimated Effort:** 13 story points

### Story 17: Diagram Builder File Format and Persistence
**As a** user  
**I want** to save and load my visual diagram work in a dedicated file format  
**So that** I can preserve the full canvas state including positions, connections, and visual properties

**Acceptance Criteria:**
- [ ] New file extension for Diagram Builder files (e.g., `.mmdx`, `.mermaidx`, or `.vmd` for Visual Mermaid Diagram)
- [ ] File format stores complete canvas state: nodes, edges, positions, sizes, styles, images
- [ ] File format includes both visual data and generated Mermaid code
- [ ] "Save As Diagram Builder File" option in File menu
- [ ] "Open Diagram Builder File" recognizes and loads the custom format
- [ ] File format is JSON-based for human readability and version control compatibility
- [ ] Backward compatibility: Can import plain `.mmd` files and convert to builder format
- [ ] Forward compatibility: Can export to plain `.mmd` format (loses visual metadata)
- [ ] File format includes metadata: version, creation date, last modified, author
- [ ] Auto-save functionality with configurable interval (default: 2 minutes)
- [ ] Recovery from unsaved changes on crash or unexpected close
- [ ] File format supports compression for large diagrams with images
- [ ] Clear distinction in UI between "Mermaid Code File (.mmd)" and "Diagram Builder File (.mmdx)"
- [ ] Recent files list shows file type indicator
- [ ] File association registers custom extension with Windows

**File Format Structure (JSON):**
```json
{
  "version": "1.0",
  "metadata": {
    "created": "2025-01-01T00:00:00Z",
    "modified": "2025-01-01T00:00:00Z",
    "author": "User Name",
    "diagramType": "flowchart"
  },
  "canvas": {
    "nodes": [...],
    "connectors": [...],
    "images": [...],
    "gridSize": 20,
    "snapToGrid": true
  },
  "mermaidCode": "flowchart TD\n  A[Start]...",
  "settings": {
    "zoom": 1.0,
    "panOffset": {"x": 0, "y": 0}
  }
}
```

**Estimated Effort:** 13 story points

## Technical Considerations

### Implementation Approach
1. **Phase 1: Core Canvas Infrastructure** - Three-panel layout, canvas control, shape toolbox, drag-and-drop
2. **Phase 2: Canvas Interactions** - Node selection/dragging/resizing, multi-select, connection drawing, properties panel
3. **Phase 3: Code Synchronization** - Canvas → code generation, code → canvas parsing, conflict resolution, metadata format
4. **Phase 4: Advanced Features** - Undo/redo system, zoom/pan, alignment tools, context menus, file format and persistence
5. **Phase 5: Multi-Diagram Support** - Diagram type switcher, type-specific libraries, type-specific generators, validation

### Technology Stack
- **Canvas Rendering**: WinUI3 `Canvas` control with XAML shapes
- **Alternative**: Win2D for advanced graphics (if performance issues)
- **Drag-Drop**: WinUI3 drag-drop APIs with custom adorners
- **Undo/Redo**: Command pattern with history stack
- **Code Generation**: Template-based generators per diagram type
- **Parsing**: Regex + custom parser for Mermaid syntax
- **File Format**: JSON-based with System.Text.Json serialization
- **File Extension**: `.mmdx` or `.vmd` for Diagram Builder files

### File Structure
See [DIAGRAM_BUILDER_DESIGN.md](../design/diagram-builder-design.md) for detailed architecture.

## Definition of Done
- [ ] All 17 user stories implemented and tested
- [ ] Unit tests for canvas operations (>80% coverage)
- [ ] Integration tests for code generation and parsing
- [ ] Performance: Handle 100+ nodes without lag
- [ ] Documentation updated
- [ ] Accessibility: Keyboard navigation, screen reader support
- [ ] Cross-theme compatibility (light/dark modes)
- [ ] No regressions in existing code editor functionality

## Priority: High
## Target Sprint: Next 4-6 Sprints
## Dependencies
- Existing Mermaid rendering infrastructure
- WebView2 preview system
- File operations (Open/Save)
- Current DiagramBuilderViewModel (to be refactored)

## Success Metrics
- 70%+ of users prefer visual builder for simple diagrams
- Average diagram creation time reduced by 50% for new users
- Zero data loss during canvas ↔ code synchronization
- Canvas operations complete in <16ms (60 FPS)
- Support for 90%+ of common Mermaid diagram features
- User satisfaction score >4.5/5 for builder usability

## Risks & Mitigation
- **Risk**: Canvas performance degrades with large diagrams  
  **Mitigation**: Implement virtualization, render only visible area, use Win2D if needed

- **Risk**: Mermaid ↔ Canvas sync conflicts cause data loss  
  **Mitigation**: Implement mode switching (Canvas Mode vs Code Mode), save both representations

- **Risk**: Complex Mermaid features not representable in visual builder  
  **Mitigation**: Clearly document limitations, allow hybrid editing, show warnings

- **Risk**: Drag-drop UX issues on touch devices  
  **Mitigation**: Test on touch devices, provide alternative input methods

- **Risk**: Undo/redo system memory consumption  
  **Mitigation**: Limit history depth, use efficient memento pattern, compress old states

## Current State Analysis
The application currently has a basic flowchart builder (`DiagramBuilderViewModel`) that:
- ✅ Manages nodes and edges collections
- ✅ Generates Mermaid code from model
- ✅ Parses Mermaid code into model
- ❌ Is disabled due to sync bugs (overwrites user code)
- ❌ Uses list-based UI instead of visual canvas
- ❌ No drag-and-drop support
- ❌ No visual connection drawing
- ❌ Limited to flowcharts only

## Migration Path
1. Keep existing `DiagramBuilderViewModel` as data model
2. Add new `DiagramCanvasViewModel` for visual layer
3. Refactor sync logic to prevent conflicts
4. Gradually migrate UI from list-based to canvas-based
5. Add new diagram type support incrementally

## Related Documentation
- [Technical Design Document](../design/diagram-builder-design.md)
- [User Guide](../user-guides/visual-diagram-builder-guide.md)
