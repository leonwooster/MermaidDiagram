# Search Feature - Technical Implementation Details

## Architecture Overview

The search feature is implemented as a self-contained module within `MainWindow.xaml.cs` with a dedicated UI panel in `MainWindow.xaml`.

## Code Structure

### State Management
```csharp
// Instance variables for search state
private List<int> _searchResults = new List<int>();  // Stores positions of all matches
private int _currentSearchIndex = -1;                // Current match index (-1 = no match)
```

### Event Handlers

#### 1. Find_Click (Menu Item)
```csharp
private void Find_Click(object sender, RoutedEventArgs e)
{
    SearchPanel.Visibility = Visibility.Visible;
    SearchTextBox.Focus(FocusState.Programmatic);
}
```
- Opens the search panel
- Sets focus to search text box
- Triggered by: Edit > Find... menu or Ctrl+F

#### 2. CloseSearch_Click
```csharp
private void CloseSearch_Click(object sender, RoutedEventArgs e)
{
    SearchPanel.Visibility = Visibility.Collapsed;
    _searchResults.Clear();
    _currentSearchIndex = -1;
    SearchResultsText.Text = string.Empty;
    CodeEditor.Focus(FocusState.Programmatic);
}
```
- Hides the search panel
- Clears search state
- Returns focus to code editor
- Triggered by: Close button (✕) or Escape key

#### 3. SearchTextBox_KeyDown
```csharp
private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
{
    if (e.Key == VirtualKey.Enter)
    {
        if (CoreWindow.GetForCurrentThread()
            .GetKeyState(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down))
        {
            FindPrevious_Click(sender, new RoutedEventArgs());
        }
        else
        {
            FindNext_Click(sender, new RoutedEventArgs());
        }
        e.Handled = true;
    }
    else if (e.Key == VirtualKey.Escape)
    {
        CloseSearch_Click(sender, new RoutedEventArgs());
        e.Handled = true;
    }
}
```
- Handles keyboard shortcuts within search box
- Enter → Find Next
- Shift+Enter → Find Previous
- Escape → Close search

#### 4. FindNext_Click / FindPrevious_Click
```csharp
private void FindNext_Click(object sender, RoutedEventArgs e)
{
    PerformSearch(forward: true);
}

private void FindPrevious_Click(object sender, RoutedEventArgs e)
{
    PerformSearch(forward: false);
}
```
- Wrapper methods that call PerformSearch
- Pass direction parameter

### Core Search Algorithm

```csharp
private void PerformSearch(bool forward)
{
    // 1. Validate input
    var searchText = SearchTextBox.Text;
    if (string.IsNullOrEmpty(searchText))
    {
        SearchResultsText.Text = string.Empty;
        return;
    }

    var codeText = CodeEditor.Text;
    if (string.IsNullOrEmpty(codeText))
    {
        SearchResultsText.Text = "No results";
        return;
    }

    // 2. Find all occurrences
    _searchResults.Clear();
    int index = 0;
    while ((index = codeText.IndexOf(searchText, index, 
            StringComparison.OrdinalIgnoreCase)) != -1)
    {
        _searchResults.Add(index);
        index += searchText.Length;
    }

    // 3. Handle no results
    if (_searchResults.Count == 0)
    {
        SearchResultsText.Text = "No results";
        _currentSearchIndex = -1;
        return;
    }

    // 4. Navigate to next/previous result
    if (_currentSearchIndex == -1)
    {
        _currentSearchIndex = forward ? 0 : _searchResults.Count - 1;
    }
    else
    {
        if (forward)
        {
            _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
        }
        else
        {
            _currentSearchIndex = (_currentSearchIndex - 1 + _searchResults.Count) 
                                  % _searchResults.Count;
        }
    }

    // 5. Update UI
    SearchResultsText.Text = $"{_currentSearchIndex + 1} of {_searchResults.Count}";
    
    // 6. Focus editor (cannot select text due to TextControlBox limitations)
    CodeEditor.Focus(FocusState.Programmatic);
}
```

## Algorithm Analysis

### Time Complexity
- **Search phase**: O(n × m)
  - n = length of code text
  - m = length of search term
  - Uses `string.IndexOf()` which is optimized
- **Navigation phase**: O(1)
  - Simple array indexing and modulo arithmetic

### Space Complexity
- O(k) where k = number of matches
- Stores only match positions, not the matched text

### Search Strategy
- **Case-insensitive**: Uses `StringComparison.OrdinalIgnoreCase`
- **Substring matching**: Finds partial matches (not whole word only)
- **Non-regex**: Simple text search, no pattern matching

## UI Implementation (XAML)

### Search Panel Structure
```xml
<Border x:Name="SearchPanel" 
        Grid.Row="0" 
        Background="{ThemeResource LayerFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="0,0,0,1"
        Padding="8"
        Visibility="Collapsed">
    <Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto,Auto">
        <!-- Label -->
        <TextBlock Grid.Column="0" Text="Find:" />
        
        <!-- Search input -->
        <TextBox x:Name="SearchTextBox" 
                 Grid.Column="1" 
                 PlaceholderText="Search in code..."
                 KeyDown="SearchTextBox_KeyDown"/>
        
        <!-- Previous button -->
        <Button x:Name="FindPreviousButton" 
                Grid.Column="2" 
                Content="▲" 
                Click="FindPrevious_Click"/>
        
        <!-- Next button -->
        <Button x:Name="FindNextButton" 
                Grid.Column="3" 
                Content="▼" 
                Click="FindNext_Click"/>
        
        <!-- Results counter -->
        <TextBlock x:Name="SearchResultsText" 
                   Grid.Column="4" />
        
        <!-- Close button -->
        <Button x:Name="CloseSearchButton" 
                Grid.Column="5" 
                Content="✕" 
                Click="CloseSearch_Click"/>
    </Grid>
</Border>
```

### Menu Integration
```xml
<MenuBarItem Title="Edit">
    <MenuFlyoutItem Text="Find..." Click="Find_Click">
        <MenuFlyoutItem.KeyboardAccelerators>
            <KeyboardAccelerator Key="F" Modifiers="Control"/>
        </MenuFlyoutItem.KeyboardAccelerators>
    </MenuFlyoutItem>
    <!-- Other menu items -->
</MenuBarItem>
```

## TextControlBox Limitations

### Missing APIs
The `TextControlBox` control does not expose:

```csharp
// ❌ Not available
public int SelectionStart { get; set; }
public int SelectionLength { get; set; }
public void Select(int start, int length);
public void ScrollToCaret();
public event EventHandler SelectionChanged;
```

### Impact on Features

#### Cannot Implement: Text Selection
```csharp
// This code would work with a standard TextBox but NOT with TextControlBox
private void HighlightMatch(int position, int length)
{
    CodeEditor.SelectionStart = position;      // ❌ Property doesn't exist
    CodeEditor.SelectionLength = length;       // ❌ Property doesn't exist
    CodeEditor.Focus(FocusState.Programmatic);
}
```

#### Cannot Implement: Auto-Scroll
```csharp
// This code would work with a standard TextBox but NOT with TextControlBox
private void ScrollToMatch(int position)
{
    CodeEditor.SelectionStart = position;      // ❌ Property doesn't exist
    CodeEditor.ScrollToCaret();                // ❌ Method doesn't exist
}
```

#### Cannot Implement: Synchronized Scrolling
```csharp
// Would need cursor position to sync with preview
private void CodeEditor_SelectionChanged(object sender, EventArgs e)
{
    int cursorPosition = CodeEditor.SelectionStart;  // ❌ Property doesn't exist
    // ... sync with preview panel
}
```

## Workarounds Considered

### 1. Reflection (Rejected)
```csharp
// Attempted to access private fields via reflection
var selectionStartField = typeof(TextControlBox)
    .GetField("_selectionStart", BindingFlags.NonPublic | BindingFlags.Instance);
```
**Reason for rejection**: 
- Fragile (breaks with control updates)
- May not exist (implementation detail)
- Poor maintainability

### 2. Visual Tree Manipulation (Rejected)
```csharp
// Attempted to find internal TextBox in visual tree
var textBox = FindChildOfType<TextBox>(CodeEditor);
```
**Reason for rejection**:
- TextControlBox may not use TextBox internally
- Implementation-dependent
- Unreliable across versions

### 3. Custom Control (Not Implemented)
```csharp
// Create custom control with selection APIs
public class SelectableTextControlBox : TextControlBox
{
    public int SelectionStart { get; set; }
    public int SelectionLength { get; set; }
}
```
**Reason for not implementing**:
- Requires forking/extending TextControlBox
- Significant development effort
- May require access to internal APIs

## Alternative Editor Controls

### Option 1: Monaco Editor (via WebView2)
```csharp
// Pros:
// - Full-featured code editor
// - Syntax highlighting
// - Selection APIs
// - Search/replace built-in

// Cons:
// - Requires WebView2
// - More complex integration
// - Larger memory footprint
```

### Option 2: Windows Community Toolkit RichEditBox
```csharp
// Pros:
// - Native WinUI control
// - Selection APIs available
// - Rich text support

// Cons:
// - Different API surface
// - May need code migration
// - Less specialized for code editing
```

### Option 3: AvalonEdit (WPF)
```csharp
// Pros:
// - Excellent code editor
// - Full selection APIs
// - Syntax highlighting

// Cons:
// - WPF control (not WinUI)
// - Requires WPF interop
// - May have styling issues
```

## Performance Considerations

### Search Performance
```csharp
// Current implementation
// Time: O(n × m) for each search
// Space: O(k) where k = number of matches

// For a 10,000 character file with 100 matches:
// - Search time: ~1-2ms (acceptable)
// - Memory: ~400 bytes (100 × 4 bytes per int)
```

### Optimization Opportunities

#### 1. Incremental Search
```csharp
// Only re-search if text changed
private string _lastSearchText = string.Empty;
private string _lastCodeText = string.Empty;

private void PerformSearch(bool forward)
{
    if (searchText == _lastSearchText && codeText == _lastCodeText)
    {
        // Just navigate, don't re-search
        NavigateToMatch(forward);
        return;
    }
    
    // Perform full search
    FindAllMatches();
    _lastSearchText = searchText;
    _lastCodeText = codeText;
}
```

#### 2. Debounced Search
```csharp
// Wait for user to stop typing before searching
private DispatcherTimer _searchDebounceTimer;

private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
{
    _searchDebounceTimer?.Stop();
    _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
    _searchDebounceTimer.Tick += (s, args) =>
    {
        PerformSearch(forward: true);
        _searchDebounceTimer.Stop();
    };
    _searchDebounceTimer.Start();
}
```

#### 3. Boyer-Moore Algorithm
```csharp
// For very large files, use more efficient search algorithm
// Current: O(n × m)
// Boyer-Moore: O(n / m) average case
```

## Testing Considerations

### Unit Tests
```csharp
[TestClass]
public class SearchFeatureTests
{
    [TestMethod]
    public void Search_FindsAllOccurrences()
    {
        // Arrange
        var text = "graph TD\ngraph LR\nGRAPH TB";
        var searchTerm = "graph";
        
        // Act
        var results = FindAllOccurrences(text, searchTerm);
        
        // Assert
        Assert.AreEqual(3, results.Count);
    }
    
    [TestMethod]
    public void Search_IsCaseInsensitive()
    {
        // Test case insensitivity
    }
    
    [TestMethod]
    public void Navigation_WrapsAround()
    {
        // Test circular navigation
    }
}
```

### Integration Tests
- Test with real TextControlBox instance
- Test keyboard shortcuts
- Test UI state transitions
- Test with large files (performance)

### Manual Testing Checklist
- [ ] Search finds all matches
- [ ] Counter shows correct "X of Y"
- [ ] Navigation wraps correctly
- [ ] Keyboard shortcuts work
- [ ] UI updates properly
- [ ] No crashes with empty input
- [ ] No crashes with no matches
- [ ] Performance acceptable with large files

## Future Enhancements

### Phase 1: Enhanced Search
- [ ] Regex support
- [ ] Match case toggle
- [ ] Whole word toggle
- [ ] Search history

### Phase 2: Replace Functionality
- [ ] Replace current match
- [ ] Replace all matches
- [ ] Replace with confirmation

### Phase 3: Advanced Features
- [ ] Multi-file search
- [ ] Search in selection
- [ ] Incremental search (search as you type)
- [ ] Search results panel

### Phase 4: Editor Replacement
- [ ] Evaluate alternative editor controls
- [ ] Implement text selection
- [ ] Implement synchronized scrolling
- [ ] Add syntax highlighting

## Conclusion

The search feature is **functional and usable** within the constraints of the TextControlBox control. While it cannot highlight matches visually, it provides:

✅ Accurate match finding
✅ Clear result counting
✅ Efficient navigation
✅ Good keyboard support
✅ Clean UI integration

For most users, this provides sufficient search functionality. Visual highlighting would require replacing the editor control, which is a larger architectural change.
