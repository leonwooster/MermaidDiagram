namespace MermaidDiagramApp.Models;

/// <summary>
/// Result of a search operation within the code editor.
/// </summary>
public record SearchResult(
    bool Found,
    int MatchIndex,
    int MatchLength,
    string StatusMessage);
