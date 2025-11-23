# Design Document: Keyboard Shortcut Fix

## Overview

This design addresses the keyboard shortcut conflict where F11 is intercepted by Windows system shortcuts (volume mute) before reaching the Mermaid Diagram Editor application. The solution implements a dual-shortcut approach with F11 and Ctrl+F11, improves keyboard event handling across different UI contexts (editor, WebView2), and provides user feedback about shortcut availability.

## Architecture

### High-Level Design

```
┌─────────────────────────────────────────────────────────────┐
│                    MainWindow (XAML)                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  KeyboardAccelerator Definitions                       │ │
│  │  - F11 → ToggleFullScreen                             │ │
│  │  - Ctrl+F11 → ToggleFullScreen                        │ │
│  │  - Escape → ExitFullScreen (conditional)              │ │
│  │  - F7 → CheckSyntax                                    │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────────┐
│              KeyboardShortcutManager (New)                   │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  - RegisterShortcut(key, modifiers, action)           │ │
│  │  - HandleKeyDown(KeyRoutedEventArgs)                  │ │
│  │  - HandleWebViewKeyEvent(string keyMessage)           │ │
│  │  - ShowShortcutTip(string message)                    │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────────┐
│              WebView2 JavaScript Interop                     │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  document.addEventListener('keydown', (e) => {        │ │
│  │    if (e.key === 'F11' || (e.ctrlKey && e.key === 'F11')) { │
│  │      e.preventDefault();                               │ │
│  │      window.chrome.webview.postMessage({              │ │
│  │        type: 'keypress',                              │ │
│  │        key: 'F11',                                    │ │
│  │        ctrlKey: e.ctrlKey                             │ │
│  │      });                                              │ │
│  │    }                                                   │ │
│  │  });                                                   │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. KeyboardShortcutManager (New Service)

**Location:** `MermaidDiagramApp/Services/KeyboardShortcutManager.cs`

**Purpose:** Centralized management of keyboard shortcuts with fallback handling and user feedback.

```csharp
public class KeyboardShortcutManager
{
    private readonly Dictionary<string, Action> _shortcuts;
    private readonly ILogger _logger;
    private bool _hasShownF11Tip;
    
    public KeyboardShortcutManager(ILogger logger);
    
    // Register a keyboard shortcut with its action
    public void RegisterShortcut(VirtualKey key, VirtualKeyModifiers modifiers, Action action);
    
    // Handle key down events from MainWindow
    public bool HandleKeyDown(KeyRoutedEventArgs e);
    
    // Handle key events forwarded from WebView2
    public bool HandleWebViewKeyEvent(string key, bool ctrlKey, bool shiftKey, bool altKey);
    
    // Show tip about alternative shortcuts
    public void ShowShortcutTip(XamlRoot xamlRoot, string message);
    
    // Check if user has dismissed tips
    public bool ShouldShowTips();
    
    // Save user preference to not show tips
    public void DismissTips();
}
```

### 2. ShortcutPreferencesService (New Service)

**Location:** `MermaidDiagramApp/Services/ShortcutPreferencesService.cs`

**Purpose:** Persist user preferences about keyboard shortcut tips.

```csharp
public class ShortcutPreferencesService
{
    private const string SHOW_TIPS_KEY = "ShowKeyboardShortcutTips";
    
    public bool GetShowTips();
    public void SetShowTips(bool show);
}
```

### 3. MainWindow Modifications

**Changes to MainWindow.xaml:**
- Add Ctrl+F11 KeyboardAccelerator to "Full Screen Preview" menu item
- Add PreviewKeyDown handler to MainWindow Grid
- Update menu item text to show both shortcuts

**Changes to MainWindow.xaml.cs:**
- Initialize KeyboardShortcutManager in constructor
- Register all keyboard shortcuts with the manager
- Add PreviewKeyDown event handler
- Enhance WebView2 message handler to process keyboard events
- Add first-run tip display logic

### 4. WebView2 JavaScript Enhancement

**Location:** `MermaidDiagramApp/Assets/UnifiedRenderer.html`

**Enhancement:** Add keyboard event listener that intercepts F11 and Ctrl+F11 before browser handles them.

```javascript
// Intercept keyboard shortcuts in WebView2
document.addEventListener('keydown', function(e) {
    // Handle F11 and Ctrl+F11
    if (e.key === 'F11' || e.keyCode === 122) {
        e.preventDefault();
        window.chrome.webview.postMessage({
            type: 'keypress',
            key: 'F11',
            ctrlKey: e.ctrlKey,
            shiftKey: e.shiftKey,
            altKey: e.altKey
        });
        return false;
    }
    
    // Handle Escape
    if (e.key === 'Escape' || e.keyCode === 27) {
        window.chrome.webview.postMessage({
            type: 'keypress',
            key: 'Escape',
            ctrlKey: e.ctrlKey,
            shiftKey: e.shiftKey,
            altKey: e.altKey
        });
    }
    
    // Handle F7
    if (e.key === 'F7' || e.keyCode === 118) {
        e.preventDefault();
        window.chrome.webview.postMessage({
            type: 'keypress',
            key: 'F7',
            ctrlKey: e.ctrlKey,
            shiftKey: e.shiftKey,
            altKey: e.altKey
        });
        return false;
    }
}, true); // Use capture phase to intercept before other handlers
```

## Data Models

### ShortcutDefinition

```csharp
public class ShortcutDefinition
{
    public VirtualKey Key { get; set; }
    public VirtualKeyModifiers Modifiers { get; set; }
    public string DisplayName { get; set; }
    public Action Action { get; set; }
    public string Description { get; set; }
}
```

### KeyboardEventMessage

```csharp
public class KeyboardEventMessage
{
    public string Type { get; set; } // "keypress"
    public string Key { get; set; }  // "F11", "Escape", etc.
    public bool CtrlKey { get; set; }
    public bool ShiftKey { get; set; }
    public bool AltKey { get; set; }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Ctrl+F11 toggles full-screen state
*For any* initial full-screen state (true or false), pressing Ctrl+F11 should toggle the state to its opposite value.
**Validates: Requirements 1.2, 1.3**

### Property 2: Escape exits full-screen when active
*For any* application state, pressing Escape should result in full-screen mode being false if it was previously true, and should remain false if it was already false.
**Validates: Requirements 1.4**

### Property 3: Keyboard shortcuts execute their actions
*For any* registered keyboard shortcut, when the corresponding key combination is pressed, the associated action should be invoked.
**Validates: Requirements 2.3**

### Property 4: F7 opens syntax checker from any focus location
*For any* UI element that has focus (editor, WebView2, menu), pressing F7 should open the syntax checker dialog.
**Validates: Requirements 3.4**

### Property 5: Tip preference round-trip
*For any* preference value (show tips = true/false), saving the preference and then loading it should return the same value.
**Validates: Requirements 5.4**

## Error Handling

### Keyboard Event Handling Errors

**Scenario:** KeyboardAccelerator fails to register
- **Handling:** Log warning and fall back to PreviewKeyDown event handler
- **User Impact:** Shortcut still works via fallback mechanism

**Scenario:** WebView2 message passing fails
- **Handling:** Log error and show user tip about using menu items
- **User Impact:** User can still access functions via menu

**Scenario:** System intercepts F11 before application
- **Handling:** Detect lack of F11 events and show tip about Ctrl+F11
- **User Impact:** User learns about alternative shortcut

### Preference Storage Errors

**Scenario:** Cannot save tip preference
- **Handling:** Log error and continue showing tips
- **User Impact:** User may see tips again on next launch

**Scenario:** Cannot load tip preference
- **Handling:** Default to showing tips (safer default)
- **User Impact:** User may see tips even if previously dismissed

## Testing Strategy

### Unit Tests

**KeyboardShortcutManager Tests:**
- Test shortcut registration
- Test key combination matching
- Test action invocation
- Test tip display logic
- Test preference checking

**ShortcutPreferencesService Tests:**
- Test saving preferences
- Test loading preferences
- Test default values
- Test invalid data handling

### Integration Tests

**Full-Screen Toggle Tests:**
- Test F11 from editor (if not intercepted)
- Test Ctrl+F11 from editor
- Test Ctrl+F11 from WebView2
- Test Escape from full-screen mode
- Test Escape when not in full-screen (no effect)

**WebView2 Interop Tests:**
- Test JavaScript message sending
- Test C# message receiving
- Test key event parsing
- Test action execution from WebView2 events

### Property-Based Tests

**Property Test 1: Toggle consistency**
- Generate random initial states
- Apply Ctrl+F11 twice
- Verify state returns to original

**Property Test 2: Escape idempotence**
- Generate random initial states
- Press Escape multiple times
- Verify full-screen remains false after first press

**Property Test 3: Preference persistence**
- Generate random preference values
- Save and load
- Verify values match

### Manual Testing

**User Experience Tests:**
- Verify menu displays both F11 and Ctrl+F11
- Verify tip appears on first run
- Verify "Don't show again" works
- Verify shortcuts work from different focus contexts
- Test on different Windows versions (10, 11)

## Implementation Notes

### XAML KeyboardAccelerator Limitations

WinUI3 KeyboardAccelerators have limitations:
1. They don't always work when WebView2 has focus
2. System shortcuts can intercept them
3. They require XamlRoot to be set for dialogs

**Solution:** Use dual approach:
- XAML KeyboardAccelerators for standard cases
- PreviewKeyDown event handler as fallback
- JavaScript interception for WebView2 focus

### WebView2 Focus Handling

When WebView2 has focus, keyboard events go to the browser first. We must:
1. Intercept in JavaScript using capture phase
2. Prevent default browser behavior
3. Forward to C# via postMessage
4. Handle in C# KeyboardShortcutManager

### First-Run Detection

Use ApplicationData.LocalSettings to track:
- `HasShownKeyboardTip`: bool indicating if tip was shown
- `ShowKeyboardShortcutTips`: bool indicating user preference

### Backward Compatibility

Existing users expect F11 to work. We must:
1. Keep F11 registered (works when not intercepted)
2. Add Ctrl+F11 as reliable alternative
3. Show tip only if F11 fails multiple times
4. Don't break existing muscle memory

## Performance Considerations

### Event Handler Performance

- KeyDown events fire frequently during typing
- Must filter quickly to avoid lag
- Use dictionary lookup for O(1) shortcut matching
- Avoid heavy operations in event handlers

### Tip Display Throttling

- Don't show tips more than once per session
- Cache preference in memory to avoid repeated disk reads
- Use async loading for preferences

### WebView2 Message Overhead

- Minimize message size (use short property names)
- Only send messages for relevant keys
- Don't send messages for every keypress

## Future Enhancements

### Customizable Shortcuts

Allow users to customize keyboard shortcuts:
- Settings dialog for shortcut configuration
- Conflict detection
- Reset to defaults option

### Shortcut Discovery

Help users learn shortcuts:
- Tooltip hints on menu items
- Keyboard shortcut cheat sheet dialog
- Search for commands by name

### Global Shortcuts

Register global shortcuts that work even when app is not focused:
- Requires Windows API interop
- Security considerations
- User permission required

## Dependencies

- **Microsoft.UI.Xaml**: For KeyboardAccelerator and KeyRoutedEventArgs
- **Microsoft.Web.WebView2**: For JavaScript interop
- **Windows.Storage**: For ApplicationData.LocalSettings
- **System.Text.Json**: For parsing WebView2 messages

## Migration Path

### Phase 1: Add Ctrl+F11 Support
- Add KeyboardAccelerator to XAML
- Update menu text
- Test basic functionality

### Phase 2: Enhance WebView2 Handling
- Add JavaScript event listener
- Add C# message handler
- Test from WebView2 focus

### Phase 3: Add User Feedback
- Implement tip system
- Add preference storage
- Test first-run experience

### Phase 4: Refactor to Manager
- Create KeyboardShortcutManager
- Migrate existing shortcuts
- Centralize handling logic

## Conclusion

This design provides a robust solution to the F11 keyboard shortcut conflict by:
1. Adding Ctrl+F11 as a reliable alternative
2. Improving keyboard event handling across all UI contexts
3. Providing user feedback about shortcut availability
4. Maintaining backward compatibility with existing shortcuts

The implementation follows SOLID principles with a centralized KeyboardShortcutManager service, clear separation of concerns, and extensibility for future enhancements.
