# Requirements Document

## Introduction

This feature adds the ability to click on a rendered Mermaid diagram in the preview panel to open it in an enlarged popup/overlay dialog. Complex diagrams (e.g., quadrant charts with overlapping labels) are often difficult to read at the default preview size. The popup provides zoom controls so users can inspect diagram details at a comfortable scale.

The feature applies to both pure Mermaid rendering mode and embedded Mermaid diagrams within Markdown content.

## Glossary

- **Preview_Panel**: The WebView2-based panel (PreviewBrowser) that renders Mermaid diagrams and Markdown content using UnifiedRenderer.html.
- **Diagram_SVG**: The rendered SVG element produced by Mermaid.js inside the Preview_Panel.
- **Enlarge_Overlay**: A full-screen or near-full-screen popup/dialog displayed on top of the Preview_Panel that shows an enlarged copy of a clicked Diagram_SVG with zoom controls.
- **Zoom_Controls**: UI elements within the Enlarge_Overlay that allow the user to zoom in, zoom out, reset zoom, and pan the diagram.
- **Content_Container**: The HTML element (`#content-container`) in UnifiedRenderer.html that holds rendered content.
- **WebView_Message**: A JSON message sent between the WebView2 JavaScript layer and the C# host via `postMessage` / `WebMessageReceived`.

## Requirements

### Requirement 1: Click Detection on Diagrams

**User Story:** As a user, I want to click on a rendered Mermaid diagram in the preview panel, so that I can open it in a larger view for detailed inspection.

#### Acceptance Criteria

1. WHEN the user clicks on a Diagram_SVG in the Preview_Panel, THE Enlarge_Overlay SHALL open and display the clicked Diagram_SVG.
2. WHEN the Preview_Panel is in mermaid-mode (pure Mermaid rendering), THE Preview_Panel SHALL treat the entire rendered SVG as a clickable diagram.
3. WHEN the Preview_Panel is in markdown-mode with embedded Mermaid diagrams, THE Preview_Panel SHALL treat each individual embedded Mermaid SVG as a separately clickable diagram.
4. THE Preview_Panel SHALL display a visual cursor change (pointer cursor) when the user hovers over a clickable Diagram_SVG.

### Requirement 2: Enlarge Overlay Display

**User Story:** As a user, I want the enlarged diagram popup to fill most of the screen, so that I have maximum space to view complex diagrams.

#### Acceptance Criteria

1. WHEN the Enlarge_Overlay opens, THE Enlarge_Overlay SHALL display the Diagram_SVG centered within a semi-transparent backdrop that covers the full viewport of the Preview_Panel.
2. THE Enlarge_Overlay SHALL render the Diagram_SVG at a size that fits within the overlay viewport while preserving the original aspect ratio.
3. THE Enlarge_Overlay SHALL include a visible close button in the top-right corner of the overlay.
4. WHEN the Enlarge_Overlay is open, THE Enlarge_Overlay SHALL prevent interaction with the underlying Preview_Panel content.

### Requirement 3: Zoom Controls in Overlay

**User Story:** As a user, I want to zoom in and out of the diagram in the popup, so that I can read small labels and inspect fine details.

#### Acceptance Criteria

1. THE Enlarge_Overlay SHALL provide a zoom-in button that increases the diagram scale by a fixed increment.
2. THE Enlarge_Overlay SHALL provide a zoom-out button that decreases the diagram scale by a fixed increment.
3. THE Enlarge_Overlay SHALL provide a reset-zoom button that restores the diagram to the fit-to-viewport scale.
4. THE Enlarge_Overlay SHALL display the current zoom level as a percentage.
5. WHEN the diagram is zoomed beyond the overlay viewport, THE Enlarge_Overlay SHALL allow the user to scroll or pan to view all parts of the diagram.

### Requirement 4: Closing the Overlay

**User Story:** As a user, I want multiple ways to close the enlarged diagram popup, so that I can quickly return to editing.

#### Acceptance Criteria

1. WHEN the user clicks the close button, THE Enlarge_Overlay SHALL close and return focus to the Preview_Panel.
2. WHEN the user presses the Escape key while the Enlarge_Overlay is open, THE Enlarge_Overlay SHALL close.
3. WHEN the user clicks on the semi-transparent backdrop area outside the diagram, THE Enlarge_Overlay SHALL close.

### Requirement 5: Mouse Wheel Zoom

**User Story:** As a user, I want to use the mouse wheel to zoom the diagram in the popup, so that I can zoom quickly without clicking buttons.

#### Acceptance Criteria

1. WHILE the Enlarge_Overlay is open, WHEN the user scrolls the mouse wheel up, THE Enlarge_Overlay SHALL zoom in on the diagram.
2. WHILE the Enlarge_Overlay is open, WHEN the user scrolls the mouse wheel down, THE Enlarge_Overlay SHALL zoom out on the diagram.
3. THE Enlarge_Overlay SHALL clamp the zoom level to a minimum of 10% and a maximum of 500%.

### Requirement 6: Theme Consistency

**User Story:** As a user, I want the enlarged diagram popup to match the current application theme, so that the viewing experience is visually consistent.

#### Acceptance Criteria

1. THE Enlarge_Overlay SHALL use a backdrop and control colors that are consistent with the current theme (dark or light) of the Preview_Panel.
2. THE Enlarge_Overlay SHALL display the Diagram_SVG without altering the diagram's rendered theme or colors.
