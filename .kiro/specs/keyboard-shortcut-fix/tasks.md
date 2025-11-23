# Implementation Plan

- [x] 1. Create preference service for keyboard shortcut tips








  - Create `ShortcutPreferencesService.cs` with methods to get/set tip display preference
  - Use `ApplicationData.LocalSettings` for persistence
  - Implement `GetShowTips()` and `SetShowTips(bool)` methods
  - Add default value handling (true by default)
  - _Requirements: 5.4_

- [x] 1.1 Write property test for preference persistence






  - **Property 5: Tip preference round-trip**
  - **Validates: Requirements 5.4**

- [x] 2. Create KeyboardShortcutManager service






  - Create `Services/KeyboardShortcutManager.cs` class
  - Implement shortcut registration with `Dictionary<string, Action>`
  - Add `RegisterShortcut(VirtualKey, VirtualKeyModifiers, Action)` method
  - Add `HandleKeyDown(KeyRoutedEventArgs)` method for WinUI events
  - Add `HandleWebViewKeyEvent(string, bool, bool, bool)` method for WebView2 events


  - Inject `ILogger` and `ShortcutPreferencesService` dependencies
  - _Requirements: 1.2, 1.3, 2.3, 3.4, 4.2_

- [x] 2.1 Write property test for shortcut action execution



  - **Property 3: Keyboard shortcuts execute their actions**
  - **Validates: Requirements 2.3**

- [x] 3. Add Ctrl+F11 keyboard accelerator to XAML




  - Open `MainWindow.xaml`
  - Locate "Full Screen Preview" MenuFlyoutItem in View menu
  - Add second KeyboardAccelerator with Key="F11" Modifiers="Control"
  - Update menu item text to show "Full Screen Preview (F11 or Ctrl+F11)"
  - _Requirements: 1.2, 1.5_

- [x] 4. Enhance WebView2 keyboard event interception





  - Open `Assets/UnifiedRenderer.html`
  - Add `keydown` event listener with capture phase (third parameter = true)
  - Intercept F11, Ctrl+F11, Escape, and F7 keys
  - Call `preventDefault()` to stop browser handling
  - Send `postMessage` to C# with key details (key, ctrlKey, shiftKey, altKey)
  - Use message type "keypress" for keyboard events
  - _Requirements: 3.2, 3.3, 4.2_

- [x] 5. Update MainWindow to use KeyboardShortcutManager





  - Add `KeyboardShortcutManager` field to MainWindow class
  - Initialize manager in constructor with logger and preferences service
  - Register all keyboard shortcuts (F11, Ctrl+F11, Escape, F7, Ctrl+F5)
  - Add `PreviewKeyDown` event handler to MainWindow Grid in XAML
  - Implement `MainWindow_PreviewKeyDown` method to call manager
  - _Requirements: 1.2, 1.3, 1.4, 3.1, 3.4_

- [x] 5.1 Write property test for Ctrl+F11 toggle behavior


  - **Property 1: Ctrl+F11 toggles full-screen state**
  - **Validates: Requirements 1.2, 1.3**

- [x] 5.2 Write property test for Escape exit behavior


  - **Property 2: Escape exits full-screen when active**
  - **Validates: Requirements 1.4**

- [x] 6. Enhance WebView2 message handler for keyboard events





  - Locate `WebMessageReceived` event handler in `InitializeWebViewAsync()`
  - Add case for message type "keypress"
  - Parse `KeyboardEventMessage` from JSON
  - Call `KeyboardShortcutManager.HandleWebViewKeyEvent()` with parsed data
  - Log keyboard events for debugging
  - _Requirements: 3.2, 3.3, 4.2_

- [x] 7. Implement first-run tip display




  - Add `ShowKeyboardShortcutTip()` method to MainWindow
  - Check `ShortcutPreferencesService.GetShowTips()` on first WebView ready
  - Display InfoBar or TeachingTip about Ctrl+F11 alternative
  - Add "Don't show again" checkbox to tip
  - Save preference when user dismisses tip
  - _Requirements: 5.1, 5.3, 5.4_

- [x] 7.1 Write unit test for tip display logic


  - Test that tip shows on first run
  - Test that tip doesn't show when preference is false
  - Test that dismissing tip saves preference

- [x] 8. Add keyboard shortcut models





  - Create `Models/ShortcutDefinition.cs` class
  - Add properties: Key, Modifiers, DisplayName, Action, Description
  - Create `Models/KeyboardEventMessage.cs` class
  - Add properties: Type, Key, CtrlKey, ShiftKey, AltKey
  - _Requirements: 4.1_

- [x] 8.1 Write unit tests for model serialization


  - Test KeyboardEventMessage JSON deserialization
  - Test handling of missing properties

- [x] 9. Update existing keyboard shortcuts to use manager




  - Migrate F5 (Presentation Mode) to manager
  - Migrate Ctrl+F5 (Refresh Preview) to manager
  - Migrate F7 (Check Syntax) to manager
  - Remove duplicate event handlers if any
  - Test all shortcuts still work
  - _Requirements: 2.3, 3.4_

- [x] 9.1 Write property test for F7 from any focus location











  - **Property 4: F7 opens syntax checker from any focus location**
  - **Validates: Requirements 3.4**

- [x] 10. Checkpoint - Ensure all tests pass




  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Add logging for keyboard shortcut debugging




  - Log when shortcuts are registered
  - Log when shortcuts are triggered
  - Log when WebView2 forwards keyboard events
  - Log when system intercepts shortcuts (F11 not received)
  - Add debug-level logging to avoid noise in production
  - _Requirements: 4.1_

- [x] 12. Update documentation





  - Update `docs/USER_GUIDE.md` with keyboard shortcut information
  - Document F11 vs Ctrl+F11 difference
  - Add troubleshooting section for keyboard shortcuts
  - List all available keyboard shortcuts in a table
  - _Requirements: 2.4_

- [x] 13. Final testing and validation




  - Test F11 behavior (may be intercepted by Windows)
  - Test Ctrl+F11 from editor focus
  - Test Ctrl+F11 from WebView2 focus
  - Test Escape from full-screen mode
  - Test Escape from presentation mode
  - Test F7 from various focus locations
  - Test first-run tip display
  - Test "Don't show again" preference
  - Verify menu displays both shortcuts
  - Test on Windows 10 and Windows 11
  - _Requirements: All_
