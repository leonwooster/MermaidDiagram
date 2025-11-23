using System;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MermaidDiagramApp.Models;

/// <summary>
/// Represents a keyboard shortcut definition with its associated action.
/// </summary>
public class ShortcutDefinition
{
    /// <summary>
    /// The virtual key for this shortcut (e.g., VirtualKey.F11).
    /// </summary>
    public VirtualKey Key { get; set; }

    /// <summary>
    /// The modifier keys required for this shortcut (e.g., VirtualKeyModifiers.Control).
    /// </summary>
    public VirtualKeyModifiers Modifiers { get; set; }

    /// <summary>
    /// The display name for this shortcut (e.g., "Full Screen Preview").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The action to execute when this shortcut is triggered.
    /// </summary>
    public Action? Action { get; set; }

    /// <summary>
    /// A description of what this shortcut does.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
