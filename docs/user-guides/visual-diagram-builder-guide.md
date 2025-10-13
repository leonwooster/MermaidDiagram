# Visual Diagram Builder - User Guide

## Overview

The Visual Diagram Builder is a professional drag-and-drop editor that allows you to create diagrams without writing Mermaid code. This feature is perfect for users who prefer visual tools while still maintaining the power and flexibility of Mermaid syntax.

**Status:** Coming Soon (Currently in Design Phase)

---

## What to Expect

### Three-Panel Interface

When you enable the Diagram Builder, the interface transforms into a three-panel layout:

#### Left Panel: Shape Toolbox
- **Categorized Shape Libraries**: Browse shapes organized by type
  - General (rectangles, circles, diamonds, etc.)
  - Flowchart (process, decision, start/end, etc.)
  - UML (class, interface, actor, package)
  - Entity-Relation (entity, relationship, attribute)
  - Arrows & Connectors (various line styles and arrow types)
- **Search & Filter**: Quickly find the shape you need
- **Visual Previews**: See what each shape looks like before adding it
- **Collapsible Categories**: Expand only the categories you need

#### Center Panel: Visual Canvas
- **Grid Background**: Helps with alignment and spacing
- **Drag-and-Drop**: Add shapes by dragging from toolbox
- **Visual Editing**: Move, resize, and connect shapes with your mouse
- **Zoom Controls**: Zoom in/out to work with large diagrams
- **Pan Support**: Navigate large diagrams easily
- **Selection Tools**: Select single or multiple shapes
- **Connection Points**: Visual indicators for where to connect shapes

#### Right Panel: Properties
- **Diagram Settings**: Grid size, snap-to-grid, background color
- **Node Properties**: Position, size, text, font, colors, shape type
- **Connection Properties**: Line style, arrow type, label, color
- **Live Updates**: Changes apply immediately to the canvas

---

## Key Features

### Drag-and-Drop Workflow
1. Browse shapes in the left toolbox
2. Drag a shape onto the canvas
3. Drop it where you want it
4. The shape appears with default styling
5. Mermaid code is automatically generated

### Visual Connection Drawing
1. Click on a shape to see connection points
2. Drag from a connection point
3. Drag to another shape's connection point
4. Release to create the connection
5. Double-click the line to add a label

### Shape Manipulation
- **Select**: Click a shape to select it
- **Move**: Drag selected shapes to reposition them
- **Resize**: Drag corner/edge handles to resize
- **Multi-Select**: Ctrl+Click or drag a selection box
- **Delete**: Press Delete key to remove selected items
- **Copy/Paste**: Ctrl+C/V to duplicate shapes

### Styling and Customization
- **Fill Color**: Change shape background color
- **Border**: Customize border color, width, and style
- **Font**: Choose font family, size, and color
- **Line Style**: Solid, dashed, dotted, or thick lines
- **Arrow Types**: Various arrow head styles

### Automatic Code Generation
- Canvas changes automatically generate Mermaid code
- Code appears in the code editor (300ms debounce)
- Generated code is properly formatted and indented
- Node positions saved as metadata for round-trip editing

### Code Import
- Click "Import to Canvas" to parse existing Mermaid code
- Extracts nodes, connections, and styling
- Preserves positions if metadata is available
- Auto-layouts diagram if no position data exists

---

## Supported Diagram Types

### Flowcharts
- Process steps, decisions, start/end points
- Data flow, documents, manual operations
- Various arrow styles and connection types

### UML Class Diagrams
- Classes with attributes and methods
- Interfaces and abstract classes
- Inheritance, composition, aggregation relationships

### UML Sequence Diagrams
- Actors and objects
- Message flows and lifelines
- Activation boxes and notes

### State Diagrams
- States and transitions
- Entry/exit actions
- Composite states

### Entity-Relationship Diagrams
- Entities and attributes
- Relationships and cardinality
- Weak entities and identifying relationships

---

## How to Use (When Available)

### Enabling the Diagram Builder
1. Go to **View** menu
2. Click **Diagram Builder**
3. The interface switches to three-panel layout
4. Toolbox appears on the left, properties on the right

### Creating Your First Diagram
1. **Select Diagram Type**: Choose from dropdown (Flowchart, Class, etc.)
2. **Add Shapes**: Drag shapes from toolbox to canvas
3. **Position Shapes**: Arrange them as needed
4. **Connect Shapes**: Draw connections between shapes
5. **Style Elements**: Use properties panel to customize
6. **Review Code**: Check the generated Mermaid code
7. **Save**: Save your diagram as usual

### Working with Shapes

#### Adding Shapes
- Drag from toolbox and drop on canvas
- Shape appears with default size and text
- Unique ID is auto-generated

#### Editing Shape Text
- Double-click a shape to edit text inline
- Or select shape and edit in properties panel
- Press Enter to confirm, Esc to cancel

#### Moving Shapes
- Click and drag to move
- Use arrow keys for precise positioning (1px)
- Hold Shift+Arrow for larger steps (10px)
- Alignment guides appear when near other shapes

#### Resizing Shapes
- Select shape to show resize handles
- Drag corner handles to resize proportionally
- Drag edge handles to resize in one direction
- Size shown in properties panel

### Working with Connections

#### Drawing Connections
1. Click on source shape (connection points appear)
2. Click and drag from a connection point
3. Drag to target shape
4. Release on target's connection point
5. Connection is created

#### Editing Connections
- Click connection to select it
- Double-click to add/edit label
- Use properties panel to change style
- Change arrow types and line styles

#### Connection Routing
- Connections auto-route to avoid shapes
- Orthogonal routing for clean diagrams
- Manual adjustment points (future feature)

### Zoom and Pan

#### Zooming
- **Mouse Wheel**: Zoom in/out
- **Ctrl+Wheel**: Finer zoom control
- **Toolbar Buttons**: Zoom In, Zoom Out, Fit to Window, Actual Size
- **Zoom Levels**: 25%, 50%, 75%, 100%, 125%, 150%, 200%, 400%

#### Panning
- **Space+Drag**: Pan the canvas
- **Middle-Click+Drag**: Alternative pan method
- **Scroll Bars**: Traditional scrolling

### Alignment Tools

#### Aligning Shapes
1. Select multiple shapes (Ctrl+Click or drag selection box)
2. Use toolbar alignment buttons:
   - Align Left, Center, Right
   - Align Top, Middle, Bottom
3. Shapes align based on selection

#### Distributing Shapes
- Distribute Horizontally: Equal horizontal spacing
- Distribute Vertically: Equal vertical spacing
- Works with 3+ selected shapes

### Undo and Redo
- **Ctrl+Z**: Undo last action
- **Ctrl+Y**: Redo action
- **Toolbar Buttons**: Undo/Redo buttons show enabled state
- **History Limit**: Last 50 actions (configurable)
- **Status Bar**: Shows current action description

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+C` | Copy selected shapes |
| `Ctrl+V` | Paste shapes |
| `Ctrl+X` | Cut selected shapes |
| `Ctrl+D` | Duplicate selected shapes |
| `Ctrl+A` | Select all shapes |
| `Delete` | Delete selected shapes |
| `Arrow Keys` | Move selected shapes (1px) |
| `Shift+Arrow` | Move selected shapes (10px) |
| `Tab` | Cycle through shapes |
| `Space+Drag` | Pan canvas |
| `Ctrl+Wheel` | Zoom in/out |
| `Double-Click` | Edit shape text inline |

---

## Context Menus

### Right-Click on Shape
- Edit Text
- Duplicate
- Delete
- Properties
- Bring to Front
- Send to Back

### Right-Click on Connection
- Edit Label
- Change Style
- Delete

### Right-Click on Canvas
- Paste
- Select All
- Grid Settings
- Background Color

---

## Synchronization Modes

### Canvas-First Mode (Default)
- Canvas is the source of truth
- Code is automatically generated
- Code editor shows warning banner
- Best for visual editing workflow

### Code-First Mode
- Code editor is the source of truth
- Canvas shows parsed view (read-only)
- "Import to Canvas" button available
- Best for code editing workflow

### Switching Modes
1. Click mode indicator in status bar
2. Select desired mode
3. Confirm if unsaved changes exist

---

## Grid and Snap Settings

### Grid Display
- Toggle grid visibility on/off
- Configurable grid size: 5px, 10px, 20px, 50px
- Grid color and opacity adjustable

### Snap to Grid
- Toggle snap-to-grid on/off
- Shapes snap to grid intersections when moving
- Configurable snap tolerance
- Snap to other shapes (alignment guides)

### Accessing Settings
- Right-click on canvas → Grid Settings
- Or use View menu → Grid Settings

---

## Tips and Best Practices

### For Best Results
- **Start with Layout**: Position shapes before connecting them
- **Use Grid**: Enable snap-to-grid for clean alignment
- **Group Related Items**: Keep related shapes close together
- **Use Alignment Tools**: Align and distribute for professional look
- **Label Connections**: Add labels to clarify relationships
- **Save Frequently**: Use Ctrl+S to save your work

### Performance Tips
- **Large Diagrams**: Use zoom and pan to navigate
- **Many Shapes**: Consider splitting into multiple diagrams
- **Complex Connections**: Use orthogonal routing for clarity

### Workflow Tips
- **Prototype Visually**: Start with visual builder for quick prototyping
- **Refine with Code**: Switch to code mode for advanced features
- **Hybrid Approach**: Use both modes as needed
- **Save Metadata**: Keep canvas metadata for future visual editing

---

## Limitations

### Current Limitations (When Released)
- Complex Mermaid features may not be fully supported in visual mode
- Some advanced syntax requires code editing
- Maximum recommended: 100 nodes per diagram for optimal performance
- Touch/pen input may have limited support initially

### Workarounds
- Use hybrid editing: visual for layout, code for advanced features
- Split large diagrams into multiple smaller diagrams
- Switch to code mode for unsupported features
- Check generated code for accuracy

---

## Troubleshooting

### Canvas Not Responding
- Check if you're in Code-First mode
- Try switching to Canvas-First mode
- Restart the application if needed

### Code Not Generating
- Ensure you're in Canvas-First mode
- Check if canvas has any elements
- Look for error messages in status bar

### Shapes Not Connecting
- Ensure you're dragging from connection point
- Check if target shape has connection points
- Try zooming in for better precision

### Performance Issues
- Reduce number of shapes on canvas
- Disable grid if not needed
- Close other applications to free memory

---

## Related Documentation

- [Technical Design Document](../design/diagram-builder-design.md)
- [Epic and User Stories](../features/visual-diagram-builder-epic.md)
- [Main User Guide](../USER_GUIDE.md)

---

**Note**: This feature is currently in the design phase. The interface and functionality described here represent the planned implementation and may change during development.
