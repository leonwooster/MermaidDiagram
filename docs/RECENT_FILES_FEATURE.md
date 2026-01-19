# Recent Files Feature

## Overview
The Recent Files feature tracks the last 100 opened files and allows users to quickly reopen them from the File menu.

## Features

### Core Functionality
- **Tracks up to 100 recent files** - Automatically maintains a list of recently opened files
- **Persistent storage** - Recent files list is saved to disk and persists across app sessions
- **Smart ordering** - Most recently opened files appear first
- **File validation** - Automatically detects and handles missing files
- **Supports all file types** - Works with .mmd, .md, .markdown, and .mmdx files

### User Interface
- **Recent Files submenu** - Located in File menu, between "Open" and "Close"
- **Shows up to 20 files** - Displays the 20 most recent files in the menu for usability
- **Full path tooltips** - Hover over a file to see its complete path
- **Clear option** - "Clear Recent Files" button to remove all entries
- **Empty state** - Shows "(No recent files)" when list is empty

### File Operations
- **Click to open** - Click any recent file to open it immediately
- **Unsaved changes prompt** - Warns if current file has unsaved changes
- **Missing file handling** - Prompts to remove missing files from the list
- **Auto-optimization** - Applies text optimization for .mmd files
- **Export integration** - Automatically loads .md/.mmd files for Word export

## Implementation

### Files Created
1. **MermaidDiagramApp/Services/RecentFilesService.cs**
   - Core service for managing recent files
   - Handles persistence to JSON file
   - Provides methods for add, remove, clear, and cleanup operations

### Files Modified
1. **MermaidDiagramApp/MainWindow.xaml**
   - Added "Recent Files" submenu to File menu

2. **MermaidDiagramApp/MainWindow.xaml.cs**
   - Added `_recentFilesService` field
   - Added `PopulateRecentFilesMenu()` method
   - Added `RecentFile_Click()` event handler
   - Added `OpenRecentFile()` method
   - Added `ClearRecentFiles_Click()` event handler
   - Integrated tracking in `Open_Click()` method
   - Calls `PopulateRecentFilesMenu()` in `MainWindow_Loaded()`

### Data Storage
- **Location**: `%LocalAppData%\Packages\[AppPackageId]\LocalState\recent-files.json`
- **Format**: JSON array of recent file entries
- **Structure**:
  ```json
  [
    {
      "FilePath": "C:\\Users\\...\\diagram.mmd",
      "FileName": "diagram.mmd",
      "LastOpened": "2026-01-19T10:30:00"
    }
  ]
  ```

## Usage

### Opening Recent Files
1. Click **File** → **Recent Files**
2. Select a file from the list
3. The file opens immediately (with unsaved changes prompt if needed)

### Clearing Recent Files
1. Click **File** → **Recent Files** → **Clear Recent Files**
2. All recent files are removed from the list

### Handling Missing Files
- If you click a file that no longer exists, you'll be prompted to remove it
- Click "Remove" to clean it from the list
- Click "Cancel" to keep it in the list

## Technical Details

### RecentFilesService API

```csharp
public class RecentFilesService
{
    // Properties
    public IReadOnlyList<RecentFileEntry> RecentFiles { get; }
    
    // Methods
    public void AddRecentFile(string filePath);
    public void RemoveRecentFile(string filePath);
    public void ClearRecentFiles();
    public void CleanupRecentFiles(); // Removes non-existent files
}

public class RecentFileEntry
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public DateTime LastOpened { get; set; }
}
```

### Automatic Tracking
Recent files are automatically tracked when:
- Opening a file via File → Open
- Opening a recent file from the Recent Files menu
- Successfully loading a .mmdx diagram builder file

### Menu Population
The Recent Files menu is populated:
- On application startup (`MainWindow_Loaded`)
- After opening any file
- After clearing recent files
- After removing a missing file

## Benefits

1. **Improved Productivity** - Quick access to frequently used files
2. **Better Workflow** - No need to navigate file system repeatedly
3. **User-Friendly** - Intuitive interface integrated into existing menu
4. **Reliable** - Handles edge cases like missing files gracefully
5. **Persistent** - Remembers files across app sessions

## Future Enhancements

Possible improvements:
- Pin favorite files to top of list
- Group files by folder or project
- Search/filter recent files
- Keyboard shortcuts for recent files (Ctrl+1, Ctrl+2, etc.)
- Show file preview on hover
- Export/import recent files list
- Configurable maximum number of recent files

---

**Version**: 1.0.0  
**Last Updated**: January 19, 2026
