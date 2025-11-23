namespace MermaidDiagramApp.Models;

/// <summary>
/// Represents a keyboard event message sent from WebView2 JavaScript.
/// </summary>
public class KeyboardEventMessage
{
    /// <summary>
    /// The message type (should be "keypress" for keyboard events).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The key that was pressed (e.g., "F11", "Escape", "F7").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Whether the Ctrl key was pressed.
    /// </summary>
    public bool CtrlKey { get; set; }

    /// <summary>
    /// Whether the Shift key was pressed.
    /// </summary>
    public bool ShiftKey { get; set; }

    /// <summary>
    /// Whether the Alt key was pressed.
    /// </summary>
    public bool AltKey { get; set; }
}
