# Bugfix Requirements Document

## Introduction

The application exhibits slow startup performance, taking approximately 3-3.5 seconds before the user can interact with the UI. The root cause is a combination of blocking WebView2 initialization in `MainWindow_Loaded`, sequential JavaScript library loading, redundant WebView2 ready-detection logic, eager initialization of rarely-used export services, a resource-competing mermaid update check during construction, and upfront DI container instantiation. The fix targets reducing time-to-first-interactive by ~2-3 seconds so the editor, menus, and recent files are usable immediately while the preview pane initializes in the background.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN the application starts THEN the system blocks the entire UI in `MainWindow_Loaded` by awaiting `InitializeWebViewAsync()` before running `PopulateRecentFilesMenu()` and `InitializeBuilderWiring()`, preventing user interaction for 2-5 seconds

1.2 WHEN `UnifiedRenderer.html` loads its JavaScript libraries THEN the system loads mermaid.js, markdown-it, and highlight.js sequentially with individual `await` calls, adding 500ms-1s of unnecessary wait time

1.3 WHEN WebView2 becomes ready THEN the system has four separate code paths (JSON "ready" message handler, legacy "MermaidReady" string handler, 1-second polling timer, and `PreviewBrowser_NavigationCompleted`) that each independently set `_isWebViewReady = true` and call `InitializeMarkdownToWordExport()` and `UpdatePreview()`, risking duplicate initialization work and wasting up to 1 second on redundant polling

1.4 WHEN WebView2 becomes ready THEN the system eagerly creates five Markdown-to-Word export service instances (`MarkdigMarkdownParser`, `OpenXmlWordDocumentGenerator`, `CoreWebView2Wrapper`, `WebView2MermaidImageRenderer`, `MarkdownToWordExportService`) even though most sessions never use Word export, adding 200-500ms to startup

1.5 WHEN the MainWindow constructor executes THEN the system fires `CheckForMermaidUpdatesAsync()` immediately, competing for network and CPU resources during the critical startup window and adding 500ms-2s of resource contention

### Expected Behavior (Correct)

2.1 WHEN the application starts THEN the system SHALL show the UI shell immediately (editor, menus, recent files menu populated, builder wired) without waiting for WebView2 initialization, and SHALL initialize WebView2 in the background

2.2 WHEN `UnifiedRenderer.html` loads its JavaScript libraries THEN the system SHALL load mermaid.js, markdown-it, and highlight.js in parallel using `Promise.all` to minimize total load time

2.3 WHEN WebView2 becomes ready THEN the system SHALL use a single consolidated `OnWebViewReady()` method with a guard (`if (_isWebViewReady) return;`) called from all detection paths, eliminating duplicate `InitializeMarkdownToWordExport()` and `UpdatePreview()` calls, and SHALL either remove the polling timer or reduce its interval to 200-300ms as a fallback

2.4 WHEN WebView2 becomes ready THEN the system SHALL defer Markdown-to-Word export service initialization until the user first requests a Word export operation (lazy initialization)

2.5 WHEN the MainWindow constructor executes THEN the system SHALL delay `CheckForMermaidUpdatesAsync()` by at least 5 seconds so it does not compete for resources during the critical startup window

### Unchanged Behavior (Regression Prevention)

3.1 WHEN WebView2 finishes initializing in the background THEN the system SHALL CONTINUE TO render the preview correctly for the current editor content

3.2 WHEN the user edits code in the editor after startup THEN the system SHALL CONTINUE TO update the preview within the existing <1 second latency target

3.3 WHEN the user triggers a Markdown-to-Word export THEN the system SHALL CONTINUE TO initialize all required export services and produce a correct .docx file

3.4 WHEN the user opens a recent file from the menu THEN the system SHALL CONTINUE TO load and display the file correctly

3.5 WHEN the application checks for Mermaid.js updates THEN the system SHALL CONTINUE TO detect available updates and notify the user

3.6 WHEN the user uses the visual diagram builder THEN the system SHALL CONTINUE TO function correctly after builder wiring completes during startup

3.7 WHEN WebView2 navigation fails THEN the system SHALL CONTINUE TO display an error message in the preview pane

3.8 WHEN the user uses keyboard shortcuts (F11, Escape) in the WebView THEN the system SHALL CONTINUE TO handle them correctly
