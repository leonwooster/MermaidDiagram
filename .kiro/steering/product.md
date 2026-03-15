# Product: WinUI3 Mermaid Diagram Editor

A Windows desktop application for writing, editing, and previewing Mermaid diagrams in real-time. Primary focus is on UML diagram types (class, sequence, state, activity) with support for flowcharts, Gantt charts, and other Mermaid diagram types.

## Core Capabilities
- Split-pane editor with live WebView2 preview (debounced at 500ms)
- Supports `.mmd` (Mermaid), `.md` (Markdown), and `.mmdx` (diagram builder) file formats
- Content type auto-detection: pure Mermaid, Markdown, or Markdown-with-embedded-Mermaid
- Export to PNG, SVG, and Word (.docx) formats
- Mermaid syntax linting with auto-fix suggestions
- Visual diagram builder for flowcharts (drag-and-drop node/edge creation)
- AI-assisted diagram generation via Ollama or OpenAI backends
- Keyboard shortcut customization, full-screen/presentation modes
- Recent files tracking, search, synchronized scroll between editor and preview
- Markdown style settings (font, line height, code font) persisted per user
- Automatic Mermaid.js version checking against CDN on startup
