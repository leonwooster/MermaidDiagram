# Implementation Plan: Multi-Tab Preview

## Overview

Implement multi-tab support in the Preview pane of the Mermaid Diagram Editor. The plan introduces a `TabState` model and `ITabService`/`TabService` to manage open tabs, adds a `TabView` control above the WebView2 preview, creates a new `MainWindow.Tabs.cs` partial class for tab UI logic, integrates tab management into existing file operations, save, close, timer, and zoom panel flows, and adjusts the default pane width ratio to 30/70.

## Tasks

- [x] 1. Create TabState model and TabChangedEventArgs
  - [x] 1.1 Create `MermaidDiagramApp/Models/TabState.cs` with properties: `Id` (Guid), `FilePath`, `FileName` (derived), `EditorContent`, `ContentType`, `IsDirty`, `ScrollTop`, `ScrollLeft`
    - Use the existing `ContentType` enum from `MermaidDiagramApp/Models/ContentType.cs`
    - `FileName` returns `Path.GetFileName(FilePath)` or `"Untitled"` when path is empty
    - `Id` initialized via `Guid.NewGuid()` in the property initializer
    - _Requirements: 2.7, 7.2_

  - [x] 1.2 Create `MermaidDiagramApp/Models/TabChangedEventArgs.cs` with `Tab` and `PreviousTab` properties
    - Extends `EventArgs`
    - Both properties are nullable `TabState?`
    - _Requirements: 2.2, 2.3_

- [x] 2. Create ITabService interface and TabService implementation
  - [x] 2.1 Create `MermaidDiagramApp/Services/ITabService.cs` defining the interface
    - Properties: `Tabs` (IReadOnlyList), `ActiveTab` (TabState?)
    - Methods: `AddTab`, `RemoveTab`, `SetActiveTab`, `FindTabByFilePath`, `UpdateTabContent`, `MarkDirty`, `UpdateScrollPosition`
    - Events: `ActiveTabChanged`, `TabClosed`, `TabAdded`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.6, 2.7, 2.8, 5.1, 5.2, 5.3, 7.3_

  - [x] 2.2 Create `MermaidDiagramApp/Services/TabService.cs` implementing `ITabService`
    - `AddTab` appends to internal `List<TabState>`, fires `TabAdded`, initializes scroll to (0, 0)
    - `RemoveTab` removes from list, fires `TabClosed`, selects adjacent tab if active was removed
    - `SetActiveTab` fires `ActiveTabChanged` with previous and new tab
    - `FindTabByFilePath` returns existing tab or null (case-insensitive path comparison)
    - `UpdateTabContent` sets `EditorContent` on the target tab
    - `MarkDirty` sets `IsDirty` on the target tab
    - `UpdateScrollPosition` sets `ScrollTop`/`ScrollLeft` on the target tab
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.6, 2.7, 2.8, 3.6, 5.1, 5.2, 5.3, 7.3_

  - [x] 2.3 Write property test: AddTab creates correctly named tab and grows collection
    - **Property 1: AddTab creates a correctly named tab and grows the collection**
    - Create `MermaidDiagramApp.Tests/Services/TabServicePropertyTests.cs`
    - For any valid file path and content, `AddTab` increases tab count by 1 and `FileName` equals `Path.GetFileName(filePath)` or "Untitled"
    - **Validates: Requirements 2.1**

  - [x] 2.4 Write property test: Tab content round-trip across switches
    - **Property 2: Tab content round-trip across switches**
    - For any set of tabs with distinct content, switching away and back preserves `EditorContent` exactly
    - **Validates: Requirements 2.2, 2.3, 2.8**

  - [x] 2.5 Write property test: Dirty flag toggle
    - **Property 3: Dirty flag toggle**
    - `MarkDirty(tabId, true)` sets `IsDirty` to true; `MarkDirty(tabId, false)` sets it to false; always reflects most recent call
    - **Validates: Requirements 2.4, 2.6**

  - [x] 2.6 Write property test: Close behavior depends on dirty state
    - **Property 4: Close behavior depends on dirty state**
    - If `IsDirty` is true, close flow requires confirmation; if false, tab can be removed immediately
    - **Validates: Requirements 3.1, 3.2**

  - [x] 2.7 Write property test: New tabs initialize with zero scroll position
    - **Property 5: New tabs initialize with zero scroll position**
    - Any newly created tab via `AddTab` has `ScrollTop == 0` and `ScrollLeft == 0`
    - **Validates: Requirements 5.1, 5.2**

  - [x] 2.8 Write property test: Scroll position round-trip across switches
    - **Property 6: Scroll position round-trip across switches**
    - For any tab with saved scroll position, switching away and back restores exact `ScrollTop`/`ScrollLeft`
    - **Validates: Requirements 5.3**

  - [x] 2.9 Write property test: Tab removal removes exactly the target tab
    - **Property 7: Tab removal removes exactly the target tab**
    - `RemoveTab(tabId)` decreases count by 1 and the removed tab no longer appears in `Tabs`
    - **Validates: Requirements 7.3**

- [x] 3. Checkpoint - Ensure model and service tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Register TabService in DI container and wire to MainWindow
  - [x] 4.1 Register `ITabService` as singleton in `App.xaml.cs` `ConfigureServices`
    - Add `services.AddSingleton<ITabService, TabService>();`
    - _Requirements: 7.1_

  - [x] 4.2 Add `ITabService` parameter to `MainWindow` constructor and store as field
    - Update constructor signature in `MainWindow.xaml.cs`
    - Update `OnLaunched` in `App.xaml.cs` to resolve and pass `ITabService`
    - _Requirements: 2.7_

- [x] 5. Update XAML layout for tab bar and default pane widths
  - [x] 5.1 Change default column widths in `MainWindow.xaml` from `*`/`*` to `3*`/`7*` for `EditorColumn`/`PreviewColumn`
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 5.2 Add `TabView` control above `WebView2` in the Preview column (Grid.Column="6")
    - Wrap existing preview content in a two-row Grid (Auto for TabBar, * for preview content)
    - Set `IsAddTabButtonVisible="False"`, `CanDragTabs="False"`, `CanReorderTabs="False"`
    - Wire `TabCloseRequested` and `SelectionChanged` events
    - Each `TabViewItem` shows file name, dirty indicator (● prefix), and built-in close button
    - _Requirements: 2.1, 2.5, 4.1, 4.3_

- [x] 6. Create MainWindow.Tabs.cs partial class for tab UI logic
  - [x] 6.1 Create `MermaidDiagramApp/MainWindow.Tabs.cs` with tab event handlers
    - `PreviewTabView_SelectionChanged`: save outgoing tab state (editor content + scroll position), load incoming tab state into CodeEditor and trigger WebView2 re-render
    - `PreviewTabView_TabCloseRequested`: check `IsDirty`, show save/discard/cancel dialog if dirty, call `ITabService.RemoveTab`
    - `SyncTabBarFromService`: rebuild `TabViewItem` collection from `ITabService.Tabs`
    - `UpdateTabDirtyIndicator(Guid tabId)`: update header text with/without ● prefix
    - `SaveCurrentTabScrollPosition`: execute JS `window.scrollX`/`window.scrollY`, store in `TabState`
    - `RestoreTabScrollPosition`: execute JS `window.scrollTo(x, y)` after render completes
    - _Requirements: 2.1, 2.2, 2.3, 2.5, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.1, 4.2, 5.1, 5.2, 5.3_

  - [x] 6.2 Write unit tests for tab close dialog logic
    - Test: dirty tab shows save/discard/cancel dialog (Requirement 3.1)
    - Test: clean tab closes without dialog (Requirement 3.2)
    - Test: Save option saves file then closes tab (Requirement 3.3)
    - Test: Discard option closes tab without saving (Requirement 3.4)
    - Test: Cancel option keeps tab open (Requirement 3.5)
    - Test: closing last tab clears editor and preview (Requirement 3.6)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 7. Integrate tab management into file operations
  - [x] 7.1 Update `Open_Click` in `MainWindow.FileOps.cs` to use `ITabService`
    - After reading file content, call `FindTabByFilePath` — if found, switch to existing tab
    - Otherwise call `AddTab(path, content, contentType)` to create a new tab
    - Remove direct `_currentFilePath` and `CodeEditor.Text` assignments (delegate to tab switching)
    - _Requirements: 2.1, 5.1, 5.2, 7.1_

  - [x] 7.2 Update `OpenRecentFile` in `MainWindow.FileOps.cs` to use `ITabService`
    - Same dedup-or-add logic as `Open_Click`
    - Remove the unsaved-changes dialog that currently checks `!string.IsNullOrEmpty(CodeEditor.Text)` — each tab tracks its own dirty state
    - _Requirements: 2.1, 3.1, 3.2_

  - [x] 7.3 Update `Save_Click` to save the active tab's content and clear dirty flag
    - On successful save, call `ITabService.MarkDirty(activeTab.Id, false)`
    - Update `_currentFilePath` from `ActiveTab.FilePath`
    - _Requirements: 2.6_

  - [x] 7.4 Update `Close_Click` to close the active tab instead of clearing the entire document
    - Delegate to the tab close flow in `MainWindow.Tabs.cs`
    - _Requirements: 3.1, 3.2, 3.6_

- [x] 8. Integrate tab management into timer and content change flow
  - [x] 8.1 Update `Timer_Tick` in `MainWindow.WebView.cs` to sync with active tab
    - When `CodeEditor.Text` changes, call `ITabService.UpdateTabContent(activeTab.Id, newContent)` and `ITabService.MarkDirty(activeTab.Id, true)`
    - Update `UpdateTabDirtyIndicator` to reflect dirty state in the tab header
    - _Requirements: 2.4, 2.5_

- [x] 9. Integrate tab management with Zoom Panel
  - [x] 9.1 Update zoom panel wiring to respond to tab switches
    - When `ActiveTabChanged` fires while zoom panel is open, re-render the new tab's content in the zoom panel
    - If the new tab has no diagram content, close the zoom panel
    - _Requirements: 6.1, 6.2_

- [x] 10. Checkpoint - Ensure all tests pass and integration works
  - Ensure all tests pass, ask the user if questions arise.

  - [x] 10.1 Write unit tests for default column widths
    - Verify `EditorColumn` is `3*` and `PreviewColumn` is `7*` at startup
    - _Requirements: 1.1_

  - [x] 10.2 Write integration tests for tab switch + zoom panel update
    - Verify zoom panel updates content when switching tabs while open
    - _Requirements: 6.1_

  - [x] 10.3 Write unit tests for duplicate file open dedup
    - Verify opening an already-open file switches to the existing tab instead of creating a new one
    - _Requirements: 2.1, 7.1_

- [x] 11. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate the 7 correctness properties defined in the design document using FsCheck.Xunit
- Unit tests validate specific examples and edge cases
- The single WebView2 instance is shared across all tabs — only the active tab's content is rendered (Requirement 7.1)
