# White Box and Z-Order Problems - Analysis and Fixes

## Problems Summary

### 1. White Box Problem
Mermaid diagrams were exporting to Word documents with white boxes instead of proper diagram images.

### 2. Z-Order Problem (Lines Above Text)
Connector lines were appearing **above** text labels on the connectors, making the text hard to read.

## Root Causes and Fixes

### Problem 1: foreignObject Elements in SVG (WHITE BOX - FIXED)
- **Problem**: Mermaid.js was generating SVG with `<foreignObject>` elements when `htmlLabels: true`
- **Impact**: These elements don't render properly when converted to PNG via Svg.Skia library, resulting in white boxes
- **Fix Applied**: Modified `UnifiedRenderer.html` to force `htmlLabels: false` for all diagram types
- **Location**: Lines 340-360, 431-435, 573-581, 756-764 in `MermaidDiagramApp/Assets/UnifiedRenderer.html`

```javascript
// CRITICAL: Re-initialize with htmlLabels: false before EACH render
mermaid.initialize({
    startOnLoad: false,
    securityLevel: 'loose',
    theme: theme.toLowerCase(),
    logLevel: 3,
    flowchart: { htmlLabels: false, useMaxWidth: true },
    sequence: { htmlLabels: false },
    gantt: { htmlLabels: false },
    class: { htmlLabels: false },
    state: { htmlLabels: false }
});
```

### Problem 2: SVG Z-Order (LINES ABOVE TEXT - FIXED)
- **Problem**: In SVG, elements are drawn in DOM order. Edge labels appeared before edges in the DOM, so lines drew on top of text
- **Impact**: Text labels on connector lines were obscured by the lines themselves
- **Fix Applied**: Modified `fixSvgZOrder` function to reorder SVG elements, moving edge labels to the end
- **Location**: Lines 645-730 in `MermaidDiagramApp/Assets/UnifiedRenderer.html`

```javascript
// Step 2: Fix z-order by moving edge labels to the end
// This ensures labels appear on top of connector lines
const rootGroup = svg.querySelector('g');
if (rootGroup) {
    const labelGroups = Array.from(rootGroup.querySelectorAll('g.edgeLabel'));
    
    if (labelGroups.length > 0) {
        // Remove labels from their current position and append to end
        labelGroups.forEach(labelGroup => {
            const parent = labelGroup.parentNode;
            if (parent) {
                parent.removeChild(labelGroup);
                parent.appendChild(labelGroup);
            }
        });
        
        log('Z-order fixed: edge labels moved to end (will render on top)');
    }
}
```

### Problem 3: Line Number Mismatch (SECONDARY ISSUE - IMPROVED LOGGING)
- **Problem**: When `.mmd` files are wrapped in markdown, line numbers need careful tracking
- **Impact**: Mermaid blocks might not be matched with their rendered images
- **Fix Applied**: Added detailed logging to track line number matching
- **Location**: `MermaidDiagramApp/Services/Export/MarkdownToWordExportService.cs` line 468-481

## Evidence from Logs

### Failed Export - White Box Issue (Before Fix)
```
[2026-01-15 23:35:34.584] Found mermaid block: False, RenderedImagePath: null
[2026-01-15 23:35:34.585] Mermaid diagram rendering failed - no rendered path or file doesn't exist
```

### Successful Export (After Fixes)
```
[2026-01-16 00:23:58.367] SVG converted successfully using sanitized version
[2026-01-16 00:23:58.413] Found mermaid block: True
[2026-01-16 00:23:58.413] File exists check: True
[2026-01-16 00:23:58.447] Image added successfully
```

### Z-Order Fix Verification (Check Logs For)
```
Reordering X edge label groups to appear on top
Z-order fixed: edge labels moved to end (will render on top)
```

## Testing Recommendations

### 1. Test Pure Mermaid Files (.mmd) - White Box Fix
```bash
# Open SampleDiagram3.mmd and export to Word
# Verify the diagram appears correctly (not as white box)
```

### 2. Test Z-Order Fix - Lines Above Text
```bash
# Open any diagram with labeled connectors (e.g., SampleDiagram3.mmd)
# Export to Word
# Verify text labels on connector lines are readable (not obscured by lines)
# Check that text appears ABOVE the connector lines
```

### 3. Test Markdown with Mermaid (.md)
```bash
# Open TestWordExport.md and export to Word
# Verify embedded mermaid diagrams render correctly
# Verify text labels are visible
```

### 4. Test Different Diagram Types
- Flowcharts (flowchart LR/TD) - **Most affected by z-order issue**
- Sequence diagrams
- Class diagrams
- State diagrams
- Gantt charts

### 5. Verify No foreignObject Elements
Check the logs for:
```
SUCCESS: SVG does not contain foreignObject
```

If you see:
```
WARNING: SVG still contains foreignObject despite htmlLabels: false!
```
This indicates a Mermaid.js version issue or theme override.

### 6. Verify Z-Order Fix Applied
Check the logs for:
```
Reordering X edge label groups to appear on top
Z-order fixed: edge labels moved to end (will render on top)
```

## Visual Comparison

### Before Z-Order Fix
```
┌─────────┐
│  Node A │────────[Label]────────┐
└─────────┘    ↑                  │
               │                  ↓
          Line covers text   ┌─────────┐
                             │  Node B │
                             └─────────┘
```

### After Z-Order Fix
```
┌─────────┐
│  Node A │────────[Label]────────┐
└─────────┘                       │
          Text clearly visible    ↓
                             ┌─────────┐
                             │  Node B │
                             └─────────┘
```

## Additional Improvements Made

1. **Enhanced Logging**: Added detailed logging throughout the export pipeline to track:
   - Line numbers during parsing
   - Mermaid block matching
   - File existence checks
   - SVG conversion success/failure

2. **Better Error Messages**: More descriptive error messages when diagrams fail to render

## Files Modified

1. **`MermaidDiagramApp/Assets/UnifiedRenderer.html`**
   - **White Box Fix**: Added `htmlLabels: false` configuration in multiple locations
   - **Z-Order Fix**: Enhanced `fixSvgZOrder` function to reorder edge labels to appear on top
   - Added logging for foreignObject detection and z-order operations

2. **`MermaidDiagramApp/Services/Export/MarkdownToWordExportService.cs`**
   - Improved line number matching logic
   - Added detailed logging for debugging

## How the Z-Order Fix Works

The fix manipulates the SVG DOM structure:

1. **Parse SVG**: Convert SVG string to DOM
2. **Find Edge Labels**: Locate all `g.edgeLabel` elements
3. **Reorder Elements**: Remove edge labels from their current position and append them to the end of their parent
4. **Result**: Edge labels now render last, appearing on top of connector lines

This works because SVG rendering follows the "painter's algorithm" - elements are drawn in the order they appear in the DOM, with later elements appearing on top of earlier ones.

## Next Steps

1. **Test both fixes**: 
   - Export SampleDiagram3.mmd to Word
   - Verify no white boxes (white box fix)
   - Verify text labels are clearly visible on connector lines (z-order fix)

2. **Monitor logs**: Check for:
   - "SUCCESS: SVG does not contain foreignObject"
   - "Z-order fixed: edge labels moved to end (will render on top)"
   - "Reordering X edge label groups to appear on top"

3. **Report results**: If issues persist, check logs for:
   - foreignObject warnings
   - Line number mismatches
   - SVG conversion failures
   - Missing z-order fix messages

## Known Limitations

- The fix relies on Mermaid.js respecting the `htmlLabels: false` configuration
- Some diagram types or themes might still generate foreignObject elements
- If foreignObject elements persist, the fallback is WebView2 screenshot (slower but reliable)
