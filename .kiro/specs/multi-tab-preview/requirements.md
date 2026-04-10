# Requirements Document

## Introduction

This feature adds multi-tab support to the Preview pane of the Mermaid Diagram Editor. Currently the application handles one file at a time using a single `_currentFilePath`. The multi-tab design allows users to open multiple files simultaneously, each in its own tab within the Preview pane. Switching tabs updates the Code pane to reflect the selected tab's markup. The feature also corrects the default pane width ratio, fixes the close-file dialog to only prompt on unsaved changes, and resets scroll positions when opening new files.

## Glossary

- **Preview_Pane**: The right-side panel (Grid Column 6) containing the WebView2 control that renders Mermaid diagrams and Markdown content.
- **Code_Pane**: The left-side panel (Grid Column 4) containing the TextControlBox code editor.
- **Tab_Bar**: A horizontal strip of selectable tabs displayed above the Preview_Pane, one tab per open file.
- **Tab**: A single item in the Tab_Bar representing one open file, displaying the file name and a close button.
- **Active_Tab**: The currently selected Tab whose content is displayed in the Preview_Pane and Code_Pane.
- **Tab_State**: The in-memory data associated with a Tab, including file path, editor content, content type, dirty flag, and scroll positions.
- **Dirty_State**: A boolean flag on each Tab_State indicating the editor content has been modified since the last save.
- **GridSplitter**: The CommunityToolkit GridSplitter control between the Code_Pane and Preview_Pane columns.
- **Zoom_Panel**: The ZoomPanelService-driven panel that shows an enlarged view of the active diagram.
- **Scroll_Position**: The vertical and horizontal scroll offsets of the WebView2 preview content.

## Requirements

### Requirement 1: Default Pane Width Ratio

**User Story:** As a user, I want the Preview pane to occupy 70% of the available width and the Code pane to occupy 30% when the application starts, so that I have a larger area to view rendered diagrams.

#### Acceptance Criteria

1. WHEN the application starts, THE Code_Pane column width SHALL be set to 3* (30% proportion) and THE Preview_Pane column width SHALL be set to 7* (70% proportion).
2. WHEN the application starts, THE GridSplitter between Code_Pane and Preview_Pane SHALL remain draggable to allow the user to resize the panes after launch.
3. WHEN the Zoom_Panel or Builder panels are not visible, THE 30/70 ratio SHALL apply to the Code_Pane and Preview_Pane columns only.

### Requirement 2: Multi-Tab Support in Preview Pane

**User Story:** As a user, I want to open multiple files in separate tabs within the Preview pane, so that I can work on several diagrams or documents without closing and reopening files.

#### Acceptance Criteria

1. WHEN the user opens a new file, THE Tab_Bar SHALL create a new Tab displaying the file name.
2. WHEN the user selects a different Tab, THE Code_Pane SHALL display the editor content associated with the selected Tab_State.
3. WHEN the user selects a different Tab, THE Preview_Pane SHALL render the content associated with the selected Tab_State.
4. WHEN the user modifies the editor content for the Active_Tab, THE Tab_State Dirty_State flag SHALL be set to true.
5. WHEN a Tab has Dirty_State equal to true, THE Tab SHALL display a visual indicator (such as a dot or asterisk) next to the file name.
6. WHEN the user saves the Active_Tab content, THE Tab_State Dirty_State flag SHALL be set to false and the visual indicator SHALL be removed.
7. THE Tab_Bar SHALL store each Tab_State in memory using a data structure that avoids duplicating the WebView2 control across tabs.
8. WHEN the user switches tabs, THE application SHALL save the current Tab_State editor content before loading the new Tab_State content into the Code_Pane.

### Requirement 3: Tab Close with Conditional Save Prompt

**User Story:** As a user, I want the application to prompt me to save only when I have unsaved changes, so that I am not interrupted by unnecessary save dialogs when closing a clean file.

#### Acceptance Criteria

1. WHEN the user closes a Tab with Dirty_State equal to true, THE application SHALL display a save confirmation dialog with Save, Discard, and Cancel options.
2. WHEN the user closes a Tab with Dirty_State equal to false, THE application SHALL close the Tab without displaying a save confirmation dialog.
3. WHEN the user selects Save in the save confirmation dialog, THE application SHALL save the file and then close the Tab.
4. WHEN the user selects Discard in the save confirmation dialog, THE application SHALL close the Tab without saving.
5. WHEN the user selects Cancel in the save confirmation dialog, THE application SHALL keep the Tab open and return focus to the Tab.
6. WHEN the last remaining Tab is closed, THE application SHALL clear the Code_Pane and Preview_Pane and display an empty state.

### Requirement 4: Tab Close Buttons

**User Story:** As a user, I want each tab to have a close button, so that I can close individual files directly from the tab without using the File menu.

#### Acceptance Criteria

1. THE Tab_Bar SHALL display a close button (X icon) on each Tab.
2. WHEN the user clicks the close button on a Tab, THE application SHALL initiate the tab close flow as defined in Requirement 3.
3. WHEN the user hovers over the close button, THE close button SHALL display a visual hover state to indicate interactivity.

### Requirement 5: Scroll Position Reset on New File

**User Story:** As a user, I want the preview scroll position to reset to the top-left when I open a new file, so that I always start viewing a new document from the beginning.

#### Acceptance Criteria

1. WHEN the user opens a new file into a new Tab, THE Preview_Pane SHALL reset the vertical scroll position to zero.
2. WHEN the user opens a new file into a new Tab, THE Preview_Pane SHALL reset the horizontal scroll position to zero.
3. WHEN the user switches to an existing Tab, THE Preview_Pane SHALL restore the saved Scroll_Position from that Tab_State.

### Requirement 6: Zoom Panel Compatibility

**User Story:** As a user, I want the Zoom panel to continue working with the multi-tab design, so that I can zoom into diagrams from whichever tab is active.

#### Acceptance Criteria

1. WHILE the Zoom_Panel is open, WHEN the user switches to a different Tab, THE Zoom_Panel SHALL update to display the diagram content from the newly Active_Tab.
2. THE Zoom_Panel zoom level, open/close behavior, and controls SHALL remain unchanged from the current implementation.

### Requirement 7: Memory Efficiency for Multiple Tabs

**User Story:** As a user, I want the application to handle multiple open tabs without excessive memory consumption, so that performance remains acceptable with many files open.

#### Acceptance Criteria

1. THE application SHALL use a single WebView2 control instance shared across all tabs, re-rendering content when the Active_Tab changes.
2. THE Tab_State SHALL store only the text content, file path, content type, Dirty_State flag, and Scroll_Position for each tab, avoiding duplication of rendering resources.
3. WHEN a Tab is closed, THE application SHALL release the associated Tab_State from memory.
