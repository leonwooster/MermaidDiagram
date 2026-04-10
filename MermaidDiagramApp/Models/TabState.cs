using System;
using System.IO;

namespace MermaidDiagramApp.Models;

public class TabState
{
    public Guid Id { get; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public string FileName => string.IsNullOrEmpty(FilePath)
        ? "Untitled"
        : Path.GetFileName(FilePath);
    public string EditorContent { get; set; } = string.Empty;
    public ContentType ContentType { get; set; } = ContentType.Unknown;
    public bool IsDirty { get; set; }
    public double ScrollTop { get; set; }
    public double ScrollLeft { get; set; }
}
