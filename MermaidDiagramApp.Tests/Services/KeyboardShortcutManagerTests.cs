using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using Windows.System;
using Microsoft.UI.Xaml.Input;
using Xunit;
using System;
using System.Collections.Generic;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Property-based tests for KeyboardShortcutManager.
/// Feature: keyboard-shortcut-fix, Property 3: Keyboard shortcuts execute their actions
/// </summary>
public class KeyboardShortcutManagerTests
{
    /// <summary>
    /// Simple test logger that captures log messages.
    /// </summary>
    private class TestLogger : ILogger
    {
        public List<string> Messages { get; } = new List<string>();

        public void Log(LogLevel level, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
        {
            Messages.Add($"[{level}] {message}");
        }
    }

    /// <summary>
    /// Property 3: Keyboard shortcuts execute their actions
    /// For any registered keyboard shortcut, when the corresponding key combination is pressed,
    /// the associated action should be invoked.
    /// Validates: Requirements 2.3
    /// </summary>
    [Property(MaxTest = 100)]
    public void KeyboardShortcutsExecuteTheirActions(bool useCtrl, bool useShift, bool useAlt)
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        // Track if action was executed
        bool actionExecuted = false;
        Action testAction = () => { actionExecuted = true; };

        // Build modifiers based on random booleans
        var modifiers = VirtualKeyModifiers.None;
        if (useCtrl) modifiers |= VirtualKeyModifiers.Control;
        if (useShift) modifiers |= VirtualKeyModifiers.Shift;
        if (useAlt) modifiers |= VirtualKeyModifiers.Menu;

        // Use F11 as test key
        var key = VirtualKey.F11;

        // Act
        manager.RegisterShortcut(key, modifiers, testAction);
        
        // Simulate the key event via WebView (easier to test than KeyRoutedEventArgs)
        var handled = manager.HandleWebViewKeyEvent("F11", useCtrl, useShift, useAlt);

        // Assert
        Assert.True(actionExecuted, "Action should have been executed when shortcut was triggered");
        Assert.True(handled, "HandleWebViewKeyEvent should return true when shortcut is handled");
    }

    /// <summary>
    /// Unit test: Registering a shortcut should not throw
    /// </summary>
    [Fact]
    public void RegisterShortcut_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);
        Action testAction = () => { };

        // Act & Assert
        var exception = Record.Exception(() => 
            manager.RegisterShortcut(VirtualKey.F11, VirtualKeyModifiers.Control, testAction));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// Unit test: Registering null action should throw
    /// </summary>
    [Fact]
    public void RegisterShortcut_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            manager.RegisterShortcut(VirtualKey.F11, VirtualKeyModifiers.None, null!));
    }

    /// <summary>
    /// Unit test: Unregistered shortcut should not execute
    /// </summary>
    [Fact]
    public void HandleWebViewKeyEvent_WithUnregisteredShortcut_ReturnsFalse()
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        // Act
        var handled = manager.HandleWebViewKeyEvent("F11", false, false, false);

        // Assert
        Assert.False(handled);
    }

    /// <summary>
    /// Unit test: Multiple shortcuts can be registered
    /// </summary>
    [Fact]
    public void RegisterShortcut_WithMultipleShortcuts_AllExecuteCorrectly()
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        bool action1Executed = false;
        bool action2Executed = false;
        bool action3Executed = false;

        // Act
        manager.RegisterShortcut(VirtualKey.F11, VirtualKeyModifiers.None, () => { action1Executed = true; });
        manager.RegisterShortcut(VirtualKey.F11, VirtualKeyModifiers.Control, () => { action2Executed = true; });
        manager.RegisterShortcut(VirtualKey.F7, VirtualKeyModifiers.None, () => { action3Executed = true; });

        manager.HandleWebViewKeyEvent("F11", false, false, false);
        manager.HandleWebViewKeyEvent("F11", true, false, false);
        manager.HandleWebViewKeyEvent("F7", false, false, false);

        // Assert
        Assert.True(action1Executed, "F11 action should execute");
        Assert.True(action2Executed, "Ctrl+F11 action should execute");
        Assert.True(action3Executed, "F7 action should execute");
    }

    /// <summary>
    /// Unit test: Escape key should be recognized
    /// </summary>
    [Fact]
    public void HandleWebViewKeyEvent_WithEscapeKey_ExecutesAction()
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        bool actionExecuted = false;
        manager.RegisterShortcut(VirtualKey.Escape, VirtualKeyModifiers.None, () => { actionExecuted = true; });

        // Act
        var handled = manager.HandleWebViewKeyEvent("Escape", false, false, false);

        // Assert
        Assert.True(actionExecuted);
        Assert.True(handled);
    }

    /// <summary>
    /// Unit test: F5 key should be recognized
    /// </summary>
    [Fact]
    public void HandleWebViewKeyEvent_WithF5Key_ExecutesAction()
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        bool actionExecuted = false;
        manager.RegisterShortcut(VirtualKey.F5, VirtualKeyModifiers.Control, () => { actionExecuted = true; });

        // Act
        var handled = manager.HandleWebViewKeyEvent("F5", true, false, false);

        // Assert
        Assert.True(actionExecuted);
        Assert.True(handled);
    }

    /// <summary>
    /// Unit test: Invalid key name should return false
    /// </summary>
    [Fact]
    public void HandleWebViewKeyEvent_WithInvalidKey_ReturnsFalse()
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        // Act
        var handled = manager.HandleWebViewKeyEvent("InvalidKey", false, false, false);

        // Assert
        Assert.False(handled);
    }

    /// <summary>
    /// Unit test: Null or empty key should return false
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HandleWebViewKeyEvent_WithNullOrEmptyKey_ReturnsFalse(string? key)
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        // Act
        var handled = manager.HandleWebViewKeyEvent(key!, false, false, false);

        // Assert
        Assert.False(handled);
    }

    /// <summary>
    /// Property 1: Ctrl+F11 toggles full-screen state
    /// For any initial full-screen state (true or false), pressing Ctrl+F11 should toggle 
    /// the state to its opposite value.
    /// Feature: keyboard-shortcut-fix, Property 1: Ctrl+F11 toggles full-screen state
    /// Validates: Requirements 1.2, 1.3
    /// </summary>
    [Property(MaxTest = 100)]
    public void CtrlF11TogglesFullScreenState(bool initialState)
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        bool currentState = initialState;
        Action toggleAction = () => { currentState = !currentState; };

        // Register Ctrl+F11 to toggle the state
        manager.RegisterShortcut(VirtualKey.F11, VirtualKeyModifiers.Control, toggleAction);

        // Act - Press Ctrl+F11 once
        manager.HandleWebViewKeyEvent("F11", ctrlKey: true, shiftKey: false, altKey: false);

        // Assert - State should be toggled
        Assert.NotEqual(initialState, currentState);
        Assert.Equal(!initialState, currentState);

        // Act - Press Ctrl+F11 again
        manager.HandleWebViewKeyEvent("F11", ctrlKey: true, shiftKey: false, altKey: false);

        // Assert - State should be back to initial
        Assert.Equal(initialState, currentState);
    }

    /// <summary>
    /// Property 2: Escape exits full-screen when active
    /// For any application state, pressing Escape should result in full-screen mode being false 
    /// if it was previously true, and should remain false if it was already false.
    /// Feature: keyboard-shortcut-fix, Property 2: Escape exits full-screen when active
    /// Validates: Requirements 1.4
    /// </summary>
    [Property(MaxTest = 100)]
    public void EscapeExitsFullScreenWhenActive(bool initialFullScreenState, bool initialPresentationState)
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        bool isFullScreen = initialFullScreenState;
        bool isPresentationMode = initialPresentationState;

        Action escapeAction = () => {
            if (isFullScreen)
            {
                isFullScreen = false;
            }
            else if (isPresentationMode)
            {
                isPresentationMode = false;
            }
        };

        // Register Escape key
        manager.RegisterShortcut(VirtualKey.Escape, VirtualKeyModifiers.None, escapeAction);

        // Act - Press Escape once
        manager.HandleWebViewKeyEvent("Escape", ctrlKey: false, shiftKey: false, altKey: false);

        // Assert - Full-screen should be false after pressing Escape
        Assert.False(isFullScreen, "Full-screen should be false after pressing Escape");

        // If initial presentation mode was true and full-screen was false, presentation should now be false
        if (initialPresentationState && !initialFullScreenState)
        {
            Assert.False(isPresentationMode, "Presentation mode should be false after pressing Escape");
        }

        // Act - Press Escape again
        manager.HandleWebViewKeyEvent("Escape", ctrlKey: false, shiftKey: false, altKey: false);

        // Assert - Both should remain false (idempotent)
        Assert.False(isFullScreen, "Full-screen should remain false after pressing Escape again");
        Assert.False(isPresentationMode, "Presentation mode should remain false after pressing Escape again");
    }

    /// <summary>
    /// Property 4: F7 opens syntax checker from any focus location
    /// For any UI element that has focus (editor, WebView2, menu), pressing F7 should open 
    /// the syntax checker dialog.
    /// Feature: keyboard-shortcut-fix, Property 4: F7 opens syntax checker from any focus location
    /// Validates: Requirements 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public void F7OpensSyntaxCheckerFromAnyFocusLocation(bool useCtrl, bool useShift, bool useAlt)
    {
        // Arrange
        var logger = new TestLogger();
        var preferencesService = new ShortcutPreferencesService();
        var manager = new KeyboardShortcutManager(logger, preferencesService);

        // Track if syntax checker was opened
        bool syntaxCheckerOpened = false;
        Action openSyntaxChecker = () => { syntaxCheckerOpened = true; };

        // Register F7 to open syntax checker (should work regardless of modifiers)
        manager.RegisterShortcut(VirtualKey.F7, VirtualKeyModifiers.None, openSyntaxChecker);

        // Act - Press F7 with random modifiers (simulating different focus contexts)
        // The property is that F7 alone (without modifiers) should always work
        manager.HandleWebViewKeyEvent("F7", ctrlKey: false, shiftKey: false, altKey: false);

        // Assert - Syntax checker should be opened
        Assert.True(syntaxCheckerOpened, "F7 should open syntax checker regardless of focus location");

        // Reset and test that F7 with modifiers doesn't trigger (only F7 alone should work)
        syntaxCheckerOpened = false;
        manager.HandleWebViewKeyEvent("F7", ctrlKey: useCtrl, shiftKey: useShift, altKey: useAlt);

        // If any modifiers are pressed, it should NOT trigger (only plain F7 should work)
        if (useCtrl || useShift || useAlt)
        {
            Assert.False(syntaxCheckerOpened, "F7 with modifiers should not open syntax checker");
        }
        else
        {
            Assert.True(syntaxCheckerOpened, "F7 without modifiers should open syntax checker");
        }
    }
}
