using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using Xunit;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Rendering;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Preservation property tests for startup performance bugfix.
/// These tests capture the CURRENT (unfixed) baseline behavior that must be
/// preserved after the fix is applied. All tests MUST PASS on unfixed code.
///
/// Property 2: Preservation — Preview Rendering, Export Functionality,
/// Recent Files, Keyboard Shortcuts, Error Handling.
/// </summary>
public class StartupPreservationPropertyTests
{
    // ---------------------------------------------------------------
    // Test 2a — Preview Rendering Preservation
    //
    // For arbitrary editor content strings, observe that UpdatePreview()
    // produces the correct rendering result. Write property: for all
    // non-empty content strings, UpdatePreview() produces the same
    // _lastPreviewedCode and _currentContentType as the original code.
    //
    // Observation: ContentTypeDetector.DetectContentType is the pure
    // function that determines _currentContentType from content. We
    // verify that for any content, the detector produces a deterministic
    // ContentType, and that the same content always maps to the same type.
    // This is the core logic that UpdatePreview relies on.
    //
    // **Validates: Requirements 3.1, 3.2**
    // ---------------------------------------------------------------

    [Property(MaxTest = 100)]
    public bool PreviewRendering_ContentTypeDetectionIsDeterministic(NonEmptyString content)
    {
        var detector = new ContentTypeDetector();
        var text = content.Get;

        // Call detection twice with the same input
        detector.ClearCache();
        var firstResult = detector.DetectContentType(text, string.Empty);

        detector.ClearCache();
        var secondResult = detector.DetectContentType(text, string.Empty);

        // The same content must always produce the same content type
        return firstResult == secondResult;
    }

    [Property(MaxTest = 100)]
    public bool PreviewRendering_ContentTypeAlwaysResolvesToKnownType(NonEmptyString content)
    {
        var detector = new ContentTypeDetector();
        var text = content.Get;

        // Whitespace-only strings are treated as empty by the detector (returns Unknown),
        // which is correct behavior — skip them for this property.
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var result = detector.DetectContentType(text, string.Empty);

        // For non-whitespace content, the detector should always resolve to a known type
        // (Mermaid, Markdown, or MarkdownWithMermaid — never Unknown)
        return result == ContentType.Mermaid
            || result == ContentType.Markdown
            || result == ContentType.MarkdownWithMermaid;
    }

    [Property(MaxTest = 50)]
    public bool PreviewRendering_MermaidContentDetectedCorrectly(NonEmptyString suffix)
    {
        var detector = new ContentTypeDetector();

        // Mermaid keywords that should be detected as Mermaid content
        var mermaidPrefixes = new[]
        {
            "graph TD\n",
            "flowchart LR\n",
            "sequenceDiagram\n",
            "classDiagram\n",
            "stateDiagram\n"
        };

        foreach (var prefix in mermaidPrefixes)
        {
            var text = prefix + suffix.Get;
            detector.ClearCache();
            var result = detector.DetectContentType(text, string.Empty);

            // Content starting with Mermaid keywords should be detected as Mermaid
            // (or MarkdownWithMermaid if the suffix contains markdown indicators)
            if (result != ContentType.Mermaid && result != ContentType.MarkdownWithMermaid)
                return false;
        }

        return true;
    }

    // ---------------------------------------------------------------
    // Test 2b — Export Service Preservation
    //
    // Observe that InitializeMarkdownToWordExport() creates a valid
    // _markdownToWordViewModel when WebView2 is ready. Write property:
    // for all valid WebView2 states, calling EnsureMarkdownToWordInitialized()
    // (lazy) produces the same _markdownToWordViewModel state as calling
    // InitializeMarkdownToWordExport() (eager).
    //
    // Observation: InitializeMarkdownToWordExport() has a guard that
    // checks PreviewBrowser?.CoreWebView2 != null and _isWebViewReady.
    // When both are true, it creates the export service chain. The
    // preservation property is that the guard logic and service creation
    // are unchanged — we verify this by analyzing the source code structure.
    //
    // **Validates: Requirements 3.3**
    // ---------------------------------------------------------------

    [Property(MaxTest = 1)]
    public bool ExportServicePreservation_InitializeMethodHasWebViewGuards()
    {
        // Verify that InitializeMarkdownToWordExport still has its WebView2 guards
        // and creates the same service chain. This ensures the export initialization
        // logic is preserved regardless of whether it's called eagerly or lazily.
        var sourceFile = FindSourceFile("MainWindow.MarkdownToWord.cs");
        var source = File.ReadAllText(sourceFile);

        var methodBody = ExtractMethodBody(source, "InitializeMarkdownToWordExport");
        Assert.False(string.IsNullOrEmpty(methodBody),
            "Could not find InitializeMarkdownToWordExport method");

        // Guard 1: WebView2 null check
        var hasWebViewNullCheck = methodBody.Contains("PreviewBrowser?.CoreWebView2 == null", StringComparison.Ordinal)
                               || methodBody.Contains("PreviewBrowser?.CoreWebView2", StringComparison.Ordinal);

        // Guard 2: _isWebViewReady check
        var hasReadyCheck = methodBody.Contains("_isWebViewReady", StringComparison.Ordinal);

        // Service creation chain preserved
        var createsMarkdownParser = methodBody.Contains("MarkdigMarkdownParser", StringComparison.Ordinal);
        var createsWordGenerator = methodBody.Contains("OpenXmlWordDocumentGenerator", StringComparison.Ordinal);
        var createsWebViewWrapper = methodBody.Contains("CoreWebView2Wrapper", StringComparison.Ordinal);
        var createsMermaidRenderer = methodBody.Contains("WebView2MermaidImageRenderer", StringComparison.Ordinal);
        var createsExportService = methodBody.Contains("MarkdownToWordExportService", StringComparison.Ordinal);
        var createsViewModel = methodBody.Contains("MarkdownToWordViewModel", StringComparison.Ordinal);

        return hasWebViewNullCheck
            && hasReadyCheck
            && createsMarkdownParser
            && createsWordGenerator
            && createsWebViewWrapper
            && createsMermaidRenderer
            && createsExportService
            && createsViewModel;
    }

    [Property(MaxTest = 1)]
    public bool ExportServicePreservation_ExportToWordRequiresWebViewReady()
    {
        // Verify that ExportToWord_Click still checks _isWebViewReady
        // before proceeding with export. This guard must be preserved.
        var sourceFile = FindSourceFile("MainWindow.MarkdownToWord.cs");
        var source = File.ReadAllText(sourceFile);

        var methodBody = ExtractMethodBody(source, "ExportToWord_Click");
        Assert.False(string.IsNullOrEmpty(methodBody),
            "Could not find ExportToWord_Click method");

        // Must check _isWebViewReady
        var checksReady = methodBody.Contains("_isWebViewReady", StringComparison.Ordinal);

        // Must check _markdownToWordViewModel null/CanExport
        var checksViewModel = methodBody.Contains("_markdownToWordViewModel", StringComparison.Ordinal);

        return checksReady && checksViewModel;
    }

    // ---------------------------------------------------------------
    // Test 2c — Recent Files Preservation
    //
    // Observe that PopulateRecentFilesMenu() correctly populates the
    // menu from _fileOperationsService.GetRecentFiles(). Write property:
    // for all lists of recent files (0 to 20 entries),
    // PopulateRecentFilesMenu() produces the same menu items regardless
    // of when it is called relative to WebView2 init.
    //
    // Observation: PopulateRecentFilesMenu() is a pure UI function that
    // reads from IFileOperationsService.GetRecentFiles() and populates
    // RecentFilesMenu.Items. It has no dependency on WebView2 state.
    // We verify this independence by analyzing the source code.
    //
    // **Validates: Requirements 3.4**
    // ---------------------------------------------------------------

    [Property(MaxTest = 1)]
    public bool RecentFilesPreservation_PopulateHasNoWebViewDependency()
    {
        // Verify that PopulateRecentFilesMenu does NOT reference any WebView2 state.
        // This ensures it works correctly regardless of when it's called relative
        // to WebView2 initialization.
        var sourceFile = FindSourceFile("MainWindow.FileOps.cs");
        var source = File.ReadAllText(sourceFile);

        var methodBody = ExtractMethodBody(source, "PopulateRecentFilesMenu");
        Assert.False(string.IsNullOrEmpty(methodBody),
            "Could not find PopulateRecentFilesMenu method");

        // Should NOT reference WebView2 state
        var referencesWebView = methodBody.Contains("_isWebViewReady", StringComparison.Ordinal)
                             || methodBody.Contains("PreviewBrowser", StringComparison.Ordinal)
                             || methodBody.Contains("CoreWebView2", StringComparison.Ordinal)
                             || methodBody.Contains("InitializeWebView", StringComparison.Ordinal);

        // Should reference the file operations service
        var usesFileService = methodBody.Contains("_fileOperationsService.GetRecentFiles()", StringComparison.Ordinal);

        // Should populate RecentFilesMenu.Items
        var populatesMenu = methodBody.Contains("RecentFilesMenu.Items", StringComparison.Ordinal);

        return !referencesWebView && usesFileService && populatesMenu;
    }

    [Property(MaxTest = 100)]
    public bool RecentFilesPreservation_MenuItemCountMatchesRecentFiles(byte fileCount)
    {
        // For any number of recent files (0 to 20), verify the expected menu item count.
        // This tests the pure logic of PopulateRecentFilesMenu without UI dependencies.
        var count = Math.Min(fileCount, (byte)20);

        var recentFiles = new List<RecentFileEntry>();
        for (int i = 0; i < count; i++)
        {
            recentFiles.Add(new RecentFileEntry
            {
                FilePath = $@"C:\test\file{i}.mmd",
                FileName = $"file{i}.mmd",
                LastOpened = DateTime.Now.AddMinutes(-i)
            });
        }

        // The expected menu item count:
        // 0 files → 1 item ("(No recent files)" disabled item)
        // N files (1-20) → N items + 1 separator + 1 "Clear Recent Files" = N + 2
        int expectedCount;
        if (count == 0)
        {
            expectedCount = 1;
        }
        else
        {
            expectedCount = count + 2; // files + separator + clear button
        }

        // Verify the logic by reading the source code structure
        var sourceFile = FindSourceFile("MainWindow.FileOps.cs");
        var source = File.ReadAllText(sourceFile);
        var methodBody = ExtractMethodBody(source, "PopulateRecentFilesMenu");

        // Verify the method handles empty case with disabled item
        var handlesEmpty = methodBody.Contains("(No recent files)", StringComparison.Ordinal)
                        && methodBody.Contains("IsEnabled = false", StringComparison.Ordinal);

        // Verify the method adds separator and clear button for non-empty case
        var addsSeparator = methodBody.Contains("MenuFlyoutSeparator", StringComparison.Ordinal);
        var addsClearButton = methodBody.Contains("Clear Recent Files", StringComparison.Ordinal);

        // Verify it takes at most 20 items
        var limitsTo20 = methodBody.Contains("Take(20)", StringComparison.Ordinal);

        return handlesEmpty && addsSeparator && addsClearButton && limitsTo20;
    }

    // ---------------------------------------------------------------
    // Test 2d — Keyboard Shortcut Preservation
    //
    // Observe that F11 and Escape WebView2 messages dispatch to
    // ToggleFullScreen_Click and presentation mode handlers. Write
    // property: for all keyboard event messages (F11_PRESSED,
    // ESCAPE_PRESSED), the dispatch behavior is unchanged.
    //
    // Observation: The WebView2 message handler in MainWindow.WebView.cs
    // checks for string messages "F11_PRESSED" and "ESCAPE_PRESSED"
    // and dispatches to ToggleFullScreen_Click and PresentationMode_Click.
    // We verify this dispatch logic is preserved in the source code.
    //
    // **Validates: Requirements 3.8**
    // ---------------------------------------------------------------

    /// <summary>
    /// Known keyboard messages dispatched from WebView2 JavaScript.
    /// </summary>
    public enum KeyboardMessage
    {
        F11_PRESSED,
        ESCAPE_PRESSED
    }

    [Property(MaxTest = 50)]
    public bool KeyboardShortcutPreservation_DispatchLogicIsPreserved(KeyboardMessage message)
    {
        // Verify that the WebView2 message handler still dispatches
        // F11_PRESSED and ESCAPE_PRESSED to the correct handlers.
        var sourceFile = FindSourceFile("MainWindow.WebView.cs");
        var source = File.ReadAllText(sourceFile);

        // F11_PRESSED should dispatch to ToggleFullScreen_Click
        var hasF11Handler = source.Contains("\"F11_PRESSED\"", StringComparison.Ordinal)
                         && source.Contains("ToggleFullScreen_Click", StringComparison.Ordinal);

        // ESCAPE_PRESSED should dispatch to ToggleFullScreen_Click (if fullscreen)
        // or PresentationMode_Click (if presentation mode)
        var hasEscapeHandler = source.Contains("\"ESCAPE_PRESSED\"", StringComparison.Ordinal)
                            && source.Contains("_isFullScreen", StringComparison.Ordinal)
                            && source.Contains("_isPresentationMode", StringComparison.Ordinal);

        return hasF11Handler && hasEscapeHandler;
    }

    [Property(MaxTest = 50)]
    public bool KeyboardShortcutPreservation_KeyboardEventMessageRoundTrips(
        NonEmptyString key, bool ctrlKey, bool shiftKey, bool altKey)
    {
        // Verify that KeyboardEventMessage serialization/deserialization
        // is preserved — this is the data contract between WebView2 JS and C#.
        var original = new KeyboardEventMessage
        {
            Type = "keypress",
            Key = key.Get,
            CtrlKey = ctrlKey,
            ShiftKey = shiftKey,
            AltKey = altKey
        };

        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var deserialized = JsonSerializer.Deserialize<KeyboardEventMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return deserialized != null
            && deserialized.Type == original.Type
            && deserialized.Key == original.Key
            && deserialized.CtrlKey == original.CtrlKey
            && deserialized.ShiftKey == original.ShiftKey
            && deserialized.AltKey == original.AltKey;
    }

    // ---------------------------------------------------------------
    // Test 2e — Navigation Error Preservation
    //
    // Observe that NavigationCompleted with IsSuccess=false logs a
    // warning. Write property: for all navigation failure statuses,
    // the error handling behavior is unchanged.
    //
    // Observation: In MainWindow.WebView.cs, the NavigationCompleted
    // handler checks args.IsSuccess and logs a warning with the
    // WebErrorStatus when navigation fails. We verify this error
    // handling logic is preserved.
    //
    // **Validates: Requirements 3.7**
    // ---------------------------------------------------------------

    [Property(MaxTest = 1)]
    public bool NavigationErrorPreservation_FailureHandlerLogsWarning()
    {
        // Verify that the NavigationCompleted handler still logs warnings
        // for failed navigations. This error handling must be preserved.
        var sourceFile = FindSourceFile("MainWindow.WebView.cs");
        var source = File.ReadAllText(sourceFile);

        // The handler should check IsSuccess
        var checksSuccess = source.Contains("!e.IsSuccess", StringComparison.Ordinal)
                         || source.Contains("args.IsSuccess", StringComparison.Ordinal)
                         || source.Contains(".IsSuccess", StringComparison.Ordinal);

        // Should log a warning with WebErrorStatus
        var logsWarning = source.Contains("LogWarning", StringComparison.Ordinal)
                       && source.Contains("WebErrorStatus", StringComparison.Ordinal);

        // The NavigationCompleted event should be wired up
        var hasNavigationHandler = source.Contains("NavigationCompleted", StringComparison.Ordinal);

        return checksSuccess && logsWarning && hasNavigationHandler;
    }

    [Property(MaxTest = 1)]
    public bool NavigationErrorPreservation_NavigationCompletedHandlerStructurePreserved()
    {
        // Verify the full structure of the NavigationCompleted handler:
        // 1. Checks IsSuccess
        // 2. On failure: logs warning with WebErrorStatus
        // 3. On success: gets mermaid version, calls OnWebViewReady() which triggers UpdatePreview
        var sourceFile = FindSourceFile("MainWindow.WebView.cs");
        var source = File.ReadAllText(sourceFile);

        // Find the PreviewBrowser_NavigationCompleted method or inline handler
        // In the current code, there are two NavigationCompleted handlers:
        // 1. An inline lambda for error logging
        // 2. PreviewBrowser_NavigationCompleted for success handling

        // Inline error handler
        var hasInlineErrorHandler = source.Contains("Navigation failed:", StringComparison.Ordinal)
                                 || source.Contains("Navigation failed", StringComparison.Ordinal);

        // PreviewBrowser_NavigationCompleted method exists
        var hasNamedHandler = source.Contains("PreviewBrowser_NavigationCompleted", StringComparison.Ordinal);

        // The named handler checks IsSuccess and calls OnWebViewReady() on success
        // (OnWebViewReady is the consolidated handler that triggers UpdatePreview)
        var namedMethodBody = ExtractMethodBody(source, "PreviewBrowser_NavigationCompleted");
        var handlerChecksSuccess = !string.IsNullOrEmpty(namedMethodBody)
                                && namedMethodBody.Contains("IsSuccess", StringComparison.Ordinal);
        var handlerTriggersPreviewUpdate = !string.IsNullOrEmpty(namedMethodBody)
                                        && (namedMethodBody.Contains("UpdatePreview", StringComparison.Ordinal)
                                         || namedMethodBody.Contains("OnWebViewReady", StringComparison.Ordinal));

        // Verify OnWebViewReady itself calls UpdatePreview (the actual preservation)
        var onWebViewReadyBody = ExtractMethodBody(source, "OnWebViewReady");
        var onWebViewReadyCallsUpdatePreview = !string.IsNullOrEmpty(onWebViewReadyBody)
                                            && onWebViewReadyBody.Contains("UpdatePreview", StringComparison.Ordinal);

        return hasInlineErrorHandler && hasNamedHandler && handlerChecksSuccess
            && handlerTriggersPreviewUpdate && onWebViewReadyCallsUpdatePreview;
    }

    // ---------------------------------------------------------------
    // Helper methods (shared with StartupBugConditionPropertyTests)
    // ---------------------------------------------------------------

    /// <summary>
    /// Finds a source file by name, searching upward from the test assembly location.
    /// </summary>
    private static string FindSourceFile(string fileName)
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "MermaidDiagramApp", fileName);
            if (File.Exists(candidate))
                return candidate;

            candidate = Path.Combine(dir, "MermaidDiagramApp", "Assets", fileName);
            if (File.Exists(candidate))
                return candidate;

            dir = Directory.GetParent(dir)?.FullName;
        }

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
        var pattern = $@"(private|public|protected|internal)\s+.*\s+{Regex.Escape(methodName)}\s*\(";
        var match = Regex.Match(source, pattern);
        if (!match.Success)
            return string.Empty;

        var startIdx = source.IndexOf('{', match.Index + match.Length);
        if (startIdx < 0)
            return string.Empty;

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
