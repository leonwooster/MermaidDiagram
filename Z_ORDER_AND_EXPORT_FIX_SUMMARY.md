# Z-Order and Export Fixes Summary

## Date: January 28, 2026

## Issues Fixed

### 1. Z-Order Problem (Lines Appearing Above Text Labels)

**Problem**: In exported Word documents and preview, connector lines were rendering on top of text labels, making them hard to read.

**Root Cause**: SVG elements render in DOM order. Edge labels appeared before edges in the DOM, so connector lines drew on top of text.

**Solution Implemented**:
Enhanced the `fixSvgZOrder` function in `MermaidDiagramApp/Assets/UnifiedRenderer.html` (lines 645-830) with:

1. **Improved Logging**: Added comprehensive logging to track SVG structure and element reordering
2. **Better Element Detection**: Enhanced selectors to find edge labels, node labels, and nodes
3. **DOM Reordering**: Moves text labels and nodes to the end of their parent elements so they render on top
4. **Alternative Approach**: If standard selectors don't find elements, falls back to finding all text elements and moving their parent groups
5. **Background Rectangles**: Adds solid background rectangles to edge labels for better visibility

**Key Changes**:
- Added detailed logging with `===` markers for easy identification in logs
- Logs all groups in SVG with their classes and IDs for debugging
- Attempts multiple strategies to find and reorder elements
- Moves elements to the end of their parent to ensure they render last (on top)

**Testing**:
To verify the fix is working, check the logs for these messages:
- `=== Fixing SVG z-order and edge label backgrounds ===`
- `Found X edge labels`
- `Found X node labels`
- `Found X nodes`
- `Successfully moved X elements to top`
- `=== SVG processing completed ===`

**Files Modified**:
- `MermaidDiagramApp/Assets/UnifiedRenderer.html` (lines 645-830)

---

### 2. Export Menu Disabled for Pasted Content

**Problem**: When users pasted content into the editor and clicked refresh, the "Export to Word" menu item remained greyed out because the export ViewModel wasn't updated.

**Root Cause**: The `UpdatePreview()` method only updated the preview rendering but didn't notify the export ViewModel about the content change. The export ViewModel's `CanExport` property requires content to be set.

**Solution Implemented**:
Modified `UpdatePreview()` method in `MermaidDiagramApp/MainWindow.xaml.cs` to update the export ViewModel whenever content is rendered:

1. **Markdown Content**: Directly passes the content to the export ViewModel
2. **Mermaid Content**: Wraps the Mermaid diagram in Markdown format with a code fence for proper export
3. **Logging**: Adds debug logging to track when export ViewModel is updated

**Key Changes**:
```csharp
// Update export ViewModel with current content so export menu is enabled
if (_markdownToWordViewModel != null)
{
    if (_currentContentType == ContentType.Markdown || _currentContentType == ContentType.MarkdownWithMermaid)
    {
        _markdownToWordViewModel.UpdateMarkdownContent(code);
        _logger.LogDebug("Updated export ViewModel with Markdown content");
    }
    else if (_currentContentType == ContentType.Mermaid)
    {
        // Wrap Mermaid diagram in Markdown format for export
        var wrappedContent = $"# Mermaid Diagram\n\n```mermaid\n{code}\n```";
        _markdownToWordViewModel.UpdateMarkdownContent(wrappedContent);
        _logger.LogDebug("Updated export ViewModel with wrapped Mermaid content");
    }
}
```

**Result**: Users can now paste content, click refresh, and immediately export to Word without saving the file first.

**Files Modified**:
- `MermaidDiagramApp/MainWindow.xaml.cs` (lines 888-903)

---

## Build Status

✅ Application rebuilt successfully with `dotnet build --arch x64`
- Build completed in 7.0 seconds
- 4 warnings (pre-existing, not related to these changes)
- Output: `MermaidDiagramApp/bin/Debug/net8.0-windows10.0.19041.0/win-x64/`

---

## Testing Instructions

### Test Z-Order Fix:
1. Restart the application completely
2. Create or open a diagram with connector lines and text labels
3. Press `Ctrl+Shift+D` in the preview to enable debug logging
4. Refresh the preview
5. Check the debug panel for z-order fix messages
6. Export to Word and verify text labels appear above lines

### Test Export for Pasted Content:
1. Copy Markdown or Mermaid content from any source
2. Paste into the left editor panel
3. Click the Refresh button (or press F5)
4. Check that "Export to Word" menu item is now enabled
5. Click "Export to Word" and verify the export works

---

## Previous Related Fixes

This builds on the previous white box fix that disabled `htmlLabels` to prevent `<foreignObject>` elements in SVG output.

---

## Notes

- The z-order fix applies to both preview and export rendering
- The export ViewModel update happens automatically on every preview refresh
- Pasted content can be exported without saving to a file first
- The temporary file path is used internally when no file is loaded
