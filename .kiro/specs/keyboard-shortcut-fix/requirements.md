# Requirements Document

## Introduction

The Mermaid Diagram Editor currently has a keyboard shortcut conflict where pressing F11 triggers the system volume mute instead of toggling full-screen preview mode. This issue occurs because Windows intercepts the F11 key before the application can handle it. This feature will fix the keyboard shortcut handling and provide users with alternative, more reliable shortcuts for full-screen functionality.

## Glossary

- **Application**: The Mermaid Diagram Editor WinUI3 desktop application
- **Full-Screen Preview**: A viewing mode where only the diagram preview is displayed, hiding the editor and menu bar
- **Presentation Mode**: A distraction-free mode for presenting diagrams (currently F5)
- **Keyboard Accelerator**: A WinUI3 keyboard shortcut binding mechanism
- **System Shortcut**: A keyboard shortcut handled by Windows OS before reaching the application

## Requirements

### Requirement 1

**User Story:** As a user, I want to toggle full-screen preview mode using a keyboard shortcut that doesn't conflict with system shortcuts, so that I can quickly enter and exit full-screen mode without interference.

#### Acceptance Criteria

1. WHEN a user presses F11, THE Application SHALL attempt to toggle full-screen preview mode
2. WHEN F11 is intercepted by the system, THE Application SHALL provide an alternative keyboard shortcut (Ctrl+F11) that reliably toggles full-screen preview
3. WHEN a user presses Ctrl+F11, THE Application SHALL toggle full-screen preview mode without system interference
4. WHEN a user presses Escape while in full-screen preview mode, THE Application SHALL exit full-screen preview mode
5. WHEN the user accesses the View menu, THE Application SHALL display both F11 and Ctrl+F11 as keyboard shortcuts for full-screen preview

### Requirement 2

**User Story:** As a user, I want clear visual feedback about which keyboard shortcuts are available, so that I can learn and use them effectively.

#### Acceptance Criteria

1. WHEN a user opens the View menu, THE Application SHALL display keyboard shortcuts next to each menu item
2. WHEN multiple keyboard shortcuts exist for the same action, THE Application SHALL display all shortcuts in the menu
3. WHEN a keyboard shortcut is pressed, THE Application SHALL execute the corresponding action immediately
4. WHEN a keyboard shortcut conflicts with system shortcuts, THE Application SHALL document the alternative shortcut in the menu

### Requirement 3

**User Story:** As a user, I want keyboard shortcuts to work consistently whether the editor or preview has focus, so that I don't have to think about which panel is active.

#### Acceptance Criteria

1. WHEN the code editor has focus and a user presses Ctrl+F11, THE Application SHALL toggle full-screen preview mode
2. WHEN the preview browser (WebView2) has focus and a user presses Ctrl+F11, THE Application SHALL toggle full-screen preview mode
3. WHEN the preview browser has focus and a user presses Escape, THE Application SHALL exit full-screen or presentation mode
4. WHEN any panel has focus and a user presses F7, THE Application SHALL open the syntax checker dialog

### Requirement 4

**User Story:** As a developer, I want the keyboard shortcut system to be maintainable and extensible, so that adding new shortcuts in the future is straightforward.

#### Acceptance Criteria

1. WHEN keyboard shortcuts are defined, THE Application SHALL use both XAML KeyboardAccelerator and code-behind event handlers for reliability
2. WHEN WebView2 has focus, THE Application SHALL intercept keyboard events via JavaScript and forward them to the C# application
3. WHEN a new keyboard shortcut is added, THE Application SHALL require changes in only one centralized location
4. WHEN keyboard shortcuts are modified, THE Application SHALL maintain backward compatibility with existing user muscle memory where possible

### Requirement 5

**User Story:** As a user, I want to know if a keyboard shortcut isn't working due to system conflicts, so that I can use the alternative shortcut instead.

#### Acceptance Criteria

1. WHEN the application starts for the first time, THE Application SHALL display a tip about using Ctrl+F11 if F11 doesn't work
2. WHEN a user attempts to use F11 and it fails, THE Application SHALL detect the failure and show an informational message about using Ctrl+F11
3. WHEN the informational message is shown, THE Application SHALL provide an option to not show it again
4. WHEN a user dismisses the tip, THE Application SHALL remember the preference and not show it again
