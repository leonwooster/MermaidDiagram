# Startup Performance Fix — Bugfix Design

## Overview

The application takes ~3–3.5 seconds before the user can interact with the UI. The root cause is a combination of blocking WebView2 initialization, sequential JS library loading, redundant ready-detection paths, eager export service creation, and a resource-competing update check during construction. The fix reorders startup so the UI shell (editor, menus, recent files) is usable immediately while the preview pane initializes in the background, and eliminates redundant/eager work.

## Glossary

- **Bug_Condition (C)**: The application startup sequence — when `MainWindow_Loaded` fires and the constructor executes, the current code blocks the UI and performs redundant/eager initialization
- **Property (P)**: The UI shell (editor, menus, recent files, builder wiring) is interactive immediately; WebView2 initializes in the background; JS libraries load in parallel; ready-detection is consolidated; export services are lazy; update check is delayed
- **Preservation**: Preview rendering, editor-to-preview latency, Word export, recent files, update checking, builder wiring, error handling, and keyboard shortcuts must all continue to work correctly after the fix
- **MainWindow_Loaded**: The event handler in `MainWindow.xaml.cs` that orchestrates post-load initialization
- **InitializeWebViewAsync()**: The async method in `MainWindow.WebView.cs` that sets up WebView2, message handlers, navigation, and the polling timer
- **OnWebViewReady()**: The proposed consolidated method that replaces the four separate `_isWebViewReady = true` code paths
- **loadLibraries()**: The JavaScript function in `UnifiedRenderer.html` that loads mermaid.js, markdown-it, and highlight.js
- **InitializeMarkdownToWordExport()**: The method in `MainWindow.MarkdownToWord.cs` that eagerly creates five export service objects
- **CheckForMermaidUpdatesAsync()**: The fire-and-forget method in `MainWindow.Export.cs` that checks for Mermaid.js updates

## Bug Details

### Bug Condition

The bug manifests during every application startup. The `MainWindow_Loaded` handler blocks the entire UI by awaiting `InitializeWebViewAsync()` before populating menus or wiring the builder. Inside WebView2, JS libraries load sequentially. Four separate code paths independently set `_isWebViewReady = true` and call `InitializeMarkdownToWordExport()` + `UpdatePreview()`. Export services are eagerly created even though most sessions never use Word export. The mermaid update check fires immediately in the constructor, competing for resources.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type StartupSequence
  OUTPUT: boolean

  RETURN input.phase == ApplicationStartup
         AND (
           mainWindowLoaded_awaitsWebViewBeforeUISetup(input)
           OR jsLibrariesLoadedSequentially(input)
           OR multipleReadyPathsExecuteIndependently(input)
           OR exportServicesCreatedEagerly(input)
           OR mermaidUpdateCheckRunsImmediately(input)
         )
END FUNCTION
```

### Examples

- User launches app → UI is blank/frozen for 2–5 seconds while `InitializeWebViewAsync()` completes, even though the code editor and menus have no WebView2 dependency
- `UnifiedRenderer.html` loads mermaid.js (200ms), then markdown-it (150ms), then highlight.js (150ms) sequentially = ~500ms; parallel would be ~200ms
- WebView2 sends `"ready"` JSON message → sets `_isWebViewReady`, calls `InitializeMarkdownToWordExport()` + `UpdatePreview()`. Then the 1-second polling timer fires → sets `_isWebViewReady` again, calls `InitializeMarkdownToWordExport()` + `UpdatePreview()` again (duplicate work)
- User launches app, never exports to Word → five export service objects created and never used (200–500ms wasted)
- `CheckForMermaidUpdatesAsync()` fires in constructor → network request competes with WebView2 initialization for CPU/network during critical startup window

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Preview renders correctly for current editor content once WebView2 finishes background initialization
- Editor-to-preview update latency remains under 1 second
- Markdown-to-Word export produces correct .docx files when the user triggers it
- Recent files menu loads and displays correctly
- Mermaid.js update detection and notification still works
- Visual diagram builder functions correctly after builder wiring completes
- WebView2 navigation failures display error messages in the preview pane
- Keyboard shortcuts (F11, Escape, F5, F7) in WebView continue to work

**Scope:**
All functionality that does not depend on startup ordering or initialization timing should be completely unaffected. This includes: all rendering logic, all file operations, all export logic, all UI interactions post-startup.

## Hypothesized Root Cause

Based on the code analysis, the root causes are:

1. **Blocking UI in MainWindow_Loaded**: `await InitializeWebViewAsync()` on line ~160 of `MainWindow.xaml.cs` blocks `PopulateRecentFilesMenu()` and `InitializeBuilderWiring()` from executing until WebView2 is fully ready (2–5 seconds)

2. **Sequential JS Loading**: `loadLibraries()` in `UnifiedRenderer.html` uses three sequential `await loadScript(...)` calls instead of `Promise.all`, adding ~300–500ms of unnecessary serial wait time

3. **Redundant Ready-Detection**: Four independent code paths in `MainWindow.WebView.cs` each set `_isWebViewReady = true` and call `InitializeMarkdownToWordExport()` + `UpdatePreview()`:
   - JSON `"ready"` message handler (line ~70)
   - Legacy `"MermaidReady"` string handler (line ~120)
   - 1-second polling timer tick (line ~170)
   - `PreviewBrowser_NavigationCompleted` (line ~195)

4. **Eager Export Service Creation**: `InitializeMarkdownToWordExport()` in `MainWindow.MarkdownToWord.cs` creates `MarkdigMarkdownParser`, `OpenXmlWordDocumentGenerator`, `CoreWebView2Wrapper`, `WebView2MermaidImageRenderer`, and `MarkdownToWordExportService` the moment WebView2 is ready, regardless of whether the user will ever export

5. **Immediate Update Check**: `_ = CheckForMermaidUpdatesAsync()` fires in the `MainWindow` constructor (line ~145 of `MainWindow.xaml.cs`) with no delay, competing for network and CPU during the critical startup window

## Correctness Properties

Property 1: Bug Condition — UI Shell Available Before WebView2

_For any_ application startup sequence, the fixed `MainWindow_Loaded` SHALL execute `PopulateRecentFilesMenu()` and `InitializeBuilderWiring()` before awaiting `InitializeWebViewAsync()`, ensuring the UI shell is interactive while WebView2 initializes in the background.

**Validates: Requirements 2.1**

Property 2: Bug Condition — Parallel JS Library Loading

_For any_ page load of `UnifiedRenderer.html`, the fixed `loadLibraries()` function SHALL load mermaid.js, markdown-it, and highlight.js concurrently using `Promise.all`, so total load time equals the slowest single library rather than the sum of all three.

**Validates: Requirements 2.2**

Property 3: Bug Condition — Idempotent WebView Ready Handling

_For any_ sequence of ready signals (JSON "ready", legacy "MermaidReady", polling timer, NavigationCompleted), the fixed `OnWebViewReady()` method SHALL execute initialization logic (setting `_isWebViewReady`, stopping the timer, triggering `UpdatePreview`) exactly once, with subsequent calls being no-ops due to the guard.

**Validates: Requirements 2.3**

Property 4: Bug Condition — Lazy Export Service Initialization

_For any_ application startup that does not include a Word export action, the fixed code SHALL NOT create `MarkdigMarkdownParser`, `OpenXmlWordDocumentGenerator`, `CoreWebView2Wrapper`, `WebView2MermaidImageRenderer`, or `MarkdownToWordExportService` instances. These SHALL only be created on first export request via `EnsureMarkdownToWordInitialized()`.

**Validates: Requirements 2.4**

Property 5: Bug Condition — Delayed Mermaid Update Check

_For any_ application startup, the fixed `CheckForMermaidUpdatesAsync()` SHALL delay at least 5 seconds before performing the network request, so it does not compete with WebView2 initialization for resources.

**Validates: Requirements 2.5**

Property 6: Preservation — Preview Rendering After Background Init

_For any_ editor content present when WebView2 finishes background initialization, the fixed code SHALL produce the same rendering result as the original code, preserving correct preview output for Mermaid diagrams, Markdown, and mixed content.

**Validates: Requirements 3.1, 3.2**

Property 7: Preservation — Export Functionality

_For any_ Word export request after lazy initialization, the fixed code SHALL produce the same .docx output as the original eager initialization, preserving all export service behavior.

**Validates: Requirements 3.3**

Property 8: Preservation — Non-Startup Functionality

_For any_ user interaction that does not depend on startup ordering (recent files, builder wiring, keyboard shortcuts, error handling), the fixed code SHALL produce exactly the same behavior as the original code.

**Validates: Requirements 3.4, 3.5, 3.6, 3.7, 3.8**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `MermaidDiagramApp/MainWindow.xaml.cs`

**Function**: `MainWindow_Loaded` + constructor

**Specific Changes**:
1. **Reorder initialization**: Move `PopulateRecentFilesMenu()` and `InitializeBuilderWiring()` before `await InitializeWebViewAsync()` so the UI shell is interactive immediately
2. **No constructor changes needed for update check** — the delay is added inside `CheckForMermaidUpdatesAsync()` itself (see change #10)

```csharp
// BEFORE
private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    await InitializeWebViewAsync();
    PopulateRecentFilesMenu();
    InitializeBuilderWiring();
}

// AFTER
private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    PopulateRecentFilesMenu();
    InitializeBuilderWiring();
    await InitializeWebViewAsync();
}
```

---

**File**: `MermaidDiagramApp/MainWindow.WebView.cs`

**Function**: `InitializeWebViewAsync` + new `OnWebViewReady()`

**Specific Changes**:
3. **Create consolidated `OnWebViewReady()` method**: Add a new private method with a guard that sets `_isWebViewReady`, stops the polling timer, and enqueues `UpdatePreview()`. Do NOT call `InitializeMarkdownToWordExport()` here (deferred to lazy init per change #7).

```csharp
private void OnWebViewReady()
{
    if (_isWebViewReady) return;
    _isWebViewReady = true;
    _checkTimer?.Stop();
    _logger.LogInformation("WebView2 ready (consolidated handler)");
    DispatcherQueue.TryEnqueue(async () =>
    {
        try
        {
            _lastPreviewedCode = null;
            await UpdatePreview();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating preview after ready: {ex.Message}", ex);
        }
    });
}
```

4. **Replace all four ready-detection paths**: In the JSON `"ready"` handler, legacy `"MermaidReady"` handler, polling timer tick, and `PreviewBrowser_NavigationCompleted`, replace the inline `_isWebViewReady = true; InitializeMarkdownToWordExport(); UpdatePreview();` logic with a single call to `OnWebViewReady()`.

5. **Reduce polling timer interval**: Change the `DispatcherTimer` interval from `1000ms` to `250ms` as a faster fallback. Adjust the timeout iteration count to maintain ~15 seconds total (60 iterations at 250ms).

6. **Promote `checkTimer` to field**: The `checkTimer` local variable needs to become a field `_checkTimer` so `OnWebViewReady()` can stop it.

---

**File**: `MermaidDiagramApp/MainWindow.MarkdownToWord.cs`

**Function**: `InitializeMarkdownToWordExport` + new `EnsureMarkdownToWordInitialized()`

**Specific Changes**:
7. **Add lazy initialization wrapper**:

```csharp
private void EnsureMarkdownToWordInitialized()
{
    if (_markdownToWordViewModel != null) return;
    InitializeMarkdownToWordExport();
}
```

8. **Call `EnsureMarkdownToWordInitialized()` from usage sites**: Add calls at the top of `ExportToWord_Click` and `OpenMarkdownFile_Click` instead of during `OnWebViewReady()`.

9. **Remove `InitializeMarkdownToWordExport()` calls** from all ready-detection paths in `MainWindow.WebView.cs` (now handled by lazy init in the usage sites).

---

**File**: `MermaidDiagramApp/Assets/UnifiedRenderer.html`

**Function**: `loadLibraries()`

**Specific Changes**:
10. **Parallelize JS loading with `Promise.all`**:

```javascript
// BEFORE
async function loadLibraries() {
    await loadScript('mermaid.min.js', '...');
    await loadScript('markdown-it.min.js', '...');
    await loadScript('highlight.min.js', '...');
}

// AFTER
async function loadLibraries() {
    log('Loading rendering libraries...');
    await Promise.all([
        loadScript('mermaid.min.js', 'https://cdn.jsdelivr.net/npm/mermaid@10.9.0/dist/mermaid.min.js'),
        loadScript('markdown-it.min.js', 'https://cdn.jsdelivr.net/npm/markdown-it@13.0.1/dist/markdown-it.min.js'),
        loadScript('highlight.min.js', 'https://cdn.jsdelivr.net/gh/highlightjs/cdn-release@11.8.0/build/highlight.min.js')
    ]);
    log('All libraries loaded successfully');
}
```

---

**File**: `MermaidDiagramApp/MainWindow.Export.cs`

**Function**: `CheckForMermaidUpdatesAsync()`

**Specific Changes**:
11. **Add 5-second delay at the start** of the method body, before the existing update check logic:

```csharp
private async Task CheckForMermaidUpdatesAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(5));
    // ... existing update check logic unchanged below
    try
    {
        var versionInfo = await _mermaidUpdateService.CheckForUpdatesAsync();
        // ...
    }
}
```

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the defects on unfixed code, then verify the fix works correctly and preserves existing behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the startup performance defects BEFORE implementing the fix. Confirm or refute the root cause analysis.

**Test Plan**: Write tests that verify the ordering and timing of startup operations. Run these tests on the UNFIXED code to observe failures and understand the root cause.

**Test Cases**:
1. **Blocking UI Test**: Assert that `PopulateRecentFilesMenu()` is called before `InitializeWebViewAsync()` completes (will fail on unfixed code — menus populated only after WebView2 init)
2. **Sequential JS Loading Test**: Assert that `loadLibraries()` uses `Promise.all` for concurrent loading (will fail on unfixed code — uses sequential awaits)
3. **Duplicate Ready-Detection Test**: Count how many times `InitializeMarkdownToWordExport()` is called during startup — assert exactly 0 from ready paths (will fail on unfixed code — called 2-4 times)
4. **Eager Export Init Test**: Assert that `_markdownToWordViewModel` is null after WebView2 ready if no export has been triggered (will fail on unfixed code — eagerly created)
5. **Immediate Update Check Test**: Assert that `CheckForMermaidUpdatesAsync()` does not make a network request within the first 5 seconds (will fail on unfixed code — fires immediately)

**Expected Counterexamples**:
- `PopulateRecentFilesMenu()` called 2-5 seconds after `MainWindow_Loaded` fires
- `InitializeMarkdownToWordExport()` called 2-4 times during a single startup
- Network request for mermaid update occurs within 100ms of constructor execution

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed functions produce the expected behavior.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := startupSequence_fixed(input)
  ASSERT menusPopulatedBeforeWebViewInit(result)
  ASSERT jsLibrariesLoadedInParallel(result)
  ASSERT readyDetectionCalledExactlyOnce(result)
  ASSERT exportServicesNotCreatedAtStartup(result)
  ASSERT updateCheckDelayedByAtLeast5Seconds(result)
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold (post-startup interactions), the fixed functions produce the same result as the original functions.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT originalFunction(input) = fixedFunction(input)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain
- It catches edge cases that manual unit tests might miss
- It provides strong guarantees that behavior is unchanged for all non-startup inputs

**Test Plan**: Observe behavior on UNFIXED code first for rendering, export, and UI interactions, then write property-based tests capturing that behavior.

**Test Cases**:
1. **Preview Rendering Preservation**: Verify that Mermaid and Markdown rendering produces identical output after the fix for arbitrary editor content
2. **Export Functionality Preservation**: Verify that lazy-initialized export services produce the same .docx output as eagerly-initialized services
3. **Recent Files Preservation**: Verify that recent files menu loads and displays correctly
4. **Keyboard Shortcut Preservation**: Verify that F11, Escape, F5, F7 shortcuts continue to work in WebView2
5. **Navigation Error Preservation**: Verify that WebView2 navigation failures still display error messages

### Unit Tests

- Test `OnWebViewReady()` idempotency: call multiple times, assert initialization side effects happen exactly once
- Test `EnsureMarkdownToWordInitialized()`: assert lazy creation on first call, no-op on subsequent calls
- Test `CheckForMermaidUpdatesAsync()` delays at least 5 seconds before network activity
- Test `loadLibraries()` uses `Promise.all` (JS unit test verifying concurrent loading)

### Property-Based Tests

- Generate random sequences of ready signals (JSON ready, legacy MermaidReady, polling timer, NavigationCompleted) and verify `OnWebViewReady()` executes initialization exactly once regardless of signal ordering or count
- Generate random startup scenarios (with/without export actions) and verify export services are only created when an export is requested
- Generate random editor content and verify `UpdatePreview` produces identical results before and after the fix

### Integration Tests

- Test full startup flow: launch app, verify editor and menus are interactive before WebView2 finishes initializing
- Test startup-to-first-render: verify preview appears correctly after background WebView2 init completes
- Test lazy export: launch app, trigger Word export, verify export services are created and produce correct output
- Test delayed update check: verify no network activity in the first 5 seconds, then update check runs successfully
