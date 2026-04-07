# Requirements Document

## Introduction

This feature adds hover-activated action icons on rendered Mermaid diagrams in the preview panel. When the user hovers over a diagram, a small toolbar appears with a zoom-in icon and a download-as-PNG icon. Clicking the zoom-in icon opens a dedicated zoom panel that sits side-by-side with the existing code and preview panes. The zoom panel provides zoom in/out controls and an exit button that closes the panel and restores the original layout.

The feature applies to both pure Mermaid rendering mode and embedded Mermaid diagrams within Markdown content.

## Glossary

- **Preview_Panel**: The WebView2-based panel (PreviewBrowser) that renders Mermaid diagrams and Markdown content using UnifiedRenderer.html.
- **Diagram_SVG**: The rendered SVG element produced by Mermaid.js inside the Preview_Panel.
- **Hover_Toolbar**: A small floating toolbar that appears over a Diagram_SVG when the user hovers over it, containing action icons (zoom-in, download PNG).
- **Zoom_Panel**: A new panel that opens alongside the Preview_Panel and Code_Panel to display an enlarged, zoomable copy of a diagram. It includes zoom-in, zoom-out, and exit controls.
- **Code_Panel**: The TextControlBox code editor on the left side of the main window.
- **Content_Container**: The HTML element (`#content-container`) in UnifiedRenderer.html that holds rendered content.
- **WebView_Message**: A JSON message sent between the WebView2 JavaScript layer and the C# host via `postMessage` / `WebMessageReceived`.

## Requirements

### Requirement 1: Hover Toolbar on Diagrams

**User Story:** As a user, I want to see action icons when I hover over a Mermaid diagram, so that I can quickly zoom in or download the diagram without extra clicks.

#### Acceptance Criteria

1. WHEN the user hovers the mouse over a Diagram_SVG in the Preview_Panel, THE Hover_Toolbar SHALL appear overlaid on the diagram (e.g., top-right corner).
2. THE Hover_Toolbar SHALL contain a zoom-in icon and a download-as-PNG icon.
3. WHEN the user moves the mouse away from both the Diagram_SVG and the Hover_Toolbar, THE Hover_Toolbar SHALL disappear.
4. WHEN the Preview_Panel is in mermaid-mode (pure Mermaid rendering), THE Hover_Toolbar SHALL appear when hovering over the single rendered diagram.
5. WHEN the Preview_Panel is in markdown-mode with embedded Mermaid diagrams, THE Hover_Toolbar SHALL appear independently for each embedded Mermaid diagram.
6. THE Hover_Toolbar SHALL not interfere with the diagram rendering or scroll behavior.

### Requirement 2: Download as PNG

**User Story:** As a user, I want to download a diagram as a PNG image directly from the hover toolbar, so that I can quickly export individual diagrams without using the menu.

#### Acceptance Criteria

1. WHEN the user clicks the download-as-PNG icon on the Hover_Toolbar, THE system SHALL export the hovered Diagram_SVG as a PNG file.
2. THE download-as-PNG action SHALL reuse the existing PNG export functionality (SVG-to-PNG rasterization via Svg.Skia/SkiaSharp).
3. THE system SHALL prompt the user with a file save dialog to choose the output location and filename.
4. WHEN the export completes, THE system SHALL not close or alter the Preview_Panel state.

### Requirement 3: Zoom Panel Opening

**User Story:** As a user, I want to click the zoom-in icon to open a dedicated zoom panel alongside the editor, so that I can inspect diagram details while keeping the code and preview visible.

#### Acceptance Criteria

1. WHEN the user clicks the zoom-in icon on the Hover_Toolbar, THE system SHALL open the Zoom_Panel as a new pane alongside the existing Code_Panel and Preview_Panel.
2. THE Zoom_Panel SHALL display the selected Diagram_SVG at a size that fits within the panel while preserving the original aspect ratio.
3. THE Zoom_Panel SHALL sit side-by-side with the Preview_Panel and Code_Panel (three-pane layout).
4. THE system SHALL save the current sizes and positions of the Code_Panel and Preview_Panel before opening the Zoom_Panel, so they can be restored on exit.

### Requirement 4: Zoom Panel Controls

**User Story:** As a user, I want zoom-in, zoom-out, and exit controls in the zoom panel, so that I can adjust the diagram scale and close the panel when done.

#### Acceptance Criteria

1. THE Zoom_Panel SHALL provide a zoom-in button that increases the diagram scale by a fixed increment (e.g., 25%).
2. THE Zoom_Panel SHALL provide a zoom-out button that decreases the diagram scale by a fixed increment (e.g., 25%).
3. THE Zoom_Panel SHALL display the current zoom level as a percentage.
4. THE Zoom_Panel SHALL provide an exit button that closes the panel.
5. WHEN the diagram is zoomed beyond the Zoom_Panel viewport, THE Zoom_Panel SHALL allow the user to scroll to view all parts of the diagram.
6. THE Zoom_Panel SHALL clamp the zoom level to a minimum of 25% and a maximum of 500%.

### Requirement 5: Zoom Panel Closing and Layout Restoration

**User Story:** As a user, I want the zoom panel to close cleanly and restore the original layout, so that my editing workspace returns to its previous state.

#### Acceptance Criteria

1. WHEN the user clicks the exit button in the Zoom_Panel, THE Zoom_Panel SHALL close.
2. WHEN the Zoom_Panel closes, THE system SHALL restore the Code_Panel and Preview_Panel to their original sizes and positions (as saved when the Zoom_Panel was opened).
3. WHEN the user presses the Escape key while the Zoom_Panel is focused, THE Zoom_Panel SHALL close and restore the layout.
4. THE system SHALL allow only one Zoom_Panel to be open at a time. If the user clicks zoom-in on a different diagram while a Zoom_Panel is already open, THE system SHALL replace the current Zoom_Panel content with the new diagram.

### Requirement 6: Mouse Wheel Zoom in Zoom Panel

**User Story:** As a user, I want to use the mouse wheel to zoom the diagram in the zoom panel, so that I can zoom quickly without clicking buttons.

#### Acceptance Criteria

1. WHILE the Zoom_Panel is open, WHEN the user scrolls the mouse wheel up over the diagram, THE Zoom_Panel SHALL zoom in.
2. WHILE the Zoom_Panel is open, WHEN the user scrolls the mouse wheel down over the diagram, THE Zoom_Panel SHALL zoom out.
3. THE mouse wheel zoom SHALL respect the same min/max zoom limits as the button controls (25%–500%).

### Requirement 7: Theme Consistency

**User Story:** As a user, I want the hover toolbar and zoom panel to match the current application theme, so that the experience is visually consistent.

#### Acceptance Criteria

1. THE Hover_Toolbar SHALL use icon and background colors consistent with the current theme (dark or light) of the Preview_Panel.
2. THE Zoom_Panel SHALL use background and control colors consistent with the current application theme.
3. THE Zoom_Panel SHALL display the Diagram_SVG without altering the diagram's rendered theme or colors.
