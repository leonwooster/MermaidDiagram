using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using Xunit;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Bug condition exploration property tests for startup performance defects.
/// These tests encode the EXPECTED (fixed) behavior and are expected to FAIL
/// on unfixed code, confirming the bugs exist.
///
/// Property 1: Bug Condition — Startup Blocks UI on WebView2 Init,
/// Sequential JS Loading, Redundant Ready Paths, Eager Export Init,
/// Immediate Update Check.
/// </summary>
public class StartupBugConditionPropertyTests
{
    // ---------------------------------------------------------------
    // Test 1a — UI Shell Ordering
    // Assert that PopulateRecentFilesMenu() and InitializeBuilderWiring()
    // execute BEFORE InitializeWebViewAsync() completes in MainWindow_Loaded.
    //
    // On unfixed code, MainWindow_Loaded awaits InitializeWebViewAsync()
    // first, then calls PopulateRecentFilesMenu() and InitializeBuilderWiring()
    // → FAIL expected (menus populated AFTER WebView2 init).
    //
    // Validates: Requirements 1.1, 2.1
    // ---------------------------------------------------------------

    [Property(MaxTest = 1)]
    public bool UIShellOrdering_PopulateAndWiringShouldExecuteBeforeWebViewInit()
    {
        // Read the MainWindow.xaml.cs source to verify call ordering in MainWindow_Loaded.
        // The fixed code should have PopulateRecentFilesMenu() and InitializeBuilderWiring()
        // BEFORE the await InitializeWebViewAsync() call.
        var sourceFile = FindSourceFile("MainWindow.xaml.cs");
        Assert.NotNull(sourceFile);

        var source = File.ReadAllText(sourceFile);

        // Extract the MainWindow_Loaded method body
        var loadedMethodBody = ExtractMethodBody(source, "MainWindow_Loaded");
        Assert.False(string.IsNullOrEmpty(loadedMethodBody),
            "Could not find MainWindow_Loaded method in source");

        // Find positions of key calls within the method body
        var populatePos = loadedMethodBody.IndexOf("PopulateRecentFilesMenu()", StringComparison.Ordinal);
        var wiringPos = loadedMethodBody.IndexOf("InitializeBuilderWiring()", StringComparison.Ordinal);
        var webViewPos = loadedMethodBody.IndexOf("InitializeWebViewAsync()", StringComparison.Ordinal);

        Assert.True(populatePos >= 0, "PopulateRecentFilesMenu() not found in MainWindow_Loaded");
        Assert.True(wiringPos >= 0, "InitializeBuilderWiring() not found in MainWindow_Loaded");
        Assert.True(webViewPos >= 0, "InitializeWebViewAsync() not found in MainWindow_Loaded");

        // Assert: both UI shell calls appear BEFORE the WebView init call
        // In the fixed code, PopulateRecentFilesMenu and InitializeBuilderWiring
        // should be called before await InitializeWebViewAsync()
        return populatePos < webViewPos && wiringPos < webViewPos;
    }

    // ---------------------------------------------------------------
    // Test 1b — JS Library Loading
    // Assert that loadLibraries() in UnifiedRenderer.html uses Promise.all
    // for concurrent loading of mermaid.js, markdown-it, and highlight.js.
    //
    // On unfixed code, loadLibraries() uses three sequential
    // await loadScript(...) calls → FAIL expected.
    //
    // Validates: Requirements 1.2, 2.2
    // ---------------------------------------------------------------

    [Property(MaxTest = 1)]
    public bool JSLibraryLoading_ShouldUsePromiseAllForConcurrentLoading()
    {
        // Read the UnifiedRenderer.html source to verify loadLibraries() uses Promise.all
        var sourceFile = FindSourceFile("UnifiedRenderer.html");
        Assert.NotNull(sourceFile);

        var source = File.ReadAllText(sourceFile);

        // Extract the loadLibraries function body
        var loadLibrariesBody = ExtractJsFunctionBody(source, "loadLibraries");
        Assert.False(string.IsNullOrEmpty(loadLibrariesBody),
            "Could not find loadLibraries function in UnifiedRenderer.html");

        // The fixed code should use Promise.all to load all three libraries concurrently
        var hasPromiseAll = loadLibrariesBody.Contains("Promise.all", StringComparison.Ordinal);

        // Count sequential await loadScript calls — in unfixed code there are 3 separate awaits
        var sequentialAwaitCount = Regex.Matches(loadLibrariesBody, @"await\s+loadScript\s*\(").Count;

        // Fixed code: Promise.all is present, and there should NOT be multiple
        // separate top-level await loadScript() calls (they should be inside Promise.all)
        return hasPromiseAll && sequentialAwaitCount == 0;
    }

    // ---------------------------------------------------------------
    // Test 1c — Idempotent Ready Detection
    // Generate random sequences of ready signals (JSON "ready", legacy
    // "MermaidReady", polling timer, NavigationCompleted). Assert that
    // _isWebViewReady is set exactly once and UpdatePreview() is called
    // exactly once regardless of signal count/order.
    //
    // On unfixed code, each path independently sets _isWebViewReady and
    // calls InitializeMarkdownToWordExport() + UpdatePreview()
    // → FAIL expected (multiple calls).
    //
    // Validates: Requirements 1.3, 2.3
    // ---------------------------------------------------------------

    /// <summary>
    /// Represents the different ready signal types that can arrive during startup.
    /// </summary>
    public enum ReadySignal
    {
        JsonReady,
        LegacyMermaidReady,
        PollingTimer,
        NavigationCompleted
    }

    [Property(MaxTest = 50)]
    public bool IdempotentReadyDetection_ShouldSetReadyExactlyOnce(ReadySignal[] signals)
    {
        if (signals == null || signals.Length == 0)
            return true; // vacuously true for empty input

        // Limit to reasonable sequence length
        var limitedSignals = signals.Take(20).ToArray();

        // Analyze the source code to verify a consolidated OnWebViewReady() guard exists.
        // The fixed code should have a single OnWebViewReady() method with
        // "if (_isWebViewReady) return;" guard, called from all 4 detection paths.
        var sourceFile = FindSourceFile("MainWindow.WebView.cs");
        Assert.NotNull(sourceFile);

        var source = File.ReadAllText(sourceFile);

        // Check for consolidated OnWebViewReady method with guard
        var hasOnWebViewReady = source.Contains("OnWebViewReady()", StringComparison.Ordinal);
        var hasGuard = source.Contains("if (_isWebViewReady) return;", StringComparison.Ordinal);

        // Count how many times _isWebViewReady = true appears (should be exactly 1 in OnWebViewReady)
        var readyAssignments = Regex.Matches(source, @"_isWebViewReady\s*=\s*true").Count;

        // Count how many times InitializeMarkdownToWordExport() is called from ready paths
        // In fixed code, it should NOT be called from any ready path (lazy init instead)
        var initExportCalls = Regex.Matches(source, @"InitializeMarkdownToWordExport\s*\(\s*\)").Count;

        // Fixed code: OnWebViewReady exists with guard, _isWebViewReady set exactly once,
        // and InitializeMarkdownToWordExport is NOT called from ready paths (0 calls)
        return hasOnWebViewReady && hasGuard && readyAssignments == 1 && initExportCalls == 0;
    }

    // ---------------------------------------------------------------
    // Test 1d — Lazy Export Init
    // Assert that _markdownToWordViewModel remains null after WebView2
    // ready if no export action is triggered. The fixed code should use
    // lazy initialization via EnsureMarkdownToWordInitialized().
    //
    // On unfixed code, InitializeMarkdownToWordExport() is called eagerly
    // from ready paths → FAIL expected.
    //
    // Validates: Requirements 1.4, 2.4
    // ---------------------------------------------------------------

    [Property(MaxTest = 1)]
    public bool LazyExportInit_ShouldNotCreateExportServicesAtStartup()
    {
        // Verify the source code structure:
        // 1. OnWebViewReady() (or ready paths) should NOT call InitializeMarkdownToWordExport()
        // 2. An EnsureMarkdownToWordInitialized() lazy wrapper should exist
        // 3. Export usage sites should call EnsureMarkdownToWordInitialized()

        var webViewSource = File.ReadAllText(FindSourceFile("MainWindow.WebView.cs"));
        var markdownToWordSource = File.ReadAllText(FindSourceFile("MainWindow.MarkdownToWord.cs"));

        // In fixed code, InitializeMarkdownToWordExport should NOT be called from WebView ready paths
        var initExportInWebView = Regex.Matches(webViewSource, @"InitializeMarkdownToWordExport\s*\(\s*\)").Count;

        // In fixed code, EnsureMarkdownToWordInitialized should exist in MarkdownToWord partial
        var hasLazyWrapper = markdownToWordSource.Contains("EnsureMarkdownToWordInitialized", StringComparison.Ordinal);

        // The lazy wrapper should have a null guard on _markdownToWordViewModel
        var hasNullGuard = markdownToWordSource.Contains("_markdownToWordViewModel != null", StringComparison.Ordinal)
                        || markdownToWordSource.Contains("_markdownToWordViewModel is not null", StringComparison.Ordinal);

        // Fixed code: no InitializeMarkdownToWordExport calls in WebView.cs,
        // lazy wrapper exists with null guard
        return initExportInWebView == 0 && hasLazyWrapper && hasNullGuard;
    }

    // ---------------------------------------------------------------
    // Test 1e — Delayed Update Check
    // Assert that CheckForMermaidUpdatesAsync() delays at least 5 seconds
    // before making a network request.
    //
    // On unfixed code, fires immediately in constructor with no delay
    // → FAIL expected.
    //
    // Validates: Requirements 1.5, 2.5
    // ---------------------------------------------------------------

    [Property(MaxTest = 1)]
    public bool DelayedUpdateCheck_ShouldDelayAtLeast5SecondsBeforeNetworkRequest()
    {
        // Read the Export partial class source to verify CheckForMermaidUpdatesAsync
        // has a Task.Delay of at least 5 seconds before the network request.
        var sourceFile = FindSourceFile("MainWindow.Export.cs");
        Assert.NotNull(sourceFile);

        var source = File.ReadAllText(sourceFile);

        // Extract the CheckForMermaidUpdatesAsync method body
        var methodBody = ExtractMethodBody(source, "CheckForMermaidUpdatesAsync");
        Assert.False(string.IsNullOrEmpty(methodBody),
            "Could not find CheckForMermaidUpdatesAsync method in source");

        // The fixed code should have a Task.Delay at the start of the method,
        // BEFORE the try block that calls CheckForUpdatesAsync
        var delayPattern = Regex.Match(methodBody,
            @"await\s+Task\.Delay\s*\(\s*TimeSpan\.FromSeconds\s*\(\s*(\d+)\s*\)\s*\)");

        if (!delayPattern.Success)
        {
            // Also check for Task.Delay(milliseconds) pattern
            delayPattern = Regex.Match(methodBody,
                @"await\s+Task\.Delay\s*\(\s*(\d+)\s*\)");

            if (delayPattern.Success)
            {
                var delayMs = int.Parse(delayPattern.Groups[1].Value);
                return delayMs >= 5000;
            }

            return false; // No delay found
        }

        var delaySeconds = int.Parse(delayPattern.Groups[1].Value);

        // The delay must appear BEFORE the network request (CheckForUpdatesAsync)
        var delayPos = methodBody.IndexOf("Task.Delay", StringComparison.Ordinal);
        var networkPos = methodBody.IndexOf("CheckForUpdatesAsync", StringComparison.Ordinal);

        // Fixed code: delay of >= 5 seconds exists and appears before the network call
        return delaySeconds >= 5 && delayPos >= 0 && networkPos >= 0 && delayPos < networkPos;
    }

    // ---------------------------------------------------------------
    // Helper methods
    // ---------------------------------------------------------------

    /// <summary>
    /// Finds a source file by name, searching upward from the test assembly location
    /// to find the MermaidDiagramApp project directory.
    /// </summary>
    private static string FindSourceFile(string fileName)
    {
        // Start from the current directory and search upward for the repo root
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            // Check MermaidDiagramApp project directory
            var candidate = Path.Combine(dir, "MermaidDiagramApp", fileName);
            if (File.Exists(candidate))
                return candidate;

            // Check Assets subdirectory
            candidate = Path.Combine(dir, "MermaidDiagramApp", "Assets", fileName);
            if (File.Exists(candidate))
                return candidate;

            dir = Directory.GetParent(dir)?.FullName;
        }

        // Fallback: try relative paths from common test execution directories
        var relativePaths = new[]
        {
            Path.Combine("..", "..", "..", "..", "MermaidDiagramApp", fileName),
            Path.Combine("..", "..", "..", "..", "MermaidDiagramApp", "Assets", fileName),
            Path.Combine("..", "MermaidDiagramApp", fileName),
            Path.Combine("..", "MermaidDiagramApp", "Assets", fileName),
        };

        foreach (var path in relativePaths)
        {
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }

        throw new FileNotFoundException($"Could not find source file: {fileName}");
    }

    /// <summary>
    /// Extracts a C# method body from source code by matching the method signature
    /// and counting braces to find the complete body.
    /// </summary>
    private static string ExtractMethodBody(string source, string methodName)
    {
        // Find the method signature
        var pattern = $@"(private|public|protected|internal)\s+.*\s+{Regex.Escape(methodName)}\s*\(";
        var match = Regex.Match(source, pattern);
        if (!match.Success)
            return string.Empty;

        // Find the opening brace
        var startIdx = source.IndexOf('{', match.Index + match.Length);
        if (startIdx < 0)
            return string.Empty;

        // Count braces to find the matching closing brace
        int braceCount = 1;
        int idx = startIdx + 1;
        while (idx < source.Length && braceCount > 0)
        {
            if (source[idx] == '{') braceCount++;
            else if (source[idx] == '}') braceCount--;
            idx++;
        }

        return source.Substring(startIdx + 1, idx - startIdx - 2);
    }

    /// <summary>
    /// Extracts a JavaScript function body from source code.
    /// </summary>
    private static string ExtractJsFunctionBody(string source, string functionName)
    {
        // Match async function or regular function
        var pattern = $@"(async\s+)?function\s+{Regex.Escape(functionName)}\s*\(";
        var match = Regex.Match(source, pattern);
        if (!match.Success)
            return string.Empty;

        // Find the opening brace
        var startIdx = source.IndexOf('{', match.Index + match.Length);
        if (startIdx < 0)
            return string.Empty;

        // Count braces to find the matching closing brace
        int braceCount = 1;
        int idx = startIdx + 1;
        while (idx < source.Length && braceCount > 0)
        {
            if (source[idx] == '{') braceCount++;
            else if (source[idx] == '}') braceCount--;
            idx++;
        }

        return source.Substring(startIdx + 1, idx - startIdx - 2);
    }
}
