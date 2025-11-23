using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Centralized management of keyboard shortcuts with fallback handling and user feedback.
/// </summary>
public class KeyboardShortcutManager
{
    private readonly Dictionary<string, Action> _shortcuts;
    private readonly ILogger _logger;
    private readonly ShortcutPreferencesService _preferencesService;

    public KeyboardShortcutManager(ILogger logger, ShortcutPreferencesService preferencesService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        _shortcuts = new Dictionary<string, Action>();
    }

    /// <summary>
    /// Registers a keyboard shortcut with its action.
    /// </summary>
    /// <param name="key">The virtual key to register</param>
    /// <param name="modifiers">The modifier keys (Ctrl, Shift, Alt)</param>
    /// <param name="action">The action to execute when the shortcut is pressed</param>
    public void RegisterShortcut(VirtualKey key, VirtualKeyModifiers modifiers, Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var shortcutKey = GetShortcutKey(key, modifiers);
        
        if (_shortcuts.ContainsKey(shortcutKey))
        {
            _logger.Log(LogLevel.Warning, $"Shortcut {shortcutKey} is already registered. Overwriting.");
        }

        _shortcuts[shortcutKey] = action;
        _logger.Log(LogLevel.Debug, $"Registered keyboard shortcut: {shortcutKey}");
        
        // Log warning for shortcuts that may be intercepted by the system
        if (key == VirtualKey.F11 && modifiers == VirtualKeyModifiers.None)
        {
            _logger.Log(LogLevel.Debug, $"Note: F11 may be intercepted by Windows system shortcuts. Ctrl+F11 is recommended as an alternative.");
        }
    }

    /// <summary>
    /// Handles key down events from MainWindow.
    /// </summary>
    /// <param name="e">The keyboard event arguments</param>
    /// <returns>True if the shortcut was handled, false otherwise</returns>
    public bool HandleKeyDown(KeyRoutedEventArgs e)
    {
        if (e == null)
        {
            return false;
        }

        var key = e.Key;
        var modifiers = GetCurrentModifiers();
        var shortcutKey = GetShortcutKey(key, modifiers);

        _logger.Log(LogLevel.Debug, $"Key down event received: {shortcutKey}");

        if (_shortcuts.TryGetValue(shortcutKey, out var action))
        {
            _logger.Log(LogLevel.Debug, $"Triggering keyboard shortcut: {shortcutKey}");
            
            try
            {
                action.Invoke();
                e.Handled = true;
                _logger.Log(LogLevel.Debug, $"Keyboard shortcut executed successfully: {shortcutKey}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Error executing keyboard shortcut {shortcutKey}", ex);
                return false;
            }
        }
        else
        {
            _logger.Log(LogLevel.Debug, $"No registered shortcut found for: {shortcutKey}");
        }

        return false;
    }

    /// <summary>
    /// Handles key events forwarded from WebView2.
    /// </summary>
    /// <param name="key">The key name (e.g., "F11", "Escape")</param>
    /// <param name="ctrlKey">Whether Ctrl key is pressed</param>
    /// <param name="shiftKey">Whether Shift key is pressed</param>
    /// <param name="altKey">Whether Alt key is pressed</param>
    /// <returns>True if the shortcut was handled, false otherwise</returns>
    public bool HandleWebViewKeyEvent(string key, bool ctrlKey, bool shiftKey, bool altKey)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.Log(LogLevel.Debug, "WebView2 keyboard event received with empty key");
            return false;
        }

        _logger.Log(LogLevel.Debug, $"WebView2 forwarded keyboard event: Key={key}, Ctrl={ctrlKey}, Shift={shiftKey}, Alt={altKey}");

        // Convert string key to VirtualKey
        if (!TryParseVirtualKey(key, out var virtualKey))
        {
            _logger.Log(LogLevel.Warning, $"Unable to parse key from WebView2: {key}");
            return false;
        }

        // Build modifiers
        var modifiers = VirtualKeyModifiers.None;
        if (ctrlKey) modifiers |= VirtualKeyModifiers.Control;
        if (shiftKey) modifiers |= VirtualKeyModifiers.Shift;
        if (altKey) modifiers |= VirtualKeyModifiers.Menu;

        var shortcutKey = GetShortcutKey(virtualKey, modifiers);

        if (_shortcuts.TryGetValue(shortcutKey, out var action))
        {
            _logger.Log(LogLevel.Debug, $"Triggering keyboard shortcut from WebView2: {shortcutKey}");
            
            try
            {
                action.Invoke();
                _logger.Log(LogLevel.Debug, $"Keyboard shortcut from WebView2 executed successfully: {shortcutKey}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Error executing keyboard shortcut {shortcutKey} from WebView2", ex);
                return false;
            }
        }
        else
        {
            _logger.Log(LogLevel.Debug, $"No registered shortcut found for WebView2 event: {shortcutKey}");
        }

        return false;
    }

    /// <summary>
    /// Gets the current modifier key states.
    /// </summary>
    private VirtualKeyModifiers GetCurrentModifiers()
    {
        var modifiers = VirtualKeyModifiers.None;
        
        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);

        if ((ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
        {
            modifiers |= VirtualKeyModifiers.Control;
        }
        
        if ((shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
        {
            modifiers |= VirtualKeyModifiers.Shift;
        }
        
        if ((altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
        {
            modifiers |= VirtualKeyModifiers.Menu;
        }

        return modifiers;
    }

    /// <summary>
    /// Creates a unique key for the shortcut dictionary.
    /// </summary>
    private string GetShortcutKey(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        return $"{modifiers}+{key}";
    }

    /// <summary>
    /// Tries to parse a string key name to a VirtualKey.
    /// </summary>
    private bool TryParseVirtualKey(string key, out VirtualKey virtualKey)
    {
        // Handle common key names
        switch (key.ToUpperInvariant())
        {
            case "F11":
                virtualKey = VirtualKey.F11;
                return true;
            case "F7":
                virtualKey = VirtualKey.F7;
                return true;
            case "F5":
                virtualKey = VirtualKey.F5;
                return true;
            case "ESCAPE":
            case "ESC":
                virtualKey = VirtualKey.Escape;
                return true;
            default:
                // Try to parse as enum
                if (Enum.TryParse<VirtualKey>(key, true, out virtualKey))
                {
                    return true;
                }
                virtualKey = VirtualKey.None;
                return false;
        }
    }
}
