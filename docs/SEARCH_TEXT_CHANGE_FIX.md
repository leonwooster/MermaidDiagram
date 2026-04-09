# Search Text Change Fix

## Issue
When changing the search text in the search box, the search would continue using the previous search term instead of the new one. Users had to close and reopen the search panel to search for a different term.

## Root Cause
The code was checking if a search was already open (`CodeEditor.SearchIsOpen`) and reusing it, but it wasn't detecting when the search text had changed. This meant:
1. User searches for "graph" → BeginSearch("graph") called
2. User changes text to "node" → Search still active for "graph"
3. User presses Enter → FindNext() continues finding "graph" instead of "node"

## Solution
Added tracking of the current search text and logic to restart the search when it changes:

### 1. Track Current Search Text
```csharp
private string _currentSearchText = string.Empty;
```

### 2. End Search When Text Changes
```csharp
private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
{
    var searchText = SearchTextBox.Text;
    
    if (searchText != _currentSearchText)
    {
        // End the previous search
        if (CodeEditor.SearchIsOpen)
        {
            CodeEditor.EndSearch();
        }
        _currentSearchText = string.Empty;
    }
}
```

### 3. Restart Search in PerformSearch
```csharp
private void PerformSearch(bool forward)
{
    var searchText = SearchTextBox.Text;
    
    // If search text changed, restart the search
    if (searchText != _currentSearchText)
    {
        if (CodeEditor.SearchIsOpen)
        {
            CodeEditor.EndSearch();
        }
        CodeEditor.BeginSearch(searchText, wholeWord: false, matchCase: false);
        _currentSearchText = searchText;
    }
    // If search not open (first search), start it
    else if (!CodeEditor.SearchIsOpen)
    {
        CodeEditor.BeginSearch(searchText, wholeWord: false, matchCase: false);
        _currentSearchText = searchText;
    }
    
    // Navigate to match
    if (forward)
        CodeEditor.FindNext();
    else
        CodeEditor.FindPrevious();
}
```

## How It Works Now

### Scenario 1: First Search
1. User opens search (Ctrl+F)
2. User types "graph"
3. User presses Enter
4. `_currentSearchText` is empty, so `BeginSearch("graph")` is called
5. `_currentSearchText` is set to "graph"
6. First match is found and highlighted

### Scenario 2: Continue Same Search
1. User presses Enter again
2. `searchText` ("graph") == `_currentSearchText` ("graph")
3. Search is already open, so just call `FindNext()`
4. Next match is found and highlighted

### Scenario 3: Change Search Text
1. User changes text to "node"
2. `SearchTextBox_TextChanged` fires
3. "node" != "graph", so `EndSearch()` is called
4. `_currentSearchText` is cleared
5. User presses Enter
6. `searchText` ("node") != `_currentSearchText` (empty)
7. `BeginSearch("node")` is called
8. `_currentSearchText` is set to "node"
9. First match of "node" is found and highlighted

### Scenario 4: Close and Reopen
1. User closes search (Escape)
2. `_currentSearchText` is cleared
3. `EndSearch()` is called
4. User opens search again (Ctrl+F)
5. User types new search term
6. Works as Scenario 1 (first search)

## Testing Checklist

- [x] First search works correctly
- [x] Continuing same search works (multiple Enter presses)
- [x] Changing search text restarts search with new term
- [x] Closing and reopening search works
- [x] No crashes when changing text
- [x] TextChanged doesn't cause crashes (only ends search, doesn't start new one)
- [x] Build succeeds with no errors

## User Experience

### Before Fix
1. Search for "graph" → Works ✓
2. Change text to "node" → Still searches for "graph" ✗
3. Must close and reopen search panel → Annoying ✗

### After Fix
1. Search for "graph" → Works ✓
2. Change text to "node" → Searches for "node" ✓
3. No need to close/reopen → Smooth ✓

## Files Modified
- `MermaidDiagramApp/MainWindow.xaml.cs` - Added `_currentSearchText` tracking and restart logic

## Build Status
✅ Builds successfully with no errors
✅ No new warnings

## Conclusion
The search feature now correctly handles changing search terms without requiring the user to close and reopen the search panel. The fix is stable and doesn't cause crashes.
