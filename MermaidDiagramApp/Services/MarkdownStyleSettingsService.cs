using System;
using Windows.Storage;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Service for persisting and retrieving Markdown style settings.
/// </summary>
public class MarkdownStyleSettingsService
{
    private const string FontSizeKey = "MarkdownFontSize";
    private const string FontFamilyKey = "MarkdownFontFamily";
    private const string LineHeightKey = "MarkdownLineHeight";
    private const string MaxContentWidthKey = "MarkdownMaxContentWidth";
    private const string CodeFontFamilyKey = "MarkdownCodeFontFamily";
    private const string CodeFontSizeKey = "MarkdownCodeFontSize";

    private readonly ApplicationDataContainer _localSettings;
    private MarkdownStyleSettings? _cachedSettings;

    public MarkdownStyleSettingsService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    /// <summary>
    /// Loads settings from local storage or returns defaults.
    /// </summary>
    public MarkdownStyleSettings LoadSettings()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        var settings = new MarkdownStyleSettings();

        try
        {
            if (_localSettings.Values.TryGetValue(FontSizeKey, out var fontSize))
            {
                settings.FontSize = Convert.ToInt32(fontSize);
            }

            if (_localSettings.Values.TryGetValue(FontFamilyKey, out var fontFamily))
            {
                settings.FontFamily = fontFamily.ToString() ?? settings.FontFamily;
            }

            if (_localSettings.Values.TryGetValue(LineHeightKey, out var lineHeight))
            {
                settings.LineHeight = Convert.ToDouble(lineHeight);
            }

            if (_localSettings.Values.TryGetValue(MaxContentWidthKey, out var maxWidth))
            {
                settings.MaxContentWidth = Convert.ToInt32(maxWidth);
            }

            if (_localSettings.Values.TryGetValue(CodeFontFamilyKey, out var codeFontFamily))
            {
                settings.CodeFontFamily = codeFontFamily.ToString() ?? settings.CodeFontFamily;
            }

            if (_localSettings.Values.TryGetValue(CodeFontSizeKey, out var codeFontSize))
            {
                settings.CodeFontSize = Convert.ToInt32(codeFontSize);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading Markdown style settings: {ex.Message}");
            settings = MarkdownStyleSettings.Default;
        }

        // Validate and cache
        _cachedSettings = settings.Validate();
        return _cachedSettings;
    }

    /// <summary>
    /// Saves settings to local storage.
    /// </summary>
    public void SaveSettings(MarkdownStyleSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        try
        {
            var validated = settings.Validate();

            _localSettings.Values[FontSizeKey] = validated.FontSize;
            _localSettings.Values[FontFamilyKey] = validated.FontFamily;
            _localSettings.Values[LineHeightKey] = validated.LineHeight;
            _localSettings.Values[MaxContentWidthKey] = validated.MaxContentWidth;
            _localSettings.Values[CodeFontFamilyKey] = validated.CodeFontFamily;
            _localSettings.Values[CodeFontSizeKey] = validated.CodeFontSize;

            _cachedSettings = validated;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving Markdown style settings: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        SaveSettings(MarkdownStyleSettings.Default);
    }

    /// <summary>
    /// Clears the cached settings, forcing a reload on next access.
    /// </summary>
    public void ClearCache()
    {
        _cachedSettings = null;
    }
}
