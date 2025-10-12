namespace MermaidDiagramApp.Models;

/// <summary>
/// Provides context information for rendering operations.
/// </summary>
public class RenderingContext : IRenderingContext
{
    public string FileExtension { get; set; } = string.Empty;
    public ContentType? ForcedContentType { get; set; }
    public bool EnableMermaidInMarkdown { get; set; } = true;
    public ThemeMode Theme { get; set; } = ThemeMode.Light;
    public string FilePath { get; set; } = string.Empty;
    public MarkdownStyleSettings? StyleSettings { get; set; }
}

/// <summary>
/// Theme mode for rendering.
/// </summary>
public enum ThemeMode
{
    Light,
    Dark
}
