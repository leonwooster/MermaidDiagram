# Mermaid Syntax Fixer - User Guide

## Overview
The Mermaid Syntax Fixer is a built-in tool that helps you identify and fix common syntax errors in your Mermaid diagrams. It detects problematic Unicode characters and formatting issues that can cause rendering failures.

## How to Use

### Accessing the Syntax Fixer
1. Open a Mermaid diagram file (`.mmd`)
2. Click **Edit** → **Check & Fix Mermaid Syntax** (or press **F7**)

### Review Detected Issues
The Syntax Issues dialog will display:
- **Total issue count** by severity (Errors, Warnings, Info)
- **List of all detected issues** with line and column numbers
- **Live preview** showing before/after comparison for each issue

### Select Issues to Fix
- By default, all issues are selected for fixing
- **Uncheck** individual issues you want to keep unchanged
- Use **Select All** or **Deselect All** buttons for bulk selection
- Click on issue type buttons at the bottom to select all issues of that type

### Apply Fixes
1. Review the selected issues and their proposed fixes
2. Click **Apply Fixes** to update your diagram
3. A success message will confirm how many fixes were applied
4. The diagram preview will automatically refresh

### Cancel Without Changes
Click **Cancel** to close the dialog without making any changes to your diagram.

## Detected Issue Types

### 1. Unicode Dashes
**Problem:** En-dashes (–) and em-dashes (—) are not compatible with Mermaid syntax.

**Example:**
```
Before: Step 1 – Load models
After:  Step 1 - Load models
```

**Why it matters:** Mermaid parser expects standard ASCII hyphens (-) and will fail with Unicode dash characters.

### 2. Unicode Arrows
**Problem:** Unicode arrow symbols (→, ←, ↔, ⇒) cannot be used in Mermaid syntax.

**Example:**
```
Before: encode image → latent
After:  encode image to latent
```

**Why it matters:** Mermaid has its own arrow syntax (-->, <--, etc.) and doesn't recognize Unicode arrows.

### 3. Smart Quotes
**Problem:** Curly quotes (" " ' ') should be straight quotes (" ').

**Example:**
```
Before: "Hello World"
After:  "Hello World"
```

**Why it matters:** Mermaid expects standard ASCII quotes for string literals.

### 4. Line Breaks
**Problem:** Escape sequence `\n` in node labels should be `<br/>` tags.

**Example:**
```
Before: A1[UnetLoader\nModel Name]
After:  A1[UnetLoader<br/>Model Name]
```

**Why it matters:** Mermaid uses HTML-style line breaks in labels, not escape sequences.

## Keyboard Shortcuts
- **F7** - Open Syntax Fixer dialog

## Tips
- Run the syntax fixer before exporting diagrams to ensure clean output
- The fixer preserves your original formatting and indentation
- All fixes can be undone with Ctrl+Z after applying
- Issues are detected in real-time when you open the dialog

## Technical Details

### Architecture
The syntax fixer follows SOLID design principles:
- **ISyntaxAnalyzer** - Interface for detecting syntax issues
- **ISyntaxFixer** - Interface for applying fixes
- **ISyntaxRule** - Interface for individual detection rules
- **MermaidSyntaxAnalyzer** - Concrete analyzer with pluggable rules
- **MermaidSyntaxFixer** - Applies fixes atomically

### Detection Rules
Each rule is independent and can be enabled/disabled:
- `UnicodeDashRule` - Detects U+2013 (en-dash) and U+2014 (em-dash)
- `UnicodeArrowRule` - Detects various Unicode arrow characters
- `SmartQuoteRule` - Detects U+201C, U+201D, U+2018, U+2019
- `LineBreakRule` - Detects `\n` within `[...]` node labels

### Extensibility
New detection rules can be added by:
1. Implementing `ISyntaxRule` interface
2. Adding the rule to `MermaidSyntaxAnalyzer` constructor
3. No changes needed to UI or other components

## Troubleshooting

### "No issues found" but diagram won't render
The syntax fixer only detects common Unicode and formatting issues. Other syntax errors (like invalid Mermaid keywords or structure) are not detected by this tool.

### Fixes didn't apply
Ensure you clicked "Apply Fixes" (not Cancel) and that the issues were checked/selected before applying.

### Want to revert changes
Use **Ctrl+Z** (Undo) immediately after applying fixes to restore the original code.

## Future Enhancements
See `BACKLOG.md` for planned features:
- Configurable detection rules
- Custom regex-based rules
- Batch processing multiple files
- Severity level customization
