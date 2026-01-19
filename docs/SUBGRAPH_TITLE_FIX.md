# Subgraph Title Overflow Fix

## Problem

Mermaid subgraph titles were overflowing and being cut off by other elements, even when using `<br/>` tags for line breaks. This was particularly noticeable with titles like:
- "API Server (Port 8000)"
- "Job Processor Service (Port 3003)"
- "Slides Generator Service (Port 9297)"

## Root Cause

The issue had two components:

1. **Text Length**: Titles were too long to fit in the available horizontal space
2. **CSS Rendering**: Mermaid's default CSS doesn't wrap text in subgraph titles, even with `<br/>` tags

## Solution

The fix involves **both** automatic text optimization AND CSS styling:

### 1. Automatic Text Optimization (MermaidTextOptimizer.cs)

**Features:**
- Detects long subgraph titles (>20 characters)
- Automatically adds `<br/>` tags at natural break points
- Special handling for port numbers: breaks before "(Port XXXX)"
- Reduces clutter by removing package names

**Configuration:**
```csharp
MaxSubgraphTitleLength = 20 characters
PreferredLineLength = 20 characters
```

**Example Transformation:**
```
Before: "API Server (Port 8000) @designsai/api"
After:  "API Server<br/>(Port 8000)"
```

### 2. CSS Text Wrapping (MermaidHost.html & UnifiedRenderer.html)

**Added CSS Rules:**
```css
/* Force text wrapping in subgraph titles */
#content-container .cluster-label,
#content-container .cluster text,
#content-container .cluster span {
    white-space: normal !important;
    word-wrap: break-word !important;
    overflow-wrap: break-word !important;
    max-width: 250px !important;
    display: inline-block !important;
    text-align: center !important;
}

/* Ensure subgraph titles don't overflow */
#content-container .cluster-label {
    max-width: 280px !important;
    line-height: 1.4 !important;
}
```

**Why This Works:**
- `white-space: normal` allows text to wrap
- `word-wrap: break-word` breaks long words if needed
- `max-width: 250px` constrains the title width
- `display: inline-block` enables width constraints
- `!important` overrides Mermaid's default styles

## Files Modified

1. **MermaidDiagramApp/Services/MermaidTextOptimizer.cs**
   - Reduced thresholds for more aggressive line breaking
   - Added special handling for port numbers
   - Improved word-based text splitting

2. **MermaidDiagramApp/Assets/MermaidHost.html**
   - Added CSS rules for text wrapping
   - Applied to cluster labels and node labels

3. **MermaidDiagramApp/Assets/UnifiedRenderer.html**
   - Added same CSS rules for consistency
   - Ensures both renderers handle text the same way

4. **docs/finding/ARCHITECTURE_DIAGRAM.md**
   - Simplified subgraph titles
   - Removed package names (@designsai/...)
   - Applied line breaks manually

## Testing

Run the application and open the architecture diagram:
1. Build: `dotnet build -p:Platform=x64`
2. Run the application
3. Open `docs/finding/ARCHITECTURE_DIAGRAM.md`
4. Verify subgraph titles wrap properly

## How It Works Together

1. **File Load**: When you open a `.mmd` file, the optimizer runs automatically
2. **Text Processing**: Long titles are split with `<br/>` tags
3. **Rendering**: Mermaid renders the diagram with the line breaks
4. **CSS Application**: Browser applies the wrapping CSS rules
5. **Result**: Titles wrap properly and don't overflow

## Limitations

- **Max Width**: Titles are constrained to 250-280px
- **Very Long Words**: Single words longer than the max width may still overflow
- **Theme Dependency**: Some Mermaid themes may override these styles

## Troubleshooting

### Issue: Titles still overflow

**Solutions:**
1. Make titles even shorter (use abbreviations)
2. Increase the CSS `max-width` value
3. Use a different Mermaid theme
4. Split into multiple subgraphs

### Issue: Text wraps too aggressively

**Solutions:**
1. Increase `MaxSubgraphTitleLength` in MermaidTextOptimizer.cs
2. Increase `PreferredLineLength` for longer lines
3. Adjust CSS `max-width` to allow more horizontal space

### Issue: CSS not applied

**Solutions:**
1. Clear browser cache (Ctrl+F5 in the preview)
2. Rebuild the application to copy updated HTML files
3. Check that the HTML files in `bin/Debug/.../Assets/` are updated

## Best Practices

1. **Keep Titles Concise**: Aim for 15-20 characters per line
2. **Use Abbreviations**: "Svc" instead of "Service", "Gen" instead of "Generator"
3. **Avoid Package Names**: Put them in node descriptions instead
4. **Test After Changes**: Always preview diagrams after modifying titles

## Future Improvements

- [ ] Make CSS max-width configurable in settings
- [ ] Add UI option to toggle automatic optimization
- [ ] Support for different text wrapping strategies
- [ ] Per-diagram CSS overrides
- [ ] Visual indicator showing which titles were optimized

---

**Version**: 1.0  
**Last Updated**: January 2026  
**Related**: AUTO_TEXT_OPTIMIZATION.md
