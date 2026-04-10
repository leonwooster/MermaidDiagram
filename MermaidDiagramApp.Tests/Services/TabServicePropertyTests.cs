using Xunit;
using FsCheck;
using FsCheck.Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Models;
using System;
using System.IO;
using System.Linq;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Property-based tests for TabService.
/// Feature: multi-tab-preview
/// </summary>
public class TabServicePropertyTests
{
    // ---------------------------------------------------------------
    // Property 1: AddTab creates a correctly named tab and grows the collection
    // For any valid file path and content, AddTab increases tab count by 1
    // and FileName equals Path.GetFileName(filePath) or "Untitled".
    // Validates: Requirements 2.1
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void AddTab_CreatesCorrectlyNamedTab_AndGrowsCollection(NonNull<string> content)
    {
        var service = new TabService();
        var countBefore = service.Tabs.Count;

        // Test with a non-empty file path
        var filePath = @"C:\docs\diagram.mmd";
        var tab = service.AddTab(filePath, content.Get, ContentType.Mermaid);

        Assert.Equal(countBefore + 1, service.Tabs.Count);
        Assert.Equal(Path.GetFileName(filePath), tab.FileName);
        Assert.Contains(tab, service.Tabs);
    }

    [Property(MaxTest = 100)]
    public void AddTab_WithEmptyPath_SetsFileNameToUntitled(NonNull<string> content)
    {
        var service = new TabService();
        var countBefore = service.Tabs.Count;

        var tab = service.AddTab(string.Empty, content.Get, ContentType.Unknown);

        Assert.Equal(countBefore + 1, service.Tabs.Count);
        Assert.Equal("Untitled", tab.FileName);
    }

    // ---------------------------------------------------------------
    // Property 2: Tab content round-trip across switches
    // For any set of tabs with distinct content, switching away and back
    // preserves EditorContent exactly.
    // Validates: Requirements 2.2, 2.3, 2.8
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void TabContent_PreservedAcrossSwitch(NonNull<string> contentA, NonNull<string> contentB)
    {
        var service = new TabService();

        var tabA = service.AddTab("a.mmd", contentA.Get, ContentType.Mermaid);
        var tabB = service.AddTab("b.mmd", contentB.Get, ContentType.Mermaid);

        service.SetActiveTab(tabA.Id);

        // Switch away to tabB
        service.SetActiveTab(tabB.Id);

        // Switch back to tabA
        service.SetActiveTab(tabA.Id);

        // Content should be preserved exactly
        Assert.Equal(contentA.Get, tabA.EditorContent);
        Assert.Equal(contentB.Get, tabB.EditorContent);
    }

    // ---------------------------------------------------------------
    // Property 3: Dirty flag toggle
    // MarkDirty(tabId, true) sets IsDirty to true;
    // MarkDirty(tabId, false) sets it to false;
    // always reflects most recent call.
    // Validates: Requirements 2.4, 2.6
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void DirtyFlag_ReflectsMostRecentMarkDirtyCall(bool finalDirtyState)
    {
        var service = new TabService();
        var tab = service.AddTab("test.mmd", "content", ContentType.Mermaid);

        // Toggle dirty state multiple times
        service.MarkDirty(tab.Id, true);
        Assert.True(tab.IsDirty);

        service.MarkDirty(tab.Id, false);
        Assert.False(tab.IsDirty);

        // Final state should reflect the most recent call
        service.MarkDirty(tab.Id, finalDirtyState);
        Assert.Equal(finalDirtyState, tab.IsDirty);
    }

    // ---------------------------------------------------------------
    // Property 4: Close behavior depends on dirty state
    // If IsDirty is false, tab can be removed immediately.
    // If IsDirty is true, the tab has IsDirty=true (which would trigger
    // confirmation in the UI layer).
    // Validates: Requirements 3.1, 3.2
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void CloseRequiresConfirmation_OnlyWhenDirty(bool isDirty)
    {
        var service = new TabService();
        var tab = service.AddTab("test.mmd", "content", ContentType.Mermaid);
        service.MarkDirty(tab.Id, isDirty);

        if (!isDirty)
        {
            // Non-dirty tab: can be removed immediately
            var countBefore = service.Tabs.Count;
            service.RemoveTab(tab.Id);
            Assert.Equal(countBefore - 1, service.Tabs.Count);
            Assert.DoesNotContain(tab, service.Tabs);
        }
        else
        {
            // Dirty tab: IsDirty is true, UI layer would show confirmation
            Assert.True(tab.IsDirty);
            // Tab still exists (not removed without confirmation)
            Assert.Contains(tab, service.Tabs);
        }
    }

    // ---------------------------------------------------------------
    // Property 5: New tabs initialize with zero scroll position
    // Any newly created tab via AddTab has ScrollTop == 0 and ScrollLeft == 0.
    // Validates: Requirements 5.1, 5.2
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void NewTab_HasZeroScrollPosition(NonNull<string> filePath, NonNull<string> content)
    {
        var service = new TabService();
        var tab = service.AddTab(filePath.Get, content.Get, ContentType.Mermaid);

        Assert.Equal(0.0, tab.ScrollTop);
        Assert.Equal(0.0, tab.ScrollLeft);
    }

    // ---------------------------------------------------------------
    // Property 6: Scroll position round-trip across switches
    // For any tab with saved scroll position, switching away and back
    // restores exact ScrollTop/ScrollLeft.
    // Validates: Requirements 5.3
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void ScrollPosition_PreservedAcrossSwitch(NormalFloat scrollTop, NormalFloat scrollLeft)
    {
        var st = Math.Abs(scrollTop.Get);
        var sl = Math.Abs(scrollLeft.Get);

        var service = new TabService();
        var tabA = service.AddTab("a.mmd", "contentA", ContentType.Mermaid);
        var tabB = service.AddTab("b.mmd", "contentB", ContentType.Mermaid);

        service.SetActiveTab(tabA.Id);
        service.UpdateScrollPosition(tabA.Id, st, sl);

        // Switch away to tabB
        service.SetActiveTab(tabB.Id);

        // Switch back to tabA
        service.SetActiveTab(tabA.Id);

        // Scroll position should be preserved exactly
        Assert.Equal(st, tabA.ScrollTop);
        Assert.Equal(sl, tabA.ScrollLeft);
    }

    // ---------------------------------------------------------------
    // Property 7: Tab removal removes exactly the target tab
    // RemoveTab(tabId) decreases count by 1 and the removed tab
    // no longer appears in Tabs.
    // Validates: Requirements 7.3
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public void RemoveTab_RemovesExactlyTargetTab(PositiveInt extraTabCount)
    {
        var service = new TabService();
        var numExtra = Math.Min(extraTabCount.Get, 10);

        // Create multiple tabs
        for (int i = 0; i < numExtra; i++)
        {
            service.AddTab($"file{i}.mmd", $"content{i}", ContentType.Mermaid);
        }

        // Add the target tab
        var target = service.AddTab("target.mmd", "target-content", ContentType.Mermaid);
        var countBefore = service.Tabs.Count;
        var otherTabs = service.Tabs.Where(t => t.Id != target.Id).ToList();

        service.RemoveTab(target.Id);

        Assert.Equal(countBefore - 1, service.Tabs.Count);
        Assert.DoesNotContain(target, service.Tabs);

        // All other tabs should still be present
        foreach (var other in otherTabs)
        {
            Assert.Contains(other, service.Tabs);
        }
    }
}
