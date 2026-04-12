// MainWindow.FileOps.cs
// Partial class for MainWindow containing file operations (open, save, close),
// recent files management, dialog utilities, and file export loading helpers.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        #region File Operations (Open, Save, Close)

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            // Check for unsaved changes in builder before opening
            if (_isBuilderVisible && DiagramCanvasControl != null && DiagramCanvasControl.ViewModel.HasUnsavedChanges)
            {
                var result = await ShowUnsavedChangesDialog();
                if (result == ContentDialogResult.None) // Cancel
                {
                    return;
                }
                else if (result == ContentDialogResult.Primary) // Save
                {
                    Save_Click(this, new RoutedEventArgs());
                    // Wait a bit for save to complete
                    await Task.Delay(100);
                }
                // Secondary = Discard, continue with open
            }
            
            var openPicker = new FileOpenPicker();
            WinRT_InterOp.InitializeWithWindow(openPicker, this);

            openPicker.FileTypeFilter.Add(".mmdx");
            openPicker.FileTypeFilter.Add(".mmd");
            openPicker.FileTypeFilter.Add(".md");
            openPicker.FileTypeFilter.Add(".markdown");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                _currentFilePath = file.Path;
                SetupFileWatcher(file.Path);

                // Check if opening .mmdx (Diagram Builder File)
                if (file.FileType.ToLower() == ".mmdx")
                {
                    var diagramFile = await _diagramFileService.LoadDiagramAsync(file.Path);
                    if (diagramFile != null)
                    {
                        // Show builder if not already visible
                        if (!_isBuilderVisible)
                        {
                            _isBuilderVisible = true;
                            BuilderTool.IsChecked = true;
                            UpdateBuilderVisibility();
                        }
                        
                        // Restore diagram to canvas
                        if (DiagramCanvasControl != null)
                        {
                            DiagramCanvasControl.ShowLoading();
                            DiagramCanvasControl.ClearVisuals();
                            DiagramCanvasControl.ViewModel.ClearCanvas();
                            await Task.Delay(50);
                            _diagramFileService.RestoreDiagram(diagramFile, DiagramCanvasControl.ViewModel);
                            DiagramCanvasControl.ViewModel.MarkAsSaved();
                            await Task.Delay(150);
                            DiagramCanvasControl.HideLoading();
                            DiagramCanvasControl.Focus(FocusState.Programmatic);
                            CodeEditor.Text = diagramFile.MermaidCode;
                            await UpdatePreview();
                        }
                        
                        _logger.LogInformation($"Diagram Builder file loaded: {file.Path}");
                        UpdateWindowTitle();
                        _fileOperationsService.AddRecentFile(file.Path);
                        PopulateRecentFilesMenu();
                    }
                    else
                    {
                        _logger.LogError($"Failed to load Diagram Builder file: {file.Path}");
                        var errorDialog = new ContentDialog
                        {
                            Title = "Open Error",
                            Content = "Failed to load the diagram file. The file may be corrupted or in an unsupported format.",
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
                else
                {
                    // Open as plain text file
                    var fileContent = await FileIO.ReadTextAsync(file);
                    
                    // Auto-optimize Mermaid diagrams for text overflow
                    if (file.FileType.ToLower() == ".mmd")
                    {
                        if (_fileOperationsService.NeedsMermaidOptimization(fileContent))
                        {
                            fileContent = _fileOperationsService.OptimizeMermaidContent(fileContent);
                            _logger.LogInformation("Diagram text was automatically optimized to prevent overflow");
                        }
                    }

                    // Tab dedup: if file is already open, switch to existing tab
                    var existingTab = _tabService.FindTabByFilePath(file.Path);
                    if (existingTab != null)
                    {
                        _tabService.SetActiveTab(existingTab.Id);
                    }
                    else
                    {
                        // Detect content type from file extension
                        var contentType = _contentTypeDetector.DetectContentType(fileContent, file.FileType);
                        var newTab = _tabService.AddTab(file.Path, fileContent, contentType);
                        _tabService.SetActiveTab(newTab.Id);
                    }
                    SyncTabBarFromService();

                    // Set editor state from the active tab
                    CodeEditor.Text = fileContent;
                    _currentFilePath = file.Path;
                    await UpdatePreview();
                    UpdateWindowTitle();
                    _fileOperationsService.AddRecentFile(file.Path);
                    PopulateRecentFilesMenu();
                    
                    if (file.FileType.ToLower() == ".md" || 
                        file.FileType.ToLower() == ".markdown" || 
                        file.FileType.ToLower() == ".mmd")
                    {
                        await LoadMarkdownFileForExport(file.Path);
                    }
                }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            
            if (_isBuilderVisible && DiagramCanvasControl != null)
            {
                savePicker.FileTypeChoices.Add("Diagram Builder File", new List<string>() { ".mmdx" });
                savePicker.FileTypeChoices.Add("Mermaid Diagram", new List<string>() { ".mmd" });
                savePicker.SuggestedFileName = "NewDiagram";
            }
            else if (_currentContentType == ContentType.Markdown || _currentContentType == ContentType.MarkdownWithMermaid)
            {
                savePicker.FileTypeChoices.Add("Markdown Document", new List<string>() { ".md" });
                savePicker.FileTypeChoices.Add("Markdown", new List<string>() { ".markdown" });
                savePicker.FileTypeChoices.Add("Mermaid Diagram", new List<string>() { ".mmd" });
                savePicker.SuggestedFileName = "NewDocument";
            }
            else
            {
                savePicker.FileTypeChoices.Add("Mermaid Diagram", new List<string>() { ".mmd" });
                savePicker.FileTypeChoices.Add("Markdown Document", new List<string>() { ".md" });
                savePicker.SuggestedFileName = "NewDiagram";
            }

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Temporarily disable file watcher to avoid triggering reload on our own save
                var wasWatching = _fileWatcher?.EnableRaisingEvents ?? false;
                if (_fileWatcher != null)
                {
                    _fileWatcher.EnableRaisingEvents = false;
                }

                try
                {
                    if (file.FileType.ToLower() == ".mmdx" && DiagramCanvasControl != null)
                    {
                        var success = await _diagramFileService.SaveDiagramAsync(file.Path, DiagramCanvasControl.ViewModel);
                        if (success)
                        {
                            _currentFilePath = file.Path;
                            DiagramCanvasControl.ViewModel.MarkAsSaved();
                            _logger.LogInformation($"Diagram Builder file saved: {file.Path}");
                            UpdateWindowTitle();
                            SetupFileWatcher(file.Path);
                        }
                        else
                        {
                            _logger.LogError($"Failed to save Diagram Builder file: {file.Path}");
                            var errorDialog = new ContentDialog
                            {
                                Title = "Save Error",
                                Content = "Failed to save the diagram file. Please try again.",
                                CloseButtonText = "OK",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        await FileIO.WriteTextAsync(file, CodeEditor.Text);
                        _currentFilePath = file.Path;
                        _logger.LogInformation($"File saved: {file.Path}");
                        UpdateWindowTitle();
                        SetupFileWatcher(file.Path);

                        // Clear dirty flag on the active tab after successful save
                        var activeTab = _tabService.ActiveTab;
                        if (activeTab != null)
                        {
                            _tabService.MarkDirty(activeTab.Id, false);
                            UpdateTabDirtyIndicator(activeTab.Id);
                        }
                    }
                }
                finally
                {
                    // Re-enable file watcher after a short delay to avoid detecting our own write
                    if (wasWatching && _fileWatcher != null)
                    {
                        await Task.Delay(500);
                        if (_fileWatcher != null)
                        {
                            _fileWatcher.EnableRaisingEvents = true;
                        }
                    }
                }
            }
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            // Check for unsaved changes in builder before closing
            if (_isBuilderVisible && DiagramCanvasControl != null && DiagramCanvasControl.ViewModel.HasUnsavedChanges)
            {
                var result = await ShowUnsavedChangesDialog();
                if (result == ContentDialogResult.None) // Cancel
                {
                    return;
                }
                else if (result == ContentDialogResult.Primary) // Save
                {
                    Save_Click(this, new RoutedEventArgs());
                    await Task.Delay(100);
                }
                // Secondary = Discard, continue with close
            }

            // If there's an active tab, delegate to the tab close flow
            var activeTab = _tabService.ActiveTab;
            if (activeTab != null)
            {
                if (activeTab.IsDirty)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Unsaved Changes",
                        Content = $"'{activeTab.FileName}' has unsaved changes. Do you want to save before closing?",
                        PrimaryButtonText = "Save",
                        SecondaryButtonText = "Discard",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.None) // Cancel
                    {
                        return;
                    }
                    if (result == ContentDialogResult.Primary) // Save
                    {
                        Save_Click(this, new RoutedEventArgs());
                        await Task.Delay(200);
                    }
                    // Discard falls through to removal
                }

                _tabService.RemoveTab(activeTab.Id);
                SyncTabBarFromService();

                // If no tabs remain, create a fresh Untitled tab
                if (_tabService.Tabs.Count == 0)
                {
                    StopFileWatcher();

                    var untitledTab = _tabService.AddTab(string.Empty, string.Empty, ContentType.Unknown);
                    _tabService.SetActiveTab(untitledTab.Id);
                    SyncTabBarFromService();

                    _currentFilePath = string.Empty;
                    CodeEditor.Text = string.Empty;
                    _currentContentType = ContentType.Unknown;
                    _lastPreviewedCode = null;

                    // Clear the diagram builder canvas if visible
                    if (_isBuilderVisible && DiagramCanvasControl != null)
                    {
                        DiagramCanvasControl.ClearVisuals();
                        DiagramCanvasControl.ViewModel.ClearCanvas();
                        DiagramCanvasControl.ViewModel.MarkAsSaved();
                    }

                    // Hide the builder panels if visible
                    if (_isBuilderVisible)
                    {
                        _isBuilderVisible = false;
                        if (BuilderTool != null)
                        {
                            BuilderTool.IsChecked = false;
                        }
                        UpdateBuilderVisibility();
                    }

                    // Clear export state
                    if (_markdownToWordViewModel != null)
                    {
                        _markdownToWordViewModel.MarkdownFilePath = null;
                    }

                    await UpdatePreview();
                    // Explicitly clear the WebView2 since UpdatePreview can't render empty content
                    if (_isWebViewReady && PreviewBrowser?.CoreWebView2 != null)
                    {
                        try
                        {
                            await PreviewBrowser.ExecuteScriptAsync(
                                "document.getElementById('content-container').innerHTML = '';");
                        }
                        catch { }
                    }
                    UpdateWindowTitle();
                    _logger.LogInformation("Last tab closed, document cleared");
                }
            }
            else
            {
                // No active tab — fall back to legacy close behavior
                StopFileWatcher();
                _currentFilePath = string.Empty;
                CodeEditor.Text = string.Empty;

                if (_isBuilderVisible && DiagramCanvasControl != null)
                {
                    DiagramCanvasControl.ClearVisuals();
                    DiagramCanvasControl.ViewModel.ClearCanvas();
                    DiagramCanvasControl.ViewModel.MarkAsSaved();
                }

                if (_isBuilderVisible)
                {
                    _isBuilderVisible = false;
                    if (BuilderTool != null)
                    {
                        BuilderTool.IsChecked = false;
                    }
                    UpdateBuilderVisibility();
                }

                if (_markdownToWordViewModel != null)
                {
                    _markdownToWordViewModel.MarkdownFilePath = null;
                }

                await UpdatePreview();
                // Explicitly clear the WebView2 since UpdatePreview can't render empty content
                if (_isWebViewReady && PreviewBrowser?.CoreWebView2 != null)
                {
                    try
                    {
                        await PreviewBrowser.ExecuteScriptAsync(
                            "document.getElementById('content-container').innerHTML = '';");
                    }
                    catch { }
                }
                UpdateWindowTitle();
                _logger.LogInformation("Document closed");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        #endregion

        #region Recent Files

        private void PopulateRecentFilesMenu()
        {
            try
            {
                RecentFilesMenu.Items.Clear();

                var recentFiles = _fileOperationsService.GetRecentFiles();

                if (recentFiles.Count == 0)
                {
                    var emptyItem = new MenuFlyoutItem
                    {
                        Text = "(No recent files)",
                        IsEnabled = false
                    };
                    RecentFilesMenu.Items.Add(emptyItem);
                }
                else
                {
                    foreach (var recentFile in recentFiles.Take(20))
                    {
                        var menuItem = new MenuFlyoutItem
                        {
                            Text = $"{recentFile.FileName}",
                            Tag = recentFile.FilePath
                        };
                        ToolTipService.SetToolTip(menuItem, recentFile.FilePath);
                        menuItem.Click += RecentFile_Click;
                        RecentFilesMenu.Items.Add(menuItem);
                    }

                    RecentFilesMenu.Items.Add(new MenuFlyoutSeparator());
                    
                    var clearItem = new MenuFlyoutItem
                    {
                        Text = "Clear Recent Files"
                    };
                    clearItem.Click += ClearRecentFiles_Click;
                    RecentFilesMenu.Items.Add(clearItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error populating recent files menu: {ex.Message}", ex);
            }
        }

        private async void RecentFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuFlyoutItem menuItem && menuItem.Tag is string filePath)
                {
                    await OpenRecentFile(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error opening recent file: {ex.Message}", ex);
            }
        }

        private async Task OpenRecentFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    var dialog = new ContentDialog
                    {
                        Title = "File Not Found",
                        Content = $"The file '{Path.GetFileName(filePath)}' no longer exists.\n\nWould you like to remove it from recent files?",
                        PrimaryButtonText = "Remove",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        _fileOperationsService.RemoveRecentFile(filePath);
                        PopulateRecentFilesMenu();
                    }
                    return;
                }

                // Each tab tracks its own dirty state, so no global unsaved-changes dialog needed

                var file = await StorageFile.GetFileFromPathAsync(filePath);
                _currentFilePath = file.Path;
                SetupFileWatcher(file.Path);

                if (file.FileType.ToLower() == ".mmdx")
                {
                    var diagramFile = await _diagramFileService.LoadDiagramAsync(file.Path);
                    if (diagramFile != null)
                    {
                        if (!_isBuilderVisible)
                        {
                            _isBuilderVisible = true;
                            BuilderTool.IsChecked = true;
                            UpdateBuilderVisibility();
                        }

                        if (DiagramCanvasControl != null)
                        {
                            DiagramCanvasControl.ShowLoading();
                            DiagramCanvasControl.ClearVisuals();
                            DiagramCanvasControl.ViewModel.ClearCanvas();
                            await Task.Delay(50);
                            _diagramFileService.RestoreDiagram(diagramFile, DiagramCanvasControl.ViewModel);
                            DiagramCanvasControl.ViewModel.MarkAsSaved();
                            await Task.Delay(150);
                            DiagramCanvasControl.HideLoading();
                            DiagramCanvasControl.Focus(FocusState.Programmatic);
                            CodeEditor.Text = diagramFile.MermaidCode;
                            await UpdatePreview();
                        }

                        _logger.LogInformation($"Diagram Builder file loaded from recent: {file.Path}");
                    }
                }
                else
                {
                    var fileContent = await FileIO.ReadTextAsync(file);

                    if (file.FileType.ToLower() == ".mmd")
                    {
                        if (_fileOperationsService.NeedsMermaidOptimization(fileContent))
                        {
                            fileContent = _fileOperationsService.OptimizeMermaidContent(fileContent);
                            _logger.LogInformation("Diagram text was automatically optimized");
                        }
                    }

                    // Tab dedup: if file is already open, switch to existing tab
                    var existingTab = _tabService.FindTabByFilePath(file.Path);
                    if (existingTab != null)
                    {
                        _tabService.SetActiveTab(existingTab.Id);
                    }
                    else
                    {
                        var contentType = _contentTypeDetector.DetectContentType(fileContent, file.FileType);
                        var newTab = _tabService.AddTab(file.Path, fileContent, contentType);
                        _tabService.SetActiveTab(newTab.Id);
                    }
                    SyncTabBarFromService();

                    CodeEditor.Text = fileContent;
                    _currentFilePath = file.Path;
                    await UpdatePreview();

                    if (file.FileType.ToLower() == ".md" ||
                        file.FileType.ToLower() == ".markdown" ||
                        file.FileType.ToLower() == ".mmd")
                    {
                        await LoadMarkdownFileForExport(file.Path);
                    }
                }

                UpdateWindowTitle();
                _fileOperationsService.AddRecentFile(filePath);
                PopulateRecentFilesMenu();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error opening recent file '{filePath}': {ex.Message}", ex);
                var errorDialog = new ContentDialog
                {
                    Title = "Open Error",
                    Content = $"Failed to open the file:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private void ClearRecentFiles_Click(object sender, RoutedEventArgs e)
        {
            _fileOperationsService.ClearRecentFiles();
            PopulateRecentFilesMenu();
        }

        #endregion

        #region Dialogs and Utility

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };

            if (this.Content is FrameworkElement rootElement)
            {
                dialog.XamlRoot = rootElement.XamlRoot;
            }

            await dialog.ShowAsync();
        }

        private async Task<ContentDialogResult> ShowUnsavedChangesDialog()
        {
            var dialog = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = "You have unsaved changes in the Diagram Builder. Do you want to save them?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Discard",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            if (this.Content is FrameworkElement rootElement)
            {
                dialog.XamlRoot = rootElement.XamlRoot;
            }

            return await dialog.ShowAsync();
        }

        private void UpdateWindowTitle()
        {
            this.Title = _fileOperationsService.GetWindowTitle(_currentFilePath);
        }

        private async Task LoadMarkdownFileForExport(string filePath)
        {
            try
            {
                if (_markdownToWordViewModel != null)
                {
                    _markdownToWordViewModel.MarkdownFilePath = null;
                    
                    var content = CodeEditor.Text;
                    
                    if (Path.GetExtension(filePath).ToLower() == ".mmd")
                    {
                        content = $"# Mermaid Diagram\n\n```mermaid\n{content}\n```";
                    }
                    
                    await _markdownToWordViewModel.LoadMarkdownFileAsync(filePath, content);
                    _logger.Log(LogLevel.Information, $"File loaded for export: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, $"Failed to load file for export: {ex.Message}", ex);
            }
        }

        private async Task<bool> FileExistsAsync(StorageFolder folder, string fileName)
        {
            try
            {
                var item = await folder.TryGetItemAsync(fileName);
                return item != null && item.IsOfType(StorageItemTypes.File);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region File System Watcher

        private void SetupFileWatcher(string filePath)
        {
            // Dispose existing watcher if any
            _fileWatcher?.Dispose();
            _fileWatcher = null;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                    return;

                _fileWatcher = new FileSystemWatcher(directory)
                {
                    Filter = fileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Changed += OnFileChanged;
                _logger.LogInformation($"File watcher enabled for: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to setup file watcher: {ex.Message}");
            }
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce: Ignore rapid successive changes (some editors trigger multiple events)
            var now = DateTime.Now;
            if ((now - _lastFileChangeTime).TotalMilliseconds < 500)
                return;

            _lastFileChangeTime = now;

            // Wait a bit to ensure file is fully written
            await Task.Delay(100);

            // Dispatch to UI thread
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await ReloadCurrentFile();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reloading file: {ex.Message}");
                }
            });
        }

        private async Task ReloadCurrentFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
                return;

            _logger.LogInformation($"Reloading file: {_currentFilePath}");

            var extension = Path.GetExtension(_currentFilePath).ToLower();

            try
            {
                if (extension == ".mmdx")
                {
                    var diagramFile = await _diagramFileService.LoadDiagramAsync(_currentFilePath);
                    if (diagramFile != null && DiagramCanvasControl != null)
                    {
                        DiagramCanvasControl.ShowLoading();
                        DiagramCanvasControl.ClearVisuals();
                        DiagramCanvasControl.ViewModel.ClearCanvas();
                        await Task.Delay(50);
                        _diagramFileService.RestoreDiagram(diagramFile, DiagramCanvasControl.ViewModel);
                        DiagramCanvasControl.ViewModel.MarkAsSaved();
                        await Task.Delay(150);
                        DiagramCanvasControl.HideLoading();
                        DiagramCanvasControl.Focus(FocusState.Programmatic);
                        CodeEditor.Text = diagramFile.MermaidCode;
                        await UpdatePreview();
                        _logger.LogInformation("Diagram Builder file reloaded successfully");
                    }
                }
                else if (extension == ".mmd" || extension == ".md" || extension == ".markdown")
                {
                    var content = await File.ReadAllTextAsync(_currentFilePath);
                    CodeEditor.Text = content;
                    await UpdatePreview();
                    _logger.LogInformation("Text file reloaded successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to reload file: {ex.Message}");
            }
        }

        private void StopFileWatcher()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Changed -= OnFileChanged;
                _fileWatcher.Dispose();
                _fileWatcher = null;
                _logger.LogInformation("File watcher stopped");
            }
        }

        #endregion
    }
}
