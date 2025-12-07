using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp
{
    /// <summary>
    /// Partial class for MainWindow containing Markdown to Word export functionality.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MarkdownToWordViewModel? _markdownToWordViewModel;
        private ContentDialog? _activeExportDialog;

        /// <summary>
        /// Initializes the Markdown to Word export functionality.
        /// </summary>
        private void InitializeMarkdownToWordExport()
        {
            try
            {
                // Ensure WebView2 is ready
                if (PreviewBrowser?.CoreWebView2 == null)
                {
                    _logger.Log(LogLevel.Warning, "WebView2 not ready, deferring Markdown to Word export initialization");
                    return;
                }

                // Create the export service components
                var markdownParser = new MarkdigMarkdownParser();
                var wordGenerator = new OpenXmlWordDocumentGenerator();
                
                // Create a wrapper for the WebView2 control
                var webViewWrapper = new CoreWebView2Wrapper(PreviewBrowser.CoreWebView2);
                var mermaidRenderer = new WebView2MermaidImageRenderer(webViewWrapper, _logger);
                
                var exportService = new MarkdownToWordExportService(
                    markdownParser,
                    wordGenerator,
                    mermaidRenderer,
                    _logger);

                // Create the ViewModel
                _markdownToWordViewModel = new MarkdownToWordViewModel(exportService, _logger);

                // Subscribe to property changes to update UI
                _markdownToWordViewModel.PropertyChanged += MarkdownToWordViewModel_PropertyChanged;

                _logger.Log(LogLevel.Information, "Markdown to Word export initialized");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Failed to initialize Markdown to Word export", ex);
            }
        }

        /// <summary>
        /// Handles property changes from the MarkdownToWordViewModel.
        /// </summary>
        private void MarkdownToWordViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MarkdownToWordViewModel.CanExport))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ExportToWordMenuItem != null)
                    {
                        ExportToWordMenuItem.IsEnabled = _markdownToWordViewModel?.CanExport ?? false;
                    }
                });
            }
            else if (e.PropertyName == nameof(MarkdownToWordViewModel.MarkdownFilePath))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateWindowTitleForMarkdownFile();
                });
            }
        }

        /// <summary>
        /// Updates the window title to reflect the loaded Markdown file.
        /// </summary>
        private void UpdateWindowTitleForMarkdownFile()
        {
            if (_markdownToWordViewModel != null && 
                !string.IsNullOrWhiteSpace(_markdownToWordViewModel.MarkdownFilePath))
            {
                var fileName = Path.GetFileName(_markdownToWordViewModel.MarkdownFilePath);
                this.Title = $"MermaidDiagramApp - {fileName}";
            }
            else if (string.IsNullOrWhiteSpace(_currentFilePath))
            {
                this.Title = "MermaidDiagramApp";
            }
        }

        /// <summary>
        /// Event handler for "Open Markdown File" menu item.
        /// </summary>
        private async void OpenMarkdownFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create file picker
                var picker = new FileOpenPicker();
                
                // Get the window handle for the picker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // Configure picker
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".md");
                picker.FileTypeFilter.Add(".markdown");

                // Show picker
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    _logger.Log(LogLevel.Information, $"Opening Markdown file: {file.Path}");

                    // Load the file
                    if (_markdownToWordViewModel != null)
                    {
                        try
                        {
                            await _markdownToWordViewModel.LoadMarkdownFileAsync(file.Path);
                            
                            // Update window title
                            UpdateWindowTitleForMarkdownFile();
                            
                            // Show success message
                            var dialog = new ContentDialog
                            {
                                Title = "File Loaded",
                                Content = $"Markdown file loaded successfully:\n{file.Name}",
                                CloseButtonText = "OK",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await dialog.ShowAsync();
                        }
                        catch (InvalidDataException encodingEx)
                        {
                            // Handle UTF-8 encoding errors specifically
                            _logger.Log(LogLevel.Error, "Invalid UTF-8 encoding in file", encodingEx);
                            
                            var errorDialog = new ContentDialog
                            {
                                Title = "Invalid File Encoding",
                                Content = $"The file contains invalid UTF-8 encoding.\n\n" +
                                         $"Please ensure the file is saved as UTF-8 and try again.\n\n" +
                                         $"File: {file.Name}",
                                CloseButtonText = "OK",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                        catch (UnauthorizedAccessException accessEx)
                        {
                            // Handle file access errors
                            _logger.Log(LogLevel.Error, "Access denied to file", accessEx);
                            
                            var errorDialog = new ContentDialog
                            {
                                Title = "Access Denied",
                                Content = $"Cannot access the file. Please check file permissions.\n\n" +
                                         $"File: {file.Name}",
                                CloseButtonText = "OK",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                        catch (FileNotFoundException fileEx)
                        {
                            // Handle file not found errors
                            _logger.Log(LogLevel.Error, "File not found", fileEx);
                            
                            var errorDialog = new ContentDialog
                            {
                                Title = "File Not Found",
                                Content = $"The file could not be found.\n\n" +
                                         $"File: {file.Name}",
                                CloseButtonText = "OK",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Failed to open Markdown file", ex);
                
                // Show error dialog
                var errorDialog = new ContentDialog
                {
                    Title = "Error Opening File",
                    Content = $"Failed to open Markdown file:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Event handler for "Export to Word" menu item.
        /// </summary>
        private async void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            if (_markdownToWordViewModel == null || !_markdownToWordViewModel.CanExport)
            {
                return;
            }

            try
            {
                // Create save file picker
                var picker = new FileSavePicker();
                
                // Get the window handle for the picker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // Configure picker
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("Word Document", new[] { ".docx" });
                
                // Suggest a file name based on the Markdown file
                if (!string.IsNullOrWhiteSpace(_markdownToWordViewModel.MarkdownFilePath))
                {
                    var markdownFileName = Path.GetFileNameWithoutExtension(_markdownToWordViewModel.MarkdownFilePath);
                    picker.SuggestedFileName = markdownFileName;
                }
                else
                {
                    picker.SuggestedFileName = "document";
                }

                // Show picker
                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    _logger.Log(LogLevel.Information, $"Exporting to Word: {file.Path}");

                    // Set output path
                    _markdownToWordViewModel.SetOutputPath(file.Path);

                    // Show progress dialog
                    await ShowExportProgressDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Failed to export to Word", ex);
                
                // Show error dialog
                var errorDialog = new ContentDialog
                {
                    Title = "Export Error",
                    Content = $"Failed to export to Word:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Shows the export progress dialog and performs the export.
        /// </summary>
        private async Task ShowExportProgressDialog()
        {
            if (_markdownToWordViewModel == null)
            {
                return;
            }

            try
            {
                // Reset progress UI
                ExportProgressBar.Value = 0;
                ExportProgressBar.IsIndeterminate = false;
                ExportProgressMessage.Text = "Preparing export...";
                ExportProgressPercentage.Text = "0%";

                // Store reference to dialog
                _activeExportDialog = ExportProgressDialog;

                // Set up dialog close handler for cancellation
                ExportProgressDialog.CloseButtonClick += (dialog, args) =>
                {
                    _markdownToWordViewModel?.CancelExport();
                };

                // Subscribe to progress updates
                System.ComponentModel.PropertyChangedEventHandler progressHandler = (sender, e) =>
                {
                    if (e.PropertyName == nameof(MarkdownToWordViewModel.ProgressPercentage))
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            ExportProgressBar.Value = _markdownToWordViewModel.ProgressPercentage;
                            ExportProgressPercentage.Text = $"{_markdownToWordViewModel.ProgressPercentage}%";
                        });
                    }
                    else if (e.PropertyName == nameof(MarkdownToWordViewModel.ProgressMessage))
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            ExportProgressMessage.Text = _markdownToWordViewModel.ProgressMessage;
                        });
                    }
                };

                _markdownToWordViewModel.PropertyChanged += progressHandler;

                // Show dialog (non-blocking)
                var dialogTask = ExportProgressDialog.ShowAsync();

                // Start export
                try
                {
                    await _markdownToWordViewModel.ExportToWordAsync();

                    // Hide dialog
                    ExportProgressDialog.Hide();

                    // Show success notification
                    var successDialog = new ContentDialog
                    {
                        Title = "Export Successful",
                        Content = $"Document exported successfully to:\n{_markdownToWordViewModel.OutputPath}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                catch (OperationCanceledException)
                {
                    // Export was cancelled
                    _logger.Log(LogLevel.Information, "Export cancelled by user");
                    
                    ExportProgressDialog.Hide();
                    
                    var cancelDialog = new ContentDialog
                    {
                        Title = "Export Cancelled",
                        Content = "The export operation was cancelled.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await cancelDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "Export failed", ex);
                    
                    ExportProgressDialog.Hide();
                    
                    var errorDialog = new ContentDialog
                    {
                        Title = "Export Failed",
                        Content = $"Failed to export document:\n{ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
                finally
                {
                    // Unsubscribe from progress updates
                    _markdownToWordViewModel.PropertyChanged -= progressHandler;
                    _activeExportDialog = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Failed to show export progress dialog", ex);
            }
        }
    }
}
