namespace MermaidDiagramApp.Models;

/// <summary>
/// Information about Mermaid.js version status.
/// </summary>
public record MermaidVersionInfo(
    string CurrentVersion,
    string LatestVersion,
    bool UpdateAvailable);
