using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Encapsulates find-next, find-previous, and search state management logic.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Gets the current search text.
    /// </summary>
    string CurrentSearchText { get; }

    /// <summary>
    /// Sets the current search text, resetting the search position if the text changes.
    /// </summary>
    void SetSearchText(string text);

    /// <summary>
    /// Finds the next occurrence of the search text within the editor content.
    /// </summary>
    SearchResult FindNext(string text, string editorContent);

    /// <summary>
    /// Finds the previous occurrence of the search text within the editor content.
    /// </summary>
    SearchResult FindPrevious(string text, string editorContent);

    /// <summary>
    /// Resets all search state (current text and position).
    /// </summary>
    void Reset();
}
