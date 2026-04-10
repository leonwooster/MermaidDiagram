using Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Models;
using System;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Integration tests for tab switch + zoom panel update behavior.
/// Since the zoom panel requires WinUI runtime, we test at the TabService level
/// to verify that ActiveTabChanged fires correctly when switching tabs,
/// which would trigger zoom panel updates in the UI layer.
/// Validates: Requirements 6.1
/// </summary>
public class TabSwitchZoomPanelTests
{
    /// <summary>
    /// Requirement 6.1: When the user switches to a different tab,
    /// ActiveTabChanged fires with the correct new and previous tab info.
    /// The UI layer uses this event to update the zoom panel.
    /// </summary>
    [Fact]
    public void SwitchingTabs_FiresActiveTabChanged_WithCorrectTabs()
    {
        var service = new TabService();
        var tabA = service.AddTab("diagram1.mmd", "graph TD; A-->B", ContentType.Mermaid);
        var tabB = service.AddTab("diagram2.mmd", "graph LR; X-->Y", ContentType.Mermaid);
        service.SetActiveTab(tabA.Id);

        TabChangedEventArgs? receivedArgs = null;
        service.ActiveTabChanged += (_, args) => receivedArgs = args;

        service.SetActiveTab(tabB.Id);

        Assert.NotNull(receivedArgs);
        Assert.Equal(tabB.Id, receivedArgs!.Tab!.Id);
        Assert.Equal(tabA.Id, receivedArgs.PreviousTab!.Id);
    }

    /// <summary>
    /// Requirement 6.1: Switching tabs updates ActiveTab so the zoom panel
    /// can read the new tab's content for re-rendering.
    /// </summary>
    [Fact]
    public void SwitchingTabs_UpdatesActiveTab_WithNewContent()
    {
        var service = new TabService();
        var tabA = service.AddTab("diagram1.mmd", "graph TD; A-->B", ContentType.Mermaid);
        var tabB = service.AddTab("diagram2.mmd", "graph LR; X-->Y", ContentType.Mermaid);
        service.SetActiveTab(tabA.Id);

        Assert.Equal("graph TD; A-->B", service.ActiveTab!.EditorContent);

        service.SetActiveTab(tabB.Id);

        Assert.Equal("graph LR; X-->Y", service.ActiveTab!.EditorContent);
    }

    /// <summary>
    /// Requirement 6.1: Each tab switch fires ActiveTabChanged exactly once,
    /// ensuring the zoom panel gets a single update per switch.
    /// </summary>
    [Fact]
    public void SwitchingTabs_FiresActiveTabChanged_ExactlyOnce()
    {
        var service = new TabService();
        var tabA = service.AddTab("a.mmd", "contentA", ContentType.Mermaid);
        var tabB = service.AddTab("b.mmd", "contentB", ContentType.Mermaid);
        service.SetActiveTab(tabA.Id);

        int fireCount = 0;
        service.ActiveTabChanged += (_, _) => fireCount++;

        service.SetActiveTab(tabB.Id);

        Assert.Equal(1, fireCount);
    }

    /// <summary>
    /// Requirement 6.1: Switching between multiple tabs fires the event
    /// each time with the correct previous/new tab pair.
    /// </summary>
    [Fact]
    public void MultipleSwitches_FireCorrectEventSequence()
    {
        var service = new TabService();
        var tabA = service.AddTab("a.mmd", "contentA", ContentType.Mermaid);
        var tabB = service.AddTab("b.mmd", "contentB", ContentType.Mermaid);
        var tabC = service.AddTab("c.mmd", "contentC", ContentType.Mermaid);
        service.SetActiveTab(tabA.Id);

        TabChangedEventArgs? lastArgs = null;
        service.ActiveTabChanged += (_, args) => lastArgs = args;

        // Switch A -> B
        service.SetActiveTab(tabB.Id);
        Assert.Equal(tabB.Id, lastArgs!.Tab!.Id);
        Assert.Equal(tabA.Id, lastArgs.PreviousTab!.Id);

        // Switch B -> C
        service.SetActiveTab(tabC.Id);
        Assert.Equal(tabC.Id, lastArgs!.Tab!.Id);
        Assert.Equal(tabB.Id, lastArgs.PreviousTab!.Id);

        // Switch C -> A
        service.SetActiveTab(tabA.Id);
        Assert.Equal(tabA.Id, lastArgs!.Tab!.Id);
        Assert.Equal(tabC.Id, lastArgs.PreviousTab!.Id);
    }
}
