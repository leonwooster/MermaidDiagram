# Visual Diagram Builder (US-012) - Software Design Document

## 1. Overview
This document outlines the architecture and design for implementing a visual diagram builder in the Mermaid Diagram Editor. The builder will allow users to create diagrams through a graphical interface without needing to write Mermaid syntax directly.

## 2. Requirements
From US-012:
- A new 'Builder' panel in the UI
- Ability to add, edit, and connect nodes/elements
- Real-time Mermaid code generation
- Initial support for flowcharts

## 3. Architecture

### 3.1 High-Level Components

```
┌───────────────────────────────────────────────────────────┐
│                    Mermaid Diagram Editor                 │
│  ┌─────────────────┐        ┌─────────────────────────┐  │
│  │   Editor View   │        │     Builder View        │  │
│  │                 │        │  ┌───────────────────┐  │  │
│  │  [Code Editor]  │◄───────┤  │  Canvas           │  │  │
│  │                 │        │  │  ┌─────┐ ┌─────┐  │  │  │
│  └─────────────────┘        │  │  │     │ │     │  │  │  │
│                             │  │  └──┬──┘ └──┬──┘  │  │  │
│  ┌─────────────────┐        │  │     │       │     │  │  │
│  │   Preview Pane  │◄───────┼──┼─────┘       └─────┼──┘  │
│  │                 │        │  │  Toolbox         │     │
│  └─────────────────┘        │  │  ┌─────────────┐  │     │
│                             │  │  │ Add Node    │  │     │
│                             │  │  │ Add Edge    │  │     │
│                             │  │  │ Properties  │  │     │
│                             │  │  └─────────────┘  │     │
│                             │  └───────────────────┘     │
│                             └─────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

### 3.2 Component Responsibilities

1. **Builder View**
   - Main container for the visual editor
   - Manages the canvas and toolbox
   - Handles user interactions

2. **Canvas**
   - Renders the visual representation of the diagram
   - Handles drag-and-drop of elements
   - Manages selection and multi-selection

3. **Toolbox**
   - Contains draggable node types
   - Provides tools for creating connections
   - Includes property editors for selected elements

4. **Mermaid Code Generator**
   - Converts visual elements to Mermaid syntax
   - Maintains synchronization between visual and code views

## 4. Data Model

### 4.1 Core Classes

```csharp
public class DiagramElement
{
    public string Id { get; set; }
    public string Type { get; set; }  // 'node', 'edge', 'subgraph', etc.
    public Dictionary<string, object> Properties { get; set; }
    public Point Position { get; set; }
    public Size Size { get; set; }
}

public class Node : DiagramElement
{
    public string Text { get; set; }
    public string Shape { get; set; }  // 'rectangle', 'circle', 'diamond', etc.
    public List<Connection> OutgoingConnections { get; set; }
    public List<Connection> IncomingConnections { get; set; }
}

public class Connection
{
    public string Id { get; set; }
    public string SourceId { get; set; }
    public string TargetId { get; set; }
    public string Label { get; set; }
    public string LineType { get; set; }  // 'solid', 'dashed', 'dotted'
    public string ArrowType { get; set; }  // 'arrow', 'none', 'cross', etc.
}

public class Diagram
{
    public Dictionary<string, Node> Nodes { get; set; }
    public Dictionary<string, Connection> Connections { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    
    public string ToMermaid()
    {
        // Implementation to convert diagram to Mermaid syntax
    }
}
```

## 5. UI/UX Design

### 5.1 Builder Panel Layout
```
┌─────────────────────────────────────────────────┐
│ Toolbox │                                       │
│ ┌───────────────┐  ┌─────────────────────────┐ │
│ │ Node Types    │  │                         │ │
│ │ ┌───────────┐ │  │                         │ │
│ │ │ Rectangle │ │  │                         │ │
│ │ └───────────┘ │  │                         │ │
│ │ ┌───────────┐ │  │        Canvas          │ │
│ │ │  Diamond  │ │  │                         │ │
│ │ └───────────┘ │  │                         │ │
│ │ ┌───────────┐ │  │                         │ │
│ │ │   Text    │ │  │                         │ │
│ │ └───────────┘ │  │                         │ │
│ │               │  │                         │ │
│ └───────────────┘  └─────────────────────────┘ │
│                                                 │
│ ┌─────────────────────────────────────────────┐ │
│ │ Properties                                 │ │
│ │ ┌───────────────────────────────────────┐  │ │
│ │ │ Selected: [Node Name]                 │  │ │
│ │ │                                       │  │ │
│ │ │ Text: [_____________________________] │  │ │
│ │ │ Shape: [Dropdown]                     │  │ │
│ │ │ Style: [Dropdown]                     │  │ │
│ │ │                                       │  │ │
│ │ └───────────────────────────────────────┘  │ │
│ └─────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

## 6. Implementation Phases

### Phase 1: Core Infrastructure (Sprint 8.1)
- [ ] Create basic diagram data model
- [ ] Implement canvas rendering with basic shapes
- [ ] Add node creation and selection

### Phase 2: Editing Capabilities (Sprint 8.2)
- [ ] Implement connection creation between nodes
- [ ] Add property panel for element customization
- [ ] Support basic undo/redo functionality

### Phase 3: Mermaid Integration (Sprint 8.3)
- [ ] Implement Mermaid code generation
- [ ] Add two-way synchronization with code editor
- [ ] Support for basic flowchart syntax

### Phase 4: Polish and Refinement (Sprint 8.4)
- [ ] Add visual feedback for interactions
- [ ] Implement keyboard shortcuts
- [ ] Add basic validation and error handling

## 7. Technical Considerations

### 7.1 Performance
- Use virtualization for large diagrams
- Implement efficient hit testing
- Batch UI updates for better performance

### 7.2 Accessibility
- Keyboard navigation support
- Screen reader compatibility
- High contrast mode

### 7.3 Testing Strategy
- Unit tests for data model and Mermaid generation
- UI tests for interaction scenarios
- Performance testing with large diagrams

## 8. Open Questions
1. Should we support subgraphs in the initial implementation?
2. What's the maximum number of nodes/edges we need to support?
3. Do we need to support importing existing Mermaid code into the visual editor?

## 9. Future Enhancements
1. Support for other diagram types (sequence, class, etc.)
2. Templates for common diagram patterns
3. Advanced styling and theming options
4. Collaboration features

## 10. Dependencies
- WinUI 3 for UI components
- .NET 6.0 or later
- Mermaid.js for rendering previews
