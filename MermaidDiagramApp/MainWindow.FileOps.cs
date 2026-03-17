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
                    
                    CodeEditor.Text = fileContent;
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
                if (file.FileType.ToLower() == ".mmdx" && DiagramCanvasControl != null)
                {
                    var success = await _diagramFileService.SaveDiagramAsync(file.Path, DiagramCanvasControl.ViewModel);
                    if (success)
                    {
                        _currentFilePath = file.Path;
                        DiagramCanvasControl.ViewModel.MarkAsSaved();
                        _logger.LogInformation($"Diagram Builder file saved: {file.Path}");
                        UpdateWindowTitle();
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
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_markdownToWordViewModel != null)
            {
                _markdownToWordViewModel.MarkdownFilePath = null;
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

                if (!string.IsNullOrEmpty(CodeEditor.Text))
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Unsaved Changes",
                        Content = "You have unsaved changes. Do you want to save before opening another file?",
                        PrimaryButtonText = "Save",
                        SecondaryButtonText = "Discard",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        Save_Click(this, new RoutedEventArgs());
                        await Task.Delay(100);
                    }
                    else if (result == ContentDialogResult.None)
                    {
                        return;
                    }
                }

                var file = await StorageFile.GetFileFromPathAsync(filePath);
                _currentFilePath = file.Path;

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

                    CodeEditor.Text = fileContent;
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
    }
}
