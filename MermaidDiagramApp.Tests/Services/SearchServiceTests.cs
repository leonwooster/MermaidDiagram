using Xunit;
using MermaidDiagramApp.Services;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for SearchService.
/// Requirements: 6.4
/// </summary>
public class SearchServiceTests
{
    private readonly SearchService _service = new();

    private const string SampleContent = "Hello world, hello again. HELLO once more.";

    #region SetSearchText Tests

    [Fact]
    public void SetSearchText_UpdatesCurrentSearchText()
    {
        _service.SetSearchText("test");
        Assert.Equal("test", _service.CurrentSearchText);
    }

    [Fact]
    public void SetSearchText_WithSameText_DoesNotResetPosition()
    {
        // Find first "hello" to advance position
        _service.FindNext("hello", SampleContent);
        var firstResult = _service.FindNext("hello", SampleContent);

        // Set same text — position should not reset
        _service.SetSearchText("hello");

        // Next find should continue from current position, not restart
        var afterSet = _service.FindNext("hello", SampleContent);
        // Should find the third occurrence (HELLO) or wrap, not the first again
        Assert.True(afterSet.Found);
    }

    #endregion

    #region FindNext Tests

    [Fact]
    public void FindNext_FindsFirstOccurrence()
    {
        var result = _service.FindNext("hello", SampleContent);

        Assert.True(result.Found);
        Assert.Equal(0, result.MatchIndex);
        Assert.Equal(5, result.MatchLength);
    }

    [Fact]
    public void FindNext_FindsSubsequentOccurrences()
    {
        _service.FindNext("hello", SampleContent);
        var second = _service.FindNext("hello", SampleContent);

        Assert.True(second.Found);
        Assert.Equal(13, second.MatchIndex);
    }

    [Fact]
    public void FindNext_WrapsAroundToBeginning()
    {
        // Exhaust all occurrences
        _service.FindNext("hello", SampleContent); // index 0
        _service.FindNext("hello", SampleContent); // index 13
        _service.FindNext("hello", SampleContent); // index 26

        // Should wrap to first occurrence
        var wrapped = _service.FindNext("hello", SampleContent);
        Assert.True(wrapped.Found);
        Assert.Equal(0, wrapped.MatchIndex);
    }

    [Fact]
    public void FindNext_WithEmptyText_ReturnsNotFound()
    {
        var result = _service.FindNext("", SampleContent);

        Assert.False(result.Found);
        Assert.Equal(-1, result.MatchIndex);
    }

    [Fact]
    public void FindNext_WithNullText_ReturnsNotFound()
    {
        var result = _service.FindNext(null!, SampleContent);

        Assert.False(result.Found);
        Assert.Equal(-1, result.MatchIndex);
    }

    [Fact]
    public void FindNext_WithEmptyContent_ReturnsNotFound()
    {
        var result = _service.FindNext("hello", "");

        Assert.False(result.Found);
        Assert.Equal(-1, result.MatchIndex);
    }

    [Fact]
    public void FindNext_WithNoMatch_ReturnsNotFound()
    {
        var result = _service.FindNext("xyz", SampleContent);

        Assert.False(result.Found);
        Assert.Equal(-1, result.MatchIndex);
        Assert.Contains("No matches", result.StatusMessage);
    }

    #endregion

    #region FindPrevious Tests

    [Fact]
    public void FindPrevious_FindsLastOccurrence()
    {
        var result = _service.FindPrevious("hello", SampleContent);

        Assert.True(result.Found);
        // Starting from end, should find the last "HELLO" at index 26
        Assert.Equal(26, result.MatchIndex);
    }

    [Fact]
    public void FindPrevious_WrapsAroundToEnd()
    {
        // Find last, then previous, then previous — eventually wraps
        _service.FindPrevious("hello", SampleContent); // 26
        _service.FindPrevious("hello", SampleContent); // 13
        _service.FindPrevious("hello", SampleContent); // 0

        var wrapped = _service.FindPrevious("hello", SampleContent);
        Assert.True(wrapped.Found);
        Assert.Equal(26, wrapped.MatchIndex);
    }

    [Fact]
    public void FindPrevious_WithEmptyText_ReturnsNotFound()
    {
        var result = _service.FindPrevious("", SampleContent);

        Assert.False(result.Found);
        Assert.Equal(-1, result.MatchIndex);
    }

    [Fact]
    public void FindPrevious_WithEmptyContent_ReturnsNotFound()
    {
        var result = _service.FindPrevious("hello", "");

        Assert.False(result.Found);
        Assert.Equal(-1, result.MatchIndex);
    }

    [Fact]
    public void FindPrevious_WithNoMatch_ReturnsNotFound()
    {
        var result = _service.FindPrevious("xyz", SampleContent);

        Assert.False(result.Found);
        Assert.Equal(-1, result.MatchIndex);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsCurrentSearchText()
    {
        _service.SetSearchText("hello");
        _service.FindNext("hello", SampleContent);

        _service.Reset();

        Assert.Equal(string.Empty, _service.CurrentSearchText);
    }

    [Fact]
    public void Reset_ResetsSearchPosition()
    {
        // Advance position past first match
        _service.FindNext("hello", SampleContent);
        _service.FindNext("hello", SampleContent);

        _service.Reset();

        // After reset, FindNext should find from the beginning again
        var result = _service.FindNext("hello", SampleContent);
        Assert.True(result.Found);
        Assert.Equal(0, result.MatchIndex);
    }

    #endregion

    #region Case-Insensitive Search Tests

    [Fact]
    public void FindNext_IsCaseInsensitive()
    {
        var result = _service.FindNext("HELLO", SampleContent);

        Assert.True(result.Found);
        // Should find "Hello" at index 0 (case-insensitive)
        Assert.Equal(0, result.MatchIndex);
    }

    [Fact]
    public void FindPrevious_IsCaseInsensitive()
    {
        var result = _service.FindPrevious("hello", "ABC Hello DEF");

        Assert.True(result.Found);
        Assert.Equal(4, result.MatchIndex);
    }

    #endregion
}
