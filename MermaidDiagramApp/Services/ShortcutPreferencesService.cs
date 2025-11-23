using System;
using Windows.Storage;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Service for persisting and retrieving keyboard shortcut tip preferences.
/// </summary>
public class ShortcutPreferencesService
{
    private const string ShowTipsKey = "ShowKeyboardShortcutTips";
    private const bool DefaultShowTips = true;

    private readonly ApplicationDataContainer? _localSettings;
    private bool? _cachedShowTips;

    public ShortcutPreferencesService()
    {
        try
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }
        catch (InvalidOperationException)
        {
            // ApplicationData.Current is not available in test context
            _localSettings = null;
        }
    }

    /// <summary>
    /// Constructor for testing that accepts a custom settings container.
    /// </summary>
    internal ShortcutPreferencesService(ApplicationDataContainer? settingsContainer)
    {
        _localSettings = settingsContainer;
    }

    /// <summary>
    /// Gets whether to show keyboard shortcut tips.
    /// Returns true by default if no preference has been set.
    /// </summary>
    public bool GetShowTips()
    {
        if (_cachedShowTips.HasValue)
        {
            return _cachedShowTips.Value;
        }

        if (_localSettings == null)
        {
            // No settings available (test context), return default
            _cachedShowTips = DefaultShowTips;
            return _cachedShowTips.Value;
        }

        try
        {
            if (_localSettings.Values.TryGetValue(ShowTipsKey, out var value))
            {
                _cachedShowTips = Convert.ToBoolean(value);
                return _cachedShowTips.Value;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading keyboard shortcut tip preference: {ex.Message}");
        }

        // Return default value if not set or error occurred
        _cachedShowTips = DefaultShowTips;
        return _cachedShowTips.Value;
    }

    /// <summary>
    /// Sets whether to show keyboard shortcut tips.
    /// </summary>
    public void SetShowTips(bool show)
    {
        _cachedShowTips = show;

        if (_localSettings == null)
        {
            // No settings available (test context), only update cache
            return;
        }

        try
        {
            _localSettings.Values[ShowTipsKey] = show;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving keyboard shortcut tip preference: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Clears the cached preference, forcing a reload on next access.
    /// </summary>
    public void ClearCache()
    {
        _cachedShowTips = null;
    }
}
