using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for default column widths in MainWindow.xaml.
/// Since MainWindow requires a WinUI runtime, we verify the XAML content directly.
/// Validates: Requirements 1.1
/// </summary>
public class DefaultColumnWidthTests
{
    private static string GetMainWindowXamlContent()
    {
        // Try multiple strategies to locate MainWindow.xaml
        var candidates = new List<string>();

        // Strategy 1: Navigate up from test bin directory (bin/x64/Debug/net8.0-windows...)
        var testDir = AppContext.BaseDirectory;
        for (int levels = 1; levels <= 6; levels++)
        {
            var upPath = string.Join(Path.DirectorySeparatorChar.ToString(),
                Enumerable.Repeat("..", levels));
            var candidate = Path.GetFullPath(Path.Combine(testDir, upPath, "MermaidDiagramApp", "MainWindow.xaml"));
            candidates.Add(candidate);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        // Strategy 2: Relative from current working directory
        var cwdPath = Path.Combine("MermaidDiagramApp", "MainWindow.xaml");
        candidates.Add(Path.GetFullPath(cwdPath));
        if (File.Exists(cwdPath))
            return File.ReadAllText(cwdPath);

        Assert.Fail($"MainWindow.xaml not found. Searched:\n{string.Join("\n", candidates)}");
        return string.Empty; // unreachable
    }

    /// <summary>
    /// Requirement 1.1: EditorColumn width SHALL be set to 3* (30% proportion).
    /// </summary>
    [Fact]
    public void EditorColumn_DefaultWidth_Is3Star()
    {
        var xaml = GetMainWindowXamlContent();

        // Match the EditorColumn definition and extract its Width attribute
        var match = Regex.Match(xaml, @"<ColumnDefinition\s+x:Name=""EditorColumn""\s+Width=""([^""]+)""");
        Assert.True(match.Success, "EditorColumn definition not found in MainWindow.xaml");

        var width = match.Groups[1].Value;
        Assert.Equal("3*", width);
    }

    /// <summary>
    /// Requirement 1.1: PreviewColumn width SHALL be set to 7* (70% proportion).
    /// </summary>
    [Fact]
    public void PreviewColumn_DefaultWidth_Is7Star()
    {
        var xaml = GetMainWindowXamlContent();

        // Match the PreviewColumn definition and extract its Width attribute
        var match = Regex.Match(xaml, @"<ColumnDefinition\s+x:Name=""PreviewColumn""\s+Width=""([^""]+)""");
        Assert.True(match.Success, "PreviewColumn definition not found in MainWindow.xaml");

        var width = match.Groups[1].Value;
        Assert.Equal("7*", width);
    }
}
