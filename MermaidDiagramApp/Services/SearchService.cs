using System;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Manages search state and provides case-insensitive find-next/find-previous
/// operations over editor content, independent of the UI layer.
/// </summary>
public class SearchService : ISearchService
{
    private int _currentPosition;

    public string CurrentSearchText { get; private set; } = string.Empty;

    public void SetSearchText(string text)
    {
        if (text != CurrentSearchText)
        {
            CurrentSearchText = text ?? string.Empty;
            _currentPosition = 0;
        }
    }

    public SearchResult FindNext(string text, string editorContent)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(editorContent))
        {
            return new SearchResult(false, -1, 0, string.Empty);
        }

        // If the search text changed, reset position
        if (text != CurrentSearchText)
        {
            CurrentSearchText = text;
            _currentPosition = 0;
        }

        int index = editorContent.IndexOf(text, _currentPosition, StringComparison.OrdinalIgnoreCase);

        // Wrap around if not found from current position
        if (index < 0 && _currentPosition > 0)
        {
            index = editorContent.IndexOf(text, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (index >= 0)
        {
            _currentPosition = index + text.Length;
            return new SearchResult(true, index, text.Length, $"Found at position {index}");
        }

        _currentPosition = 0;
        return new SearchResult(false, -1, 0, "No matches found");
    }

    public SearchResult FindPrevious(string text, string editorContent)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(editorContent))
        {
            return new SearchResult(false, -1, 0, string.Empty);
        }

        // If the search text changed, reset position to end
        if (text != CurrentSearchText)
        {
            CurrentSearchText = text;
            _currentPosition = editorContent.Length;
        }

        // Search backwards from just before the current position
        int searchFrom = Math.Max(0, _currentPosition - text.Length);
        int index = editorContent.LastIndexOf(text, searchFrom, StringComparison.OrdinalIgnoreCase);

        // Wrap around if not found before current position
        if (index < 0)
        {
            index = editorContent.LastIndexOf(text, editorContent.Length - 1, StringComparison.OrdinalIgnoreCase);
        }

        if (index >= 0)
        {
            _currentPosition = index;
            return new SearchResult(true, index, text.Length, $"Found at position {index}");
        }

        _currentPosition = 0;
        return new SearchResult(false, -1, 0, "No matches found");
    }

    public void Reset()
    {
        CurrentSearchText = string.Empty;
        _currentPosition = 0;
    }
}
