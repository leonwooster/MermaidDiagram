# Search Feature Documentation

## Overview
The search feature allows users to find text within the code editor (markup panel) with automatic highlighting and navigation. The feature uses TextControlBox's built-in search APIs for optimal performance and user experience.

## Features Implemented

### 1. Search Panel UI
- **Location**: Appears at the top of the code editor when activated
- **Components**:
  - Search text box with placeholder "Search in code..."
  - Find Previous button (▲) - navigates to previous match
  - Find Next button (▼) - navigates to next match
  - Close button (✕) - closes the search panel

### 2. Keyboard Shortcuts
- **Ctrl+F**: Open search panel
- **Enter**: Find next match
- **Shift+Enter**: Find previous match
- **Escape**: Close search panel

### 3. Search Functionality
- **Case-insensitive search**: Finds all occurrences regardless of case
- **Automatic highlighting**: Matched text is highlighted in the editor
- **Auto-scroll**: Editor automatically scrolls to show the current match
- **Real-time search**: Search begins as you type
- **Circular navigation**: Wraps around from last to first result and vice versa

## How It Works

### TextControlBox Built-in Search
The implementation uses TextControlBox's native search APIs:
- `BeginSearch(word, wholeWord, matchCase)` - Starts a search session
- `FindNext()` - Finds and navigates to next match (with highlighting)
- `FindPrevious()` - Finds and navigates to previous match (with highlighting)
- `EndSearch()` - Ends the search session
- `SearchIsOpen` - Property to check if search is active

### Search Flow
1. User opens search with Ctrl+F
2. User types search term
3. Search automatically begins and finds first match
4. Editor highlights the match and scrolls to show it
5. User navigates with Enter/Shift+Enter or buttons
6. Each match is highlighted and brought into view
7. User closes search with Escape

## What Works

✅ **Text highlighting**: Matched text is visually highlighted
✅ **Auto-scroll**: Editor scrolls to show current match
✅ **Navigation**: Jump between matches with keyboard or buttons
✅ **Real-time search**: Search updates as you type
✅ **Case-insensitive**: Finds matches regardless of case
✅ **Circular navigation**: Wraps from last to first match

## Current Limitations

### Synchronized Scrolling
The synchronized scrolling feature (clicking in code scrolls preview, and vice versa) is **not implemented**. This would require:
- Cursor position change events
- Line number to preview element mapping
- Bidirectional communication between editor and preview

This is a separate feature beyond the scope of text search.

## Usage

### Opening Search
1. Click **Edit > Find...** in the menu bar, or
2. Press **Ctrl+F**

### Searching
1. Type your search term in the search box
2. First match is automatically found and highlighted
3. Press **Enter** or click **▼** to find next match
4. Press **Shift+Enter** or click **▲** to find previous match
5. Editor automatically scrolls to show each match

### Closing Search
1. Click the **✕** button, or
2. Press **Escape**

## Implementation Details

### Files Modified
- `MermaidDiagramApp/MainWindow.xaml` - Added search panel UI with TextChanged event
- `MermaidDiagramApp/MainWindow.xaml.cs` - Added search logic using TextControlBox APIs

### Code Structure
```csharp
// Search state
private string _lastSearchText = string.Empty;

// Start search when text changes
private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
{
    if (searchText != _lastSearchText)
    {
        CodeEditor.EndSearch();
        CodeEditor.BeginSearch(searchText, wholeWord: false, matchCase: false);
        CodeEditor.FindNext(); // Highlights and scrolls automatically
    }
}

// Navigate between matches
private void PerformSearch(bool forward)
{
    if (!CodeEditor.SearchIsOpen)
    {
        CodeEditor.BeginSearch(searchText, wholeWord: false, matchCase: false);
    }
    
    if (forward)
        CodeEditor.FindNext();
    else
        CodeEditor.FindPrevious();
    // TextControlBox handles highlighting and scrolling
}
```

## Future Enhancements

Possible improvements:
1. **Match counter**: Show "X of Y" results (requires API support)
2. **Replace functionality**: Add "Replace" and "Replace All" buttons
3. **Regex support**: Allow regular expression searches
4. **Match case option**: Toggle case-sensitive search
5. **Whole word option**: Match whole words only
6. **Search history**: Remember recent searches
7. **Synchronized scrolling**: Implement bidirectional code-preview sync (separate feature)

## Related Features

### Recent Files Feature
See `docs/RECENT_FILES_FEATURE.md` for the recently implemented file tracking feature.

### Subgraph Title Fix
See `docs/SUBGRAPH_TITLE_AUTO_FIX.md` for the Mermaid text wrapping fix.

## Technical Notes

### Why This Works Now
The initial implementation tried to manually track match positions but couldn't:
- Set cursor position (no SelectionStart property)
- Select text (no SelectionLength property)
- Scroll to position (no ScrollToCaret method)

The fixed implementation uses TextControlBox's built-in search, which:
- Internally manages match positions
- Automatically highlights matches
- Automatically scrolls to show matches
- Provides a clean API for navigation

### Performance
- Search is fast even for large files
- No manual string parsing needed
- TextControlBox handles all the heavy lifting
- Minimal memory overhead

## Conclusion

The search feature is **fully functional** and provides an excellent user experience with automatic highlighting, scrolling, and navigation. It leverages TextControlBox's built-in capabilities for optimal performance and reliability.
