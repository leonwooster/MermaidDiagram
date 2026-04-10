using System;
using System.Collections.Generic;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Manages the ordered collection of open tabs and the active tab pointer.
/// Registered as a singleton in the DI container.
/// </summary>
public interface ITabService
{
    /// <summary>
    /// Gets the read-only list of currently open tabs.
    /// </summary>
    IReadOnlyList<TabState> Tabs { get; }

    /// <summary>
    /// Gets the currently active tab, or null if no tabs are open.
    /// </summary>
    TabState? ActiveTab { get; }

    /// <summary>
    /// Creates a new tab for the given file and appends it to the tab collection.
    /// Scroll position is initialized to (0, 0).
    /// </summary>
    TabState AddTab(string filePath, string content, ContentType contentType);

    /// <summary>
    /// Removes the tab with the specified ID from the collection.
    /// If the removed tab was active, an adjacent tab is selected.
    /// </summary>
    void RemoveTab(Guid tabId);

    /// <summary>
    /// Sets the tab with the specified ID as the active tab.
    /// </summary>
    void SetActiveTab(Guid tabId);

    /// <summary>
    /// Finds an existing tab by file path using case-insensitive comparison.
    /// Returns null if no matching tab is found.
    /// </summary>
    TabState? FindTabByFilePath(string filePath);

    /// <summary>
    /// Updates the editor content for the specified tab.
    /// </summary>
    void UpdateTabContent(Guid tabId, string content);

    /// <summary>
    /// Sets or clears the dirty flag on the specified tab.
    /// </summary>
    void MarkDirty(Guid tabId, bool isDirty);

    /// <summary>
    /// Updates the scroll position for the specified tab.
    /// </summary>
    void UpdateScrollPosition(Guid tabId, double scrollTop, double scrollLeft);

    /// <summary>
    /// Raised when the active tab changes. Includes the previous and new active tab.
    /// </summary>
    event EventHandler<TabChangedEventArgs>? ActiveTabChanged;

    /// <summary>
    /// Raised when a tab is closed and removed from the collection.
    /// </summary>
    event EventHandler<TabChangedEventArgs>? TabClosed;

    /// <summary>
    /// Raised when a new tab is added to the collection.
    /// </summary>
    event EventHandler<TabChangedEventArgs>? TabAdded;
}
