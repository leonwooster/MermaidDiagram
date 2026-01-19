# Subgraph Title Fix - Disable Automatic Text Wrapping

## Problem
Mermaid diagram subgraph titles (e.g., "Job Processor Service (Port 3003)", "Backend Services Layer") were being wrapped to multiple lines despite having ample horizontal space available.

## Root Cause Analysis

After extensive investigation and inspecting the rendered SVG, we discovered **FOUR separate issues** causing the unwanted text wrapping:

### Issue 1: Mermaid's Automatic Text Wrapping Feature ✅ FIXED
**This was the primary cause!**

Mermaid v10.1.0+ introduced automatic text wrapping for markdown strings. This feature creates multiple `<tspan>` elements within SVG `<text>` elements to wrap long text, even when there's ample horizontal space.

**Evidence from inspection:**
```html
<text style>
  <tspan xml:space="preserve" dy="1em" x="0" class="row">Job Processor Service (Port 3003)</tspan>
  <tspan xml:space="preserve" dy="1em" x="0" class="row">@designsai/job-processor</tspan>
</text>
```

The text was being split into multiple tspans by Mermaid's internal wrapping logic.

**Solution:** Set `markdownAutoWrap: false` in Mermaid configuration.

### Issue 2: Mermaid Configuration - `wrappingWidth` Setting ✅ FIXED
The Mermaid library was configured with `wrappingWidth: 200` (later increased to 300) in multiple locations, forcing text to wrap at 200-300 pixels.

### Issue 3: CSS Overrides - Forced Text Wrapping ✅ FIXED
CSS rules were forcing text wrapping with `max-width: 250px !important`, overriding Mermaid's default behavior.

### Issue 4: JavaScript "Fix" Function ✅ DISABLED
The `fixSubgraphTitleOverflow()` function was attempting to "fix" titles by splitting them, which was itself causing problems.

## Solution

### 1. Disable Automatic Text Wrapping ✅ **MOST IMPORTANT**
Added `markdownAutoWrap: false` to Mermaid configuration in **3 locations**:
- `UnifiedRenderer.html` - initial config (line ~347)
- `UnifiedRenderer.html` - renderMermaid config (line ~440)
- `MermaidHost.html` - initialization config (line ~170)

This prevents Mermaid from creating multiple `<tspan>` elements for text wrapping.

### 2. Remove `wrappingWidth` Configuration ✅
Removed all `wrappingWidth` settings from Mermaid initialization in:
- `UnifiedRenderer.html` (2 locations)
- `MermaidHost.html` (1 location)

### 3. Remove Forced CSS Wrapping ✅
Removed the CSS rules that were forcing text to wrap at 250-280px.

### 4. COMPLETELY DISABLE JavaScript Fix ✅
The `fixSubgraphTitleOverflow()` function is now completely disabled - it does nothing.

### 5. Remove Text Splitting Logic ✅
Removed the `splitTextForDisplay()` function entirely - no longer needed.

## Key Finding
✅ **Mermaid v10.1.0+ has automatic text wrapping enabled by default!**  
✅ This feature creates multiple `<tspan>` elements to wrap text.  
✅ Setting `markdownAutoWrap: false` disables this behavior.  
✅ All our other "fixes" (CSS, wrappingWidth, JavaScript) were **also causing** problems.  
✅ By disabling automatic wrapping and removing ALL constraints, titles display on single lines as intended.  
✅ **NO post-processing is needed** - just configure Mermaid correctly.

## Implementation

### Location
- `MermaidDiagramApp/Assets/UnifiedRenderer.html` - `fixSubgraphTitleOverflow()` function (DISABLED)
- `MermaidDiagramApp/Assets/MermaidHost.html` - `fixSubgraphTitleOverflow()` function (DISABLED)

### Current State
**The fix function is completely disabled and does nothing:**
```javascript
function fixSubgraphTitleOverflow(container) {
    // Do nothing - Mermaid handles text layout correctly by default
    log('fixSubgraphTitleOverflow: DISABLED - letting Mermaid handle text layout');
}
```

**Why disabled?**
- Mermaid's default rendering is correct
- The function was causing unnecessary text wrapping
- No post-processing is needed

## Testing Results

After removing ALL constraints (CSS, wrappingWidth, JavaScript fix):
- ✅ "API Server (Port 8000)" - displays on **single line** 
- ✅ "Job Processor Service (Port 3003)" - displays on **single line**
- ✅ "Backend Services Layer" - displays on **single line**
- ✅ "Slides Generator Service (Port 9297)" - displays on **single line**
- ✅ **NO text wrapping** unless text genuinely exceeds available width
- ✅ **NO post-processing needed** - Mermaid handles everything correctly

## Files Modified

1. **MermaidDiagramApp/Assets/UnifiedRenderer.html**
   - **Added `markdownAutoWrap: false`** to initial config (line ~347)
   - **Added `markdownAutoWrap: false`** to renderMermaid config (line ~440)
   - Removed `wrappingWidth` from initial config
   - Removed `wrappingWidth` from renderMermaid config
   - Removed forced CSS wrapping rules
   - DISABLED `fixSubgraphTitleOverflow()` function
   - REMOVED `splitTextForDisplay()` function

2. **MermaidDiagramApp/Assets/MermaidHost.html**
   - **Added `markdownAutoWrap: false`** to initialization (line ~170)
   - Removed `wrappingWidth: 300` from initialization
   - Removed forced CSS wrapping rules
   - DISABLED `fixSubgraphTitleOverflow()` function
   - REMOVED `splitTextForDisplay()` function

## Configuration

**Key Configuration Setting:**
```javascript
markdownAutoWrap: false  // Disables Mermaid's automatic text wrapping (v10.1.0+)
```

This setting is now applied in all Mermaid initialization calls in both HTML files.

## Benefits

1. **Disables Automatic Wrapping**: Prevents Mermaid from creating multiple tspans
2. **Zero Post-Processing**: No JavaScript manipulation of SVG needed
3. **No Forced Wrapping**: Removed all CSS and config constraints
4. **Single-Line Titles**: Titles display on one line when there's adequate space
5. **Clean Code**: Removed unnecessary "fix" functions
6. **No Manual Edits**: Markdown files don't need special formatting
7. **Proper Configuration**: Uses Mermaid's built-in settings correctly

## Lessons Learned

1. **Read the documentation** - Mermaid v10.1.0+ introduced automatic text wrapping
2. **Inspect the actual output** - Looking at the SVG revealed multiple tspans were being created
3. **Configuration over code** - Use `markdownAutoWrap: false` instead of post-processing
4. **CSS `!important` rules can override library behavior** - Use sparingly
5. **Configuration settings like `wrappingWidth` have global effects** - Remove if not needed
6. **Test by disabling fixes** - Sometimes the "fix" is the problem
7. **Check version-specific features** - New versions may introduce breaking changes

## References

- [Mermaid Automatic Text Wrapping Blog Post](https://www.mermaidchart.com/blog/posts/automatic-text-wrapping-in-flowcharts-is-here)
- [Mermaid Config Schema - markdownAutoWrap](https://mermaid.js.org/config/schema-docs/config-properties-markdownautowrap.html)
- [GitHub Issue #4391 - Markdown strings without automatic wrapping](https://github.com/mermaid-js/mermaid/issues/4391)

## How to Test

1. Open the app and load any diagram with subgraph titles
2. Press **Ctrl+Shift+D** to open the debug panel
3. Look for the message: "fixSubgraphTitleOverflow: DISABLED - letting Mermaid handle text layout"
4. Verify titles render on single lines without wrapping

## Related Files

- `MermaidDiagramApp/Assets/UnifiedRenderer.html` - Implementation
- `MermaidDiagramApp/Services/Rendering/MermaidRenderer.cs` - Rendering orchestration

---

**Last Updated**: January 19, 2026  
**Version**: 5.0.0 (Added `markdownAutoWrap: false` to disable Mermaid's automatic text wrapping feature)
