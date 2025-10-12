using System;

namespace MermaidDiagramApp.Models;

/// <summary>
/// User-configurable settings for Markdown rendering style.
/// </summary>
public class MarkdownStyleSettings
{
    /// <summary>
    /// Font size for Markdown content in pixels.
    /// </summary>
    public int FontSize { get; set; } = 16;

    /// <summary>
    /// Font family for Markdown content.
    /// </summary>
    public string FontFamily { get; set; } = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif";

    /// <summary>
    /// Line height multiplier for better readability.
    /// </summary>
    public double LineHeight { get; set; } = 1.6;

    /// <summary>
    /// Maximum content width in pixels (0 = no limit).
    /// </summary>
    public int MaxContentWidth { get; set; } = 900;

    /// <summary>
    /// Code block font family.
    /// </summary>
    public string CodeFontFamily { get; set; } = "'Consolas', 'Monaco', 'Courier New', monospace";

    /// <summary>
    /// Code block font size in pixels.
    /// </summary>
    public int CodeFontSize { get; set; } = 14;

    /// <summary>
    /// Validates the settings and returns a corrected copy if needed.
    /// </summary>
    public MarkdownStyleSettings Validate()
    {
        var validated = new MarkdownStyleSettings
        {
            FontSize = Math.Clamp(FontSize, 10, 32),
            FontFamily = string.IsNullOrWhiteSpace(FontFamily) 
                ? "-apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif" 
                : FontFamily,
            LineHeight = Math.Clamp(LineHeight, 1.0, 3.0),
            MaxContentWidth = Math.Clamp(MaxContentWidth, 0, 2000),
            CodeFontFamily = string.IsNullOrWhiteSpace(CodeFontFamily)
                ? "'Consolas', 'Monaco', 'Courier New', monospace"
                : CodeFontFamily,
            CodeFontSize = Math.Clamp(CodeFontSize, 8, 24)
        };

        return validated;
    }

    /// <summary>
    /// Creates a default settings instance.
    /// </summary>
    public static MarkdownStyleSettings Default => new();

    /// <summary>
    /// Generates CSS variables string for WebView2.
    /// </summary>
    public string ToCssVariables()
    {
        return $@"
            --md-font-size: {FontSize}px;
            --md-font-family: {FontFamily};
            --md-line-height: {LineHeight};
            --md-max-width: {(MaxContentWidth > 0 ? $"{MaxContentWidth}px" : "100%")};
            --md-code-font-family: {CodeFontFamily};
            --md-code-font-size: {CodeFontSize}px;
        ";
    }
}
