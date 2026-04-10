using Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Models;
using System;
using System.Linq;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for tab close dialog logic at the TabService level.
/// Since the actual dialog is a WinUI ContentDialog that can't be unit tested directly,
/// these tests validate the TabService-level behavior that drives the close flow.
/// </summary>
public class TabCloseDialogTests
{
    /// <summary>
    /// Requirement 3.1: When a tab is marked dirty, IsDirty is true,
    /// which would trigger the save dialog in the UI layer.
    /// </summary>
    [Fact]
    public void DirtyTab_HasIsDirtyTrue()
    {
        var service = new TabService();
        var tab = service.AddTab("diagram.mmd", "graph TD; A-->B", ContentType.Mermaid);

        service.MarkDirty(tab.Id, true);

        Assert.True(tab.IsDirty);
    }

    /// <summary>
    /// Requirement 3.2: When a tab has IsDirty=false, RemoveTab removes it
    /// immediately and the tab is no longer in the collection.
    /// </summary>
    [Fact]
    public void CleanTab_ClosesWithoutDialog()
    {
        var service = new TabService();
        var tab = service.AddTab("diagram.mmd", "graph TD; A-->B", ContentType.Mermaid);

        Assert.False(tab.IsDirty);

        service.RemoveTab(tab.Id);

        Assert.DoesNotContain(tab, service.Tabs);
    }

    /// <summary>
    /// Requirement 3.3: After saving (MarkDirty false), RemoveTab succeeds
    /// and tab is removed. Simulates the Save option in the dialog.
    /// </summary>
    [Fact]
    public void SaveOption_SavesFileThenClosesTab()
    {
        var service = new TabService();
        var tab = service.AddTab("diagram.mmd", "graph TD; A-->B", ContentType.Mermaid);
        service.MarkDirty(tab.Id, true);
        Assert.True(tab.IsDirty);

        // Simulate save: clear dirty flag then remove
        service.MarkDirty(tab.Id, false);
        Assert.False(tab.IsDirty);

        service.RemoveTab(tab.Id);

        Assert.DoesNotContain(tab, service.Tabs);
    }

    /// <summary>
    /// Requirement 3.4: RemoveTab on a dirty tab removes it.
    /// Simulates discard — the UI layer would call RemoveTab after user confirms discard.
    /// </summary>
    [Fact]
    public void DiscardOption_ClosesTabWithoutSaving()
    {
        var service = new TabService();
        var tab = service.AddTab("diagram.mmd", "graph TD; A-->B", ContentType.Mermaid);
        service.MarkDirty(tab.Id, true);
        Assert.True(tab.IsDirty);

        // Simulate discard: remove without clearing dirty flag
        service.RemoveTab(tab.Id);

        Assert.DoesNotContain(tab, service.Tabs);
    }

    /// <summary>
    /// Requirement 3.5: If we don't call RemoveTab (simulating cancel),
    /// the tab remains in the collection.
    /// </summary>
    [Fact]
    public void CancelOption_KeepsTabOpen()
    {
        var service = new TabService();
        var tab = service.AddTab("diagram.mmd", "graph TD; A-->B", ContentType.Mermaid);
        service.MarkDirty(tab.Id, true);

        // Simulate cancel: do nothing (don't call RemoveTab)

        Assert.Contains(tab, service.Tabs);
        Assert.True(tab.IsDirty);
    }

    /// <summary>
    /// Requirement 3.6: When the last tab is removed, ActiveTab becomes null
    /// and Tabs is empty.
    /// </summary>
    [Fact]
    public void ClosingLastTab_ClearsState()
    {
        var service = new TabService();
        var tab = service.AddTab("diagram.mmd", "graph TD; A-->B", ContentType.Mermaid);
        service.SetActiveTab(tab.Id);
        Assert.NotNull(service.ActiveTab);

        service.RemoveTab(tab.Id);

        Assert.Null(service.ActiveTab);
        Assert.Empty(service.Tabs);
    }
}
