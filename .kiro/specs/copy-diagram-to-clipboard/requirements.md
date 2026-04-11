# Requirements Document

## Introduction

This feature adds a one-click "Copy as Image" action to the Mermaid Diagram Editor that captures the current diagram or Markdown preview as a PNG image and places it on the Windows clipboard. Users can then paste the image directly into Slack, Teams, emails, or any application that accepts pasted images, without going through a file-save workflow. The action is accessible from the Edit menu, a keyboard shortcut (Ctrl+Shift+C), and a floating toolbar button in the preview pane.

## Glossary

- **Preview_Pane**: The right-side panel containing the WebView2 control that renders Mermaid diagrams and Markdown content.
- **Clipboard_Service**: The application component responsible for placing PNG image data onto the Windows clipboard using the WinRT `DataPackage` API.
- **Copy_As_Image_Action**: The user-initiated action that captures the current Preview_Pane content as a PNG image and copies it to the Windows clipboard.
- **Status_Bar**: The bottom bar of the application window that displays the current render mode and contextual messages.
- **Confirmation_Message**: A brief, auto-dismissing text notification shown in the Status_Bar after a clipboard operation completes.
- **WebView2_Control**: The Microsoft Edge WebView2 control (`PreviewBrowser`) used to render Mermaid diagrams and Markdown content.
- **Keyboard_Shortcut_Manager**: The `KeyboardShortcutManager` service that registers and dispatches keyboard shortcuts to their associated actions.

## Requirements

### Requirement 1: Copy As Image Menu Entry

**User Story:** As a user, I want a "Copy as Image" option in the Edit menu, so that I can copy the current preview to the clipboard using the menu bar.

#### Acceptance Criteria

1. THE Edit menu SHALL contain a "Copy as Image" menu item placed after the "Find..." item and before the "Check & Fix Mermaid Syntax" item.
2. WHEN the user clicks the "Copy as Image" menu item, THE application SHALL execute the Copy_As_Image_Action.
3. THE "Copy as Image" menu item SHALL display the keyboard accelerator hint "Ctrl+Shift+C".

### Requirement 2: Keyboard Shortcut for Copy As Image

**User Story:** As a user, I want to press Ctrl+Shift+C to copy the diagram as an image, so that I can quickly copy without navigating menus.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+Shift+C while the application has focus, THE Keyboard_Shortcut_Manager SHALL execute the Copy_As_Image_Action.
2. WHEN the user presses Ctrl+Shift+C while the WebView2_Control has focus, THE Keyboard_Shortcut_Manager SHALL execute the Copy_As_Image_Action via the WebView2 key forwarding mechanism.

### Requirement 3: Floating Copy Button in Preview Pane

**User Story:** As a user, I want a copy button in the preview pane, so that I can copy the diagram with a single click while viewing it.

#### Acceptance Criteria

1. THE Preview_Pane SHALL display a floating copy button positioned near the existing floating refresh button.
2. THE floating copy button SHALL use a recognizable copy icon (clipboard glyph) and display a "Copy as Image" tooltip.
3. WHEN the user clicks the floating copy button, THE application SHALL execute the Copy_As_Image_Action.

### Requirement 4: Capture Preview as PNG

**User Story:** As a user, I want the copy action to capture the full rendered preview as a PNG image, so that the pasted image matches what I see in the preview pane.

#### Acceptance Criteria

1. WHEN the Copy_As_Image_Action is executed, THE application SHALL capture the current WebView2_Control content as PNG image data using the `CoreWebView2.CapturePreviewAsync` method.
2. WHEN the primary capture method succeeds, THE application SHALL use the captured PNG data without additional rasterization.
3. WHEN the primary capture via `CapturePreviewAsync` fails, THE application SHALL fall back to retrieving the SVG content via `getSvg()` and rasterizing it to PNG using the existing `IDiagramExportService.RasterizeSvgToPngAsync` method.

### Requirement 5: Copy PNG to Windows Clipboard

**User Story:** As a user, I want the captured image placed on the Windows clipboard, so that I can paste it into any application.

#### Acceptance Criteria

1. WHEN PNG image data has been captured, THE Clipboard_Service SHALL create a `DataPackage` containing the PNG data as a bitmap stream.
2. WHEN the `DataPackage` is ready, THE Clipboard_Service SHALL set the Windows clipboard content using `Clipboard.SetContent`.
3. WHEN the clipboard content is set, THE Clipboard_Service SHALL call `Clipboard.Flush` so the data remains available after the application is closed.

### Requirement 6: Success Confirmation

**User Story:** As a user, I want visual feedback after copying, so that I know the image was successfully placed on the clipboard.

#### Acceptance Criteria

1. WHEN the Copy_As_Image_Action completes successfully, THE Status_Bar SHALL display a Confirmation_Message with the text "Image copied to clipboard".
2. THE Confirmation_Message SHALL auto-dismiss after 3 seconds and restore the previous Status_Bar content.

### Requirement 7: Empty Preview Handling

**User Story:** As a user, I want a clear message when I try to copy an empty preview, so that I understand why the copy did not work.

#### Acceptance Criteria

1. WHEN the Copy_As_Image_Action is executed and the Preview_Pane has no rendered content (empty editor text), THE application SHALL display a Confirmation_Message in the Status_Bar with the text "Nothing to copy — the preview is empty".
2. WHEN the Preview_Pane has no rendered content, THE application SHALL not modify the clipboard contents.

### Requirement 8: WebView2 Not Ready Handling

**User Story:** As a user, I want the copy action to handle the case where the preview engine is still loading, so that the application does not crash.

#### Acceptance Criteria

1. WHEN the Copy_As_Image_Action is executed and the WebView2_Control CoreWebView2 property is null, THE application SHALL display a Confirmation_Message in the Status_Bar with the text "Preview is not ready yet".
2. WHEN the WebView2_Control is not ready, THE application SHALL not attempt to capture or modify the clipboard.

### Requirement 9: Clipboard Failure Handling

**User Story:** As a user, I want to know if the copy failed, so that I can retry or use an alternative method.

#### Acceptance Criteria

1. IF an exception occurs during PNG capture or clipboard operations, THEN THE application SHALL display a Confirmation_Message in the Status_Bar with the text "Failed to copy image to clipboard".
2. IF an exception occurs during the Copy_As_Image_Action, THEN THE application SHALL log the error details including the exception message using the application logger.
3. IF an exception occurs during the Copy_As_Image_Action, THEN THE application SHALL not crash or leave the UI in an inconsistent state.
