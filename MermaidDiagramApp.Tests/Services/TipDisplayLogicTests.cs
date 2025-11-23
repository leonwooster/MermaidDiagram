using Xunit;
using MermaidDiagramApp.Services;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for tip display logic.
/// Tests that tip shows on first run, doesn't show when preference is false,
/// and that dismissing tip saves preference.
/// </summary>
public class TipDisplayLogicTests
{
    /// <summary>
    /// Test that tip shows on first run (when preference is true/default)
    /// </summary>
    [Fact]
    public void ShouldShowTip_WhenPreferenceIsTrue()
    {
        // Arrange
        var service = new ShortcutPreferencesService();
        service.SetShowTips(true);
        
        // Act
        var shouldShow = service.GetShowTips();
        
        // Assert
        Assert.True(shouldShow);
    }

    /// <summary>
    /// Test that tip doesn't show when preference is false
    /// </summary>
    [Fact]
    public void ShouldNotShowTip_WhenPreferenceIsFalse()
    {
        // Arrange
        var service = new ShortcutPreferencesService();
        service.SetShowTips(false);
        
        // Act
        var shouldShow = service.GetShowTips();
        
        // Assert
        Assert.False(shouldShow);
    }

    /// <summary>
    /// Test that dismissing tip saves preference
    /// </summary>
    [Fact]
    public void DismissingTip_SavesPreference()
    {
        // Arrange
        var service = new ShortcutPreferencesService();
        service.SetShowTips(true); // Initially show tips
        
        // Act - Simulate dismissing the tip
        service.SetShowTips(false);
        var shouldShowAfterDismiss = service.GetShowTips();
        
        // Assert
        Assert.False(shouldShowAfterDismiss);
    }

    /// <summary>
    /// Test that default value is true (show tips on first run)
    /// </summary>
    [Fact]
    public void DefaultValue_IsTrue()
    {
        // Arrange
        var service = new ShortcutPreferencesService();
        service.ClearCache(); // Ensure we get the default value
        
        // Act
        var defaultValue = service.GetShowTips();
        
        // Assert
        Assert.True(defaultValue);
    }

    /// <summary>
    /// Test that preference persists within the same service instance
    /// Note: In test environment, ApplicationData.Current is not available,
    /// so we test persistence within the same instance using cache.
    /// </summary>
    [Fact]
    public void Preference_PersistsWithinInstance()
    {
        // Arrange
        var service = new ShortcutPreferencesService();
        service.SetShowTips(false);
        
        // Act - Check if preference persists within same instance
        var persistedValue = service.GetShowTips();
        
        // Assert
        Assert.False(persistedValue);
        
        // Act - Change preference and verify it persists
        service.SetShowTips(true);
        var updatedValue = service.GetShowTips();
        
        // Assert
        Assert.True(updatedValue);
    }
}
