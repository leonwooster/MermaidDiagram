# Startup Performance Recommendations

Analysis of the application startup flow identified several bottlenecks. Below are actionable recommendations ranked by impact.

---

## 1. WebView2 Initialization (Critical — 2-5s)

`InitializeWebViewAsync()` is the single biggest bottleneck. It blocks `MainWindow_Loaded`, which means the user can't interact with anything (editor, menus, recent files) until WebView2 is fully ready.

### Problem

```
MainWindow_Loaded
  └─ await InitializeWebViewAsync()    ← blocks everything
  └─ PopulateRecentFilesMenu()         ← waits for WebView2
  └─ InitializeBuilderWiring()         ← waits for WebView2
```

### Recommendation

Show the UI shell immediately. Let WebView2 initialize in the background while the user can already see and use the code editor.

```csharp
private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    // These can run immediately — no WebView2 dependency
    PopulateRecentFilesMenu();
    InitializeBuilderWiring();

    // WebView2 init runs without blocking the rest of the UI
    await InitializeWebViewAsync();
}
```

---

## 2. Sequential JS Library Loading (High — 500ms-1s)

In `UnifiedRenderer.html`, mermaid.js, markdown-it, and highlight.js load one after another with `await`. These are independent scripts.

### Current Code

```javascript
async function loadLibraries() {
    await loadScript('mermaid.min.js', '...');
    await loadScript('markdown-it.min.js', '...');
    await loadScript('highlight.min.js', '...');
}
```

### Recommendation

```javascript
async function loadLibraries() {
    await Promise.all([
        loadScript('mermaid.min.js', 'https://cdn.jsdelivr.net/npm/mermaid@10.9.0/dist/mermaid.min.js'),
        loadScript('markdown-it.min.js', 'https://cdn.jsdelivr.net/npm/markdown-it@13.0.1/dist/markdown-it.min.js'),
        loadScript('highlight.min.js', 'https://cdn.jsdelivr.net/gh/highlightjs/cdn-release@11.8.0/build/highlight.min.js')
    ]);
}
```

---

## 3. Redundant Ready-Detection (Medium — up to 1s wasted)

There are four separate code paths that set `_isWebViewReady = true`:

1. The `"ready"` JSON WebMessage handler
2. The legacy `"MermaidReady"` string handler
3. The 1-second polling timer in `InitializeWebViewAsync`
4. `PreviewBrowser_NavigationCompleted`

Each one independently calls `InitializeMarkdownToWordExport()` and `UpdatePreview()`, risking duplicate work.

### Recommendation

Consolidate into a single `OnWebViewReady()` method with a guard:

```csharp
private void OnWebViewReady()
{
    if (_isWebViewReady) return; // Already handled
    _isWebViewReady = true;
    _checkTimer?.Stop();
    InitializeMarkdownToWordExport(); // or defer — see #4
    DispatcherQueue.TryEnqueue(async () => await UpdatePreview());
}
```

Call `OnWebViewReady()` from all four paths instead of duplicating the logic.

Additionally, the polling timer interval is 1000ms. If the HTML page's `"ready"` message is already working reliably, the timer can be removed entirely. If kept as a fallback, reduce the interval to 200-300ms.

---

## 4. Eager Markdown-to-Word Export Init (Medium — 200-500ms)

`InitializeMarkdownToWordExport()` creates five objects (`MarkdigMarkdownParser`, `OpenXmlWordDocumentGenerator`, `CoreWebView2Wrapper`, `WebView2MermaidImageRenderer`, `MarkdownToWordExportService`) the moment WebView2 is ready. Most sessions never use Word export.

### Recommendation

Lazy-initialize on first use:

```csharp
private void EnsureMarkdownToWordInitialized()
{
    if (_markdownToWordViewModel != null) return;
    InitializeMarkdownToWordExport();
}
```

Call `EnsureMarkdownToWordInitialized()` in `ExportToWord_Click` and `OpenMarkdownFile_Click` instead of during startup.

---

## 5. Mermaid Update Check During Construction (Low — 500ms-2s)

`CheckForMermaidUpdatesAsync()` fires during the MainWindow constructor. While it's fire-and-forget, it competes for network and CPU resources during the critical startup window.

### Recommendation

Delay it so it runs after the app is fully loaded:

```csharp
private async Task CheckForMermaidUpdatesAsync()
{
    await Task.Delay(TimeSpan.FromSeconds(5));
    // ... existing update check logic
}
```

---

## 6. DI Container Instantiates Everything Upfront (Low — 100-500ms)

All 20+ services are registered as singletons and instantiated during `ConfigureServices()`. Services like `MermaidSyntaxAnalyzer`, `MermaidSyntaxFixer`, and `MermaidTextOptimizer` are not needed at startup.

### Recommendation

Use factory registrations or `Lazy<T>` for services not required during initialization:

```csharp
// Instead of:
services.AddTransient<MermaidSyntaxAnalyzer>();

// Use:
services.AddTransient<Lazy<MermaidSyntaxAnalyzer>>();
```

Or register with a factory delegate so instantiation is deferred until first resolution.

---

## Estimated Startup Timeline

### Before Optimization

| Time | Event |
|------|-------|
| T+0ms | App constructor, DI container built |
| T+100ms | MainWindow created, services injected |
| T+110ms | `MainWindow_Loaded` fires |
| T+120ms | `InitializeWebViewAsync()` starts — **UI blocked** |
| T+200ms | `EnsureCoreWebView2Async()` completes |
| T+500ms | HTML/CSS/JS assets loading (sequential) |
| T+1500ms | First polling check for mermaid/md globals |
| T+2500ms | Renderers ready, `InitializeMarkdownToWordExport()` |
| T+3000ms | First render, recent files menu populated |
| T+3500ms | **UI fully interactive** |

### After Optimization (Estimated)

| Time | Event |
|------|-------|
| T+0ms | App constructor, DI container built |
| T+100ms | MainWindow created, services injected |
| T+110ms | `MainWindow_Loaded` fires |
| T+120ms | Recent files menu populated, builder wired — **editor usable** |
| T+130ms | `InitializeWebViewAsync()` starts in background |
| T+500ms | JS libraries loaded in parallel |
| T+800ms | `"ready"` message received, first render |
| T+1000ms | **Preview fully interactive** |

Estimated improvement: **~2-3 seconds faster** to first interactive state.
