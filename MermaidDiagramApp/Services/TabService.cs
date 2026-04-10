using System;
using System.Collections.Generic;
using System.Linq;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Manages the collection of open tabs and the active tab pointer.
/// </summary>
public class TabService : ITabService
{
    private readonly List<TabState> _tabs = new();
    private Guid? _activeTabId;

    public IReadOnlyList<TabState> Tabs => _tabs.AsReadOnly();

    public TabState? ActiveTab => _activeTabId.HasValue
        ? _tabs.FirstOrDefault(t => t.Id == _activeTabId.Value)
        : null;

    public event EventHandler<TabChangedEventArgs>? ActiveTabChanged;
    public event EventHandler<TabChangedEventArgs>? TabClosed;
    public event EventHandler<TabChangedEventArgs>? TabAdded;

    public TabState AddTab(string filePath, string content, ContentType contentType)
    {
        var tab = new TabState
        {
            FilePath = filePath,
            EditorContent = content,
            ContentType = contentType,
            ScrollTop = 0,
            ScrollLeft = 0
        };

        _tabs.Add(tab);
        TabAdded?.Invoke(this, new TabChangedEventArgs { Tab = tab });

        return tab;
    }

    public void RemoveTab(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null) return;

        var index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        TabClosed?.Invoke(this, new TabChangedEventArgs { Tab = tab });

        // If the removed tab was active, select an adjacent tab
        if (_activeTabId == tabId)
        {
            if (_tabs.Count == 0)
            {
                _activeTabId = null;
                ActiveTabChanged?.Invoke(this, new TabChangedEventArgs
                {
                    Tab = null,
                    PreviousTab = tab
                });
            }
            else
            {
                // Pick the tab at the same index, or the last tab if we removed the last one
                var newIndex = Math.Min(index, _tabs.Count - 1);
                var newActiveTab = _tabs[newIndex];
                _activeTabId = newActiveTab.Id;
                ActiveTabChanged?.Invoke(this, new TabChangedEventArgs
                {
                    Tab = newActiveTab,
                    PreviousTab = tab
                });
            }
        }
    }

    public void SetActiveTab(Guid tabId)
    {
        var newTab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (newTab is null) return;

        var previousTab = ActiveTab;
        _activeTabId = tabId;

        ActiveTabChanged?.Invoke(this, new TabChangedEventArgs
        {
            Tab = newTab,
            PreviousTab = previousTab
        });
    }

    public TabState? FindTabByFilePath(string filePath)
    {
        return _tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
    }

    public void UpdateTabContent(Guid tabId, string content)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is not null)
        {
            tab.EditorContent = content;
        }
    }

    public void MarkDirty(Guid tabId, bool isDirty)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is not null)
        {
            tab.IsDirty = isDirty;
        }
    }

    public void UpdateScrollPosition(Guid tabId, double scrollTop, double scrollLeft)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is not null)
        {
            tab.ScrollTop = scrollTop;
            tab.ScrollLeft = scrollLeft;
        }
    }
}
