using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services;

namespace MermaidDiagramApp.Views;

public sealed partial class MarkdownStyleSettingsDialog : ContentDialog
{
    private readonly MarkdownStyleSettingsService _settingsService;
    private MarkdownStyleSettings _currentSettings;
    private bool _isInitializing = true;

    public MarkdownStyleSettings Settings => _currentSettings;

    public MarkdownStyleSettingsDialog()
    {
        this.InitializeComponent();
        _settingsService = new MarkdownStyleSettingsService();
        _currentSettings = _settingsService.LoadSettings();
        LoadSettings();
        _isInitializing = false;
        UpdatePreview();
    }

    private void LoadSettings()
    {
        FontSizeSlider.Value = _currentSettings.FontSize;
        LineHeightSlider.Value = _currentSettings.LineHeight;
        MaxWidthSlider.Value = _currentSettings.MaxContentWidth;
        CodeFontSizeSlider.Value = _currentSettings.CodeFontSize;

        // Set font family combo box
        SetFontFamilySelection(_currentSettings.FontFamily, FontFamilyComboBox);
        SetCodeFontFamilySelection(_currentSettings.CodeFontFamily, CodeFontFamilyComboBox);
    }

    private void SetFontFamilySelection(string fontFamily, ComboBox comboBox)
    {
        if (fontFamily.Contains("-apple-system") || fontFamily.Contains("BlinkMacSystemFont"))
        {
            comboBox.SelectedIndex = 0;
        }
        else if (fontFamily.Contains("Segoe UI"))
        {
            comboBox.SelectedIndex = 1;
        }
        else if (fontFamily.Contains("Arial"))
        {
            comboBox.SelectedIndex = 2;
        }
        else if (fontFamily.Contains("Georgia"))
        {
            comboBox.SelectedIndex = 3;
        }
        else if (fontFamily.Contains("Times New Roman"))
        {
            comboBox.SelectedIndex = 4;
        }
        else if (fontFamily.Contains("Verdana"))
        {
            comboBox.SelectedIndex = 5;
        }
        else if (fontFamily.Contains("Calibri"))
        {
            comboBox.SelectedIndex = 6;
        }
        else
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private void SetCodeFontFamilySelection(string fontFamily, ComboBox comboBox)
    {
        if (fontFamily.Contains("Consolas") && fontFamily.Contains("Monaco"))
        {
            comboBox.SelectedIndex = 0;
        }
        else if (fontFamily.Contains("Consolas"))
        {
            comboBox.SelectedIndex = 1;
        }
        else if (fontFamily.Contains("Monaco"))
        {
            comboBox.SelectedIndex = 2;
        }
        else if (fontFamily.Contains("Courier New"))
        {
            comboBox.SelectedIndex = 3;
        }
        else if (fontFamily.Contains("Fira Code"))
        {
            comboBox.SelectedIndex = 4;
        }
        else if (fontFamily.Contains("Source Code Pro"))
        {
            comboBox.SelectedIndex = 5;
        }
        else
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private string GetSelectedFontFamily()
    {
        return FontFamilyComboBox.SelectedIndex switch
        {
            0 => "-apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif",
            1 => "'Segoe UI', sans-serif",
            2 => "Arial, sans-serif",
            3 => "Georgia, serif",
            4 => "'Times New Roman', serif",
            5 => "Verdana, sans-serif",
            6 => "Calibri, sans-serif",
            _ => "-apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif"
        };
    }

    private string GetSelectedCodeFontFamily()
    {
        return CodeFontFamilyComboBox.SelectedIndex switch
        {
            0 => "'Consolas', 'Monaco', 'Courier New', monospace",
            1 => "'Consolas', monospace",
            2 => "'Monaco', monospace",
            3 => "'Courier New', monospace",
            4 => "'Fira Code', monospace",
            5 => "'Source Code Pro', monospace",
            _ => "'Consolas', 'Monaco', 'Courier New', monospace"
        };
    }

    private void UpdateCurrentSettings()
    {
        _currentSettings = new MarkdownStyleSettings
        {
            FontSize = (int)FontSizeSlider.Value,
            FontFamily = GetSelectedFontFamily(),
            LineHeight = LineHeightSlider.Value,
            MaxContentWidth = (int)MaxWidthSlider.Value,
            CodeFontFamily = GetSelectedCodeFontFamily(),
            CodeFontSize = (int)CodeFontSizeSlider.Value
        };
    }

    private void UpdatePreview()
    {
        if (_isInitializing) return;

        UpdateCurrentSettings();

        // Update preview text styling
        PreviewText.FontSize = _currentSettings.FontSize;
        PreviewText.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(_currentSettings.FontFamily.Split(',')[0].Trim().Trim('\''));
        PreviewText.LineHeight = _currentSettings.LineHeight * _currentSettings.FontSize;

        // Update preview code styling
        PreviewCode.FontSize = _currentSettings.CodeFontSize;
        PreviewCode.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(_currentSettings.CodeFontFamily.Split(',')[0].Trim().Trim('\''));
    }

    private void FontSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void LineHeightSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void MaxWidthSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void CodeFontSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void SaveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        UpdateCurrentSettings();
        _settingsService.SaveSettings(_currentSettings);
    }

    private void CancelButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // No action needed, dialog will close
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _currentSettings = MarkdownStyleSettings.Default;
        LoadSettings();
        UpdatePreview();
    }
}
