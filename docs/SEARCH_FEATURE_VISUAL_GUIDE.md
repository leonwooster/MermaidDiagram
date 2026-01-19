# Search Feature Visual Guide

## UI Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ File  Edit  View  Export  Help                                  │
│       └─ Find... (Ctrl+F)                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ Find: [Search in code...] [▲] [▼] [3 of 15] [✕]          │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                   │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 1  graph TD                                                  │ │
│ │ 2      A[Start] --> B[Process]                              │ │
│ │ 3      B --> C[End]                                         │ │
│ │ 4                                                            │ │
│ │                                                              │ │
│ │                  CODE EDITOR                                │ │
│ │                                                              │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Search Panel Components

```
┌──────────────────────────────────────────────────────────────┐
│ Find: [Search text box] [▲] [▼] [Results] [✕]               │
│       └─ Type here      │   │   │          └─ Close (Esc)   │
│                         │   │   └─ Shows "X of Y"           │
│                         │   └─ Find Next (Enter)            │
│                         └─ Find Previous (Shift+Enter)      │
└──────────────────────────────────────────────────────────────┘
```

## User Flow

### Opening Search
```
User Action                    Result
───────────────────────────────────────────────────────
Press Ctrl+F          →        Search panel appears
                               Focus moves to search box
                               
OR

Click Edit > Find...  →        Search panel appears
                               Focus moves to search box
```

### Performing Search
```
User Action                    Result
───────────────────────────────────────────────────────
Type "graph"          →        Finds all occurrences
                               Shows "1 of 5" (if 5 matches)
                               
Press Enter           →        Shows "2 of 5"
                               (moves to next match)
                               
Press Shift+Enter     →        Shows "1 of 5"
                               (moves to previous match)
                               
Click ▼ button        →        Shows "2 of 5"
                               (same as Enter)
                               
Click ▲ button        →        Shows "1 of 5"
                               (same as Shift+Enter)
```

### Closing Search
```
User Action                    Result
───────────────────────────────────────────────────────
Press Escape          →        Search panel disappears
                               Focus returns to editor
                               
Click ✕ button        →        Search panel disappears
                               Focus returns to editor
```

## Search Behavior

### Case Sensitivity
```
Search Term: "graph"

Matches:
✅ graph TD
✅ GRAPH LR
✅ Graph TB
✅ GrApH RL
```

### Circular Navigation
```
Matches: [1] [2] [3] [4] [5]

Current: 5 of 5
Press Enter → Goes to: 1 of 5 (wraps to beginning)

Current: 1 of 5
Press Shift+Enter → Goes to: 5 of 5 (wraps to end)
```

### No Results
```
Search Term: "xyz123"

Result:
┌──────────────────────────────────────────────────────┐
│ Find: [xyz123] [▲] [▼] [No results] [✕]             │
└──────────────────────────────────────────────────────┘
```

### Empty Search
```
Search Term: (empty)

Result:
┌──────────────────────────────────────────────────────┐
│ Find: [          ] [▲] [▼] [        ] [✕]            │
└──────────────────────────────────────────────────────┘
```

## Keyboard Shortcuts Summary

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Open search panel |
| `Enter` | Find next match |
| `Shift+Enter` | Find previous match |
| `Escape` | Close search panel |

## Tooltips

Hover over buttons to see helpful tooltips:

- **▲ button**: "Find Previous (Shift+F3)"
- **▼ button**: "Find Next (F3)"
- **✕ button**: "Close (Esc)"

## Current Limitations

### What You'll See
```
Search finds: "graph" (3 of 5)

Code Editor:
┌─────────────────────────────────────┐
│ 1  graph TD                         │  ← Match 1 (not highlighted)
│ 2      A[Start] --> B[Process]      │
│ 3  graph LR                         │  ← Match 2 (not highlighted)
│ 4      X --> Y                      │
│ 5  graph TB                         │  ← Match 3 (current, not highlighted)
│ 6      M --> N                      │
└─────────────────────────────────────┘
```

**Note**: The matches are found and counted, but not visually highlighted in the editor. You'll need to manually locate the text using the line numbers and context.

## Tips for Users

1. **Use specific search terms** to reduce the number of matches
2. **Watch the counter** to track your position (e.g., "3 of 15")
3. **Use keyboard shortcuts** for faster navigation
4. **Search is case-insensitive** - "Graph" finds "graph", "GRAPH", etc.
5. **Navigation wraps around** - you can cycle through all matches

## Example Workflow

```
1. Open file with Mermaid diagram
2. Press Ctrl+F
3. Type "subgraph"
4. See "1 of 8" - there are 8 subgraphs
5. Press Enter repeatedly to cycle through all 8
6. Manually locate each one in the editor
7. Press Escape when done
```

## Integration with Other Features

### Works With
- ✅ Recent Files feature
- ✅ Syntax checking
- ✅ Mermaid rendering
- ✅ File operations (Open, Save, etc.)

### Doesn't Work With
- ❌ Synchronized scrolling (not implemented)
- ❌ Text selection (TextControlBox limitation)
- ❌ Auto-scroll to match (TextControlBox limitation)
