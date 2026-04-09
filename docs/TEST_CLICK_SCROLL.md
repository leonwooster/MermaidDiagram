# Test Click-to-Scroll Feature

## Step-by-Step Test

### 1. Run the Application

Run the application from Visual Studio or the executable.

### 2. Open a Mermaid File

Open `ClickSyncTest.mmd` or any `.mmd` file.

### 3. Click in the Code Editor

Click on different lines in the left panel (code editor):
- Click on line 2 (where a node is defined)
- Click on line 3
- Click on line 4

### 4. Check the Log File

Run this PowerShell command to view the logs:

```powershell
.\ViewLogs.ps1
```

Or manually open:
```
%LOCALAPPDATA%\MermaidDiagramApp\Logs\MermaidDiagramApp-YYYYMMDD.log
```

## What to Look For in Logs

### When App Starts:
```
[INFO] Initializing synchronized scrolling...
[INFO] Synchronized scrolling initialized (click-based) - Event handler attached
```

### When You Open a File:
```
[INFO] Parsed X elements for scroll sync
[INFO] Line markers injected into SVG
```

### When You Click in Code:
```
[INFO] CodeEditor clicked!
[INFO] Current line index: 2
[INFO] Elements count: 6
[INFO] WebView ready: True
[INFO] Clicked on line 2, syncing...
[DEBUG] Syncing to element 'A' at line 2
[DEBUG] Scroll result: "success"
```

## Troubleshooting

### If You Don't See "CodeEditor clicked!" in Logs

**Problem**: Click event handler not firing

**Possible Causes**:
1. Event handler not attached
2. Clicking on wrong area (try clicking on the text, not margins)
3. CodeEditor control not initialized

**Solution**: Check if you see "Event handler attached" in logs

### If You See "Current line index: -1"

**Problem**: TextControlBox not reporting cursor position

**Possible Causes**:
1. `CurrentLineIndex` property not available
2. Cursor position not updated yet

**Solution**: This is a TextControlBox limitation - we may need a different approach

### If You See "Elements count: 0"

**Problem**: Parser didn't find any nodes

**Possible Causes**:
1. File not rendered yet
2. Parser regex doesn't match your code format

**Solution**: Make sure file is rendered before clicking

### If You See "WebView ready: False"

**Problem**: WebView not initialized

**Solution**: Wait a few seconds after opening file, then try clicking

### If You See "element not found" in Scroll Result

**Problem**: Line markers not injected into SVG

**Possible Causes**:
1. Injection script failed
2. SVG structure different than expected

**Solution**: Check browser console for errors

## Alternative: Check if Event Handler is Attached

Add this to the log search:
```
"Event handler attached"
```

If you don't see this message, the initialization failed.

## Next Steps

1. **Run the app**
2. **Open a `.mmd` file**
3. **Click on line 2 in the code**
4. **Run `.\ViewLogs.ps1`**
5. **Share the log output** - especially these sections:
   - Initialization messages
   - "CodeEditor clicked!" messages
   - Any error messages

This will tell us exactly what's happening (or not happening) when you click.
