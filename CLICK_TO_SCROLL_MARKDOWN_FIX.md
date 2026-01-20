# Click-to-Scroll Feature Fixed for Markdown Files

## Problem
The click-to-scroll synchronization feature was **only working for Mermaid diagrams** (SVG content) but **not for Markdown files** (HTML content). When clicking anywhere in the code editor while viewing a Markdown file, the logs showed:
- `Scroll result: "no svg"` (repeated)
- `SyncPreviewToLine: No elements available`

Additionally, when the feature was initially implemented, **the anchors were inaccurate** - clicking on one line would scroll to the wrong element in the preview.

## Root Causes
1. **Parser**: Only parsed Mermaid syntax (nodes, subgraphs) - ignored Markdown structure
2. **Injection Script**: Only looked for SVG elements to inject `data-line` attributes
3. **Scroll Script**: Only searched for elements within `<svg>` tags
4. **Matching Logic**: Elements were matched by order, not by content, causing misalignment

## Solution
Extended the system to support **both Mermaid and Markdown** content with **accurate content-based matching**:

### 1. Enhanced Parser (`MermaidCodeParser.cs`)
- Added `ParseMarkdownStructure()` method to parse Markdown elements:
  - **Headings** (H1-H6): `# Heading`, `## Subheading`, etc.
  - **List items**: `- item`, `* item`, `1. item` (extracts just the text, not the marker)
  - **Paragraphs**: Regular text blocks (up to 100 chars for matching)
- Updated `ParseCode()` to accept `isMarkdown` parameter
- Parser now extracts **both line numbers AND text content** for accurate matching
- Improved regex to handle trimmed lines and extract clean text

### 2. Updated Injection Script (`GenerateLineNumberInjectionScript()`)
- Detects rendering mode: `mermaid-mode` vs `markdown-mode`
- **For Mermaid**: Injects `data-line` into SVG elements (existing behavior)
- **For Markdown**: Injects `data-line` into HTML elements using **content-based matching**:
  - Headings: Matches `<h1>`, `<h2>`, etc. by comparing text content
  - Paragraphs: Matches `<p>` by comparing text prefix
  - List items: Matches `<li>` by comparing text content
- Uses `normalizeText()` helper to handle whitespace and case differences
- Only assigns `data-line` to elements that haven't been matched yet (prevents duplicates)
- Includes the `Label` field in JSON for matching

### 3. Updated Scroll Script (`SyncPreviewToLine()`)
- Detects rendering mode automatically
- **For Mermaid**: Searches within `<svg>` for elements with `data-line`
- **For Markdown**: Searches within `.markdown-body` for elements with `data-line`
- Highlights clicked element with:
  - Blue outline: `2px solid #58a6ff`
  - Subtle background (Markdown only): `rgba(88, 166, 255, 0.1)`

### 4. Updated Setup (`SetupScrollSynchronization()`)
- Passes `isMarkdown` flag based on `_currentContentType`
- Works for both `ContentType.Markdown` and `ContentType.MarkdownWithMermaid`

## How It Works Now

### For Markdown Files:
1. User clicks line 5: `## Table of Contents`
2. Parser extracts: `{ Type: "h2", LineNumber: 5, Label: "Table of Contents" }`
3. Injection script finds `<h2>` with text "Table of Contents" and adds `data-line="5"`
4. Scroll script finds `<h2 data-line="5">` and scrolls it into view
5. Element is highlighted with blue outline and background

### For Mermaid Diagrams:
1. User clicks line 10 in the code editor
2. Parser finds the Mermaid node/subgraph at/before line 10
3. Injection script adds `data-line="10"` to the corresponding SVG `<g>` element
4. Scroll script finds `<g data-line="10">` and scrolls it into view
5. Element is highlighted with blue outline

## Key Improvements
✅ **Content-based matching**: Elements are matched by their actual text content, not by order
✅ **Accurate alignment**: Clicking line 5 scrolls to the element from line 5
✅ **Handles variations**: Normalizes whitespace and case for robust matching
✅ **Prevents duplicates**: Only assigns `data-line` once per element
✅ **Better text extraction**: List items show clean text without markers

## Testing
Build the project and test with:
1. **Markdown file** (`.md`): Click on line 5 "## Table of Contents" - should scroll to that exact heading
2. **Mermaid diagram** (`.mmd`): Click on different lines - preview should scroll to corresponding nodes
3. **Mixed content** (Markdown with embedded Mermaid): Both should work accurately

## Files Modified
- `MermaidDiagramApp/Services/MermaidCodeParser.cs`
  - Added `ParseMarkdownStructure()` method with improved text extraction
  - Updated `ParseCode()` signature to accept `isMarkdown` parameter
  - Enhanced `GenerateLineNumberInjectionScript()` with content-based matching logic
  - Now includes `Label` field in JSON for accurate matching
  
- `MermaidDiagramApp/MainWindow.xaml.cs`
  - Updated `SyncPreviewToLine()` for dual-mode scrolling
  - Updated `SetupScrollSynchronization()` to pass `isMarkdown` flag

## Result
✅ Click-to-scroll now works for **both Markdown and Mermaid** content
✅ **Accurate matching** - clicking a line scrolls to the correct element
✅ Automatic detection of content type
✅ Smooth scrolling with visual highlighting
✅ No breaking changes to existing Mermaid functionality
