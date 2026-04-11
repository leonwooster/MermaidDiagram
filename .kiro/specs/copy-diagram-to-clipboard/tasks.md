# Implementation Plan: Copy Diagram to Clipboard

## Overview

Adds a "Copy as Image" action that captures the WebView2 preview as PNG and places it on the Windows clipboard. Three entry points (Edit menu, Ctrl+Shift+C, floating button), with status bar feedback and graceful error handling. Follows the existing partial-class and DI-registered service patterns.

## Tasks

- [x] 1. Create IClipboardService interface and ClipboardService implementation
  - [x] 1.1 Create `MermaidDiagramApp/Services/IClipboardService.cs` with `Task<bool> CopyPngToClipboardAsync(byte[] pngData)` method
    - _Requirements: 5.1, 5.2, 5.3_
  - [x] 1.2 Create `MermaidDiagramApp/Services/ClipboardService.cs` implementing `IClipboardService`
    - Create `DataPackage`, write PNG bytes to `InMemoryRandomAccessStream`, call `SetBitmap`, `Clipboard.SetContent`, and `Clipboard.Flush`
    - Return false for null/empty input without touching clipboard
    - Catch exceptions, log via `ILogger`, return false
    - _Requirements: 5.1, 5.2, 5.3, 9.2_
  - [x] 1.3 Write property test: ClipboardService rejects empty input (Property 3)
    - **Property 3: ClipboardService rejects empty input**
    - **Validates: Requirements 5.1**
    - Create `MermaidDiagramApp.Tests/Services/ClipboardServicePropertyTests.cs`
    - For any null or zero-length byte array, `CopyPngToClipboardAsync` returns false without attempting clipboard operations
  - [x] 1.4 Write unit tests for ClipboardService edge cases
    - Create `MermaidDiagramApp.Tests/Services/ClipboardServiceTests.cs`
    - Test constructor throws `ArgumentNullException` for null logger
    - Test null input returns false
    - Test empty array returns false
    - _Requirements: 5.1_

- [x] 2. Register ClipboardService in DI and wire into MainWindow
  - [x] 2.1 Register `IClipboardService` / `ClipboardService` as singleton in `App.xaml.cs` `ConfigureServices()`
    - Add registration alongside existing `IExportService` registration
    - _Requirements: 5.1_
  - [x] 2.2 Add `IClipboardService` parameter to `MainWindow` constructor and store as `_clipboardService` field
    - Update `App.xaml.cs` `OnLaunched` to resolve and pass `IClipboardService`
    - _Requirements: 5.1_

- [x] 3. Checkpoint — Ensure project builds
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Create MainWindow.Clipboard.cs orchestration partial class
  - [x] 4.1 Create `MermaidDiagramApp/MainWindow.Clipboard.cs` with `CopyAsImage_Click` handler and `ExecuteCopyAsImageAsync` orchestration method
    - Guard: if `PreviewBrowser?.CoreWebView2 == null`, show "Preview is not ready yet" and return
    - Guard: if `CodeEditor.Text` is empty/whitespace, show "Nothing to copy — the preview is empty" and return
    - Primary capture via `CapturePreviewAsync` with fallback to `getSvg()` + `RasterizeSvgToPngAsync`
    - Call `_clipboardService.CopyPngToClipboardAsync` and show success/failure status
    - Catch all exceptions, log error, show "Failed to copy image to clipboard"
    - _Requirements: 4.1, 4.2, 4.3, 7.1, 7.2, 8.1, 8.2, 9.1, 9.2, 9.3_
  - [x] 4.2 Implement `ShowClipboardStatus` method with 3-second auto-dismiss timer
    - Save previous `RenderModeText.Text`, set new message, start `DispatcherTimer` that restores previous text after 3 seconds
    - _Requirements: 6.1, 6.2_
  - [x] 4.3 Write property test: Empty or whitespace editor content prevents clipboard modification (Property 4)
    - **Property 4: Empty or whitespace editor content prevents clipboard modification**
    - **Validates: Requirements 7.1, 7.2**
    - Create `MermaidDiagramApp.Tests/Services/CopyAsImageOrchestrationPropertyTests.cs`
    - For any whitespace-only string, orchestration shows "Nothing to copy" and does not invoke clipboard service
  - [x] 4.4 Write property test: Exceptions during copy produce failure status and are logged (Property 5)
    - **Property 5: Exceptions during copy produce failure status and are logged**
    - **Validates: Requirements 9.1, 9.2, 9.3**
    - For any exception thrown during capture/clipboard, orchestration catches it, shows "Failed to copy image to clipboard", and logs the error
  - [x] 4.5 Write property test: Successful copy produces success status message (Property 6)
    - **Property 6: Successful copy produces success status message**
    - **Validates: Requirements 6.1**
    - For any successful clipboard operation, status bar displays "Image copied to clipboard"
  - [x] 4.6 Write property test: Successful primary capture skips SVG fallback (Property 2)
    - **Property 2: Successful primary capture skips SVG fallback**
    - **Validates: Requirements 4.2**
    - For any non-empty PNG returned by primary capture, SVG fallback is not invoked

- [x] 5. Add XAML changes — Edit menu item and floating copy button
  - [x] 5.1 Add "Copy as Image" `MenuFlyoutItem` to the Edit menu in `MainWindow.xaml`
    - Place after "Find..." and before the separator / "Check & Fix Mermaid Syntax"
    - Include `KeyboardAccelerator` for `Ctrl+Shift+C`
    - Wire `Click="CopyAsImage_Click"`
    - _Requirements: 1.1, 1.2, 1.3_
  - [x] 5.2 Add floating copy button in the preview pane Grid in `MainWindow.xaml`
    - Use Segoe MDL2 clipboard glyph `&#xE8C8;`, positioned below `FloatingRefreshButton` at `Margin="16,128,16,16"`
    - Set tooltip "Copy as Image (Ctrl+Shift+C)"
    - Wire `Click="CopyAsImage_Click"`
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 6. Register Ctrl+Shift+C keyboard shortcut
  - [x] 6.1 Register `Ctrl+Shift+C` shortcut in `KeyboardShortcutManager` during MainWindow initialization (in `MainWindow.UI.cs` where other shortcuts are registered)
    - Map to `() => CopyAsImage_Click(this, new RoutedEventArgs())`
    - Ensure it works via both direct key handling and WebView2 key forwarding
    - _Requirements: 2.1, 2.2_
  - [x] 6.2 Write property test: Keyboard shortcut dispatch invokes registered action (Property 1)
    - **Property 1: Keyboard shortcut dispatch invokes registered action**
    - **Validates: Requirements 2.1, 2.2**
    - Extend or add to `MermaidDiagramApp.Tests/Services/KeyboardShortcutManagerTests.cs`
    - For any registered shortcut, `HandleKeyDown` / `HandleWebViewKeyEvent` executes the action and returns true

- [x] 7. Checkpoint — Ensure all tests pass and full build succeeds
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The design uses C# throughout, so no language selection was needed
