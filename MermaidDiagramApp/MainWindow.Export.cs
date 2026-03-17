// MainWindow.Export.cs
// Partial class for MainWindow containing SVG/PNG export handlers
// and Mermaid.js update checking/installation logic.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;
using Windows.Storage.Pickers;
using Svg.Skia;
using SkiaSharp;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        #region Export Handlers

        private async void ExportSvg_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewBrowser?.CoreWebView2 == null) return;

            var svg = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("getSvg()");
            var unescapedSvg = System.Text.Json.JsonSerializer.Deserialize<string>(svg);

            if (string.IsNullOrEmpty(unescapedSvg)) return;

            var modifiedSvg = AddBackgroundToSvg(unescapedSvg);

            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("SVG Image", new List<string>() { ".svg" });
            savePicker.SuggestedFileName = "Diagram";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, modifiedSvg);
            }
        }

        private async void ExportPng_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewBrowser?.CoreWebView2 == null) return;

            PngExportDialog.XamlRoot = this.Content.XamlRoot;
            var result = await PngExportDialog.ShowAsync();

            if (result != ContentDialogResult.Primary) return;

            var scale = (float)ScaleNumberBox.Value;

            var savePicker = new FileSavePicker();
            WinRT_InterOp.InitializeWithWindow(savePicker, this);

            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("PNG Image", new List<string>() { ".png" });
            savePicker.SuggestedFileName = "Diagram";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    using var stream = new MemoryStream();
                    await PreviewBrowser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream.AsRandomAccessStream());
                    stream.Position = 0;

                    if (Math.Abs(scale - 1.0f) > 0.01f)
                    {
                        using var originalBitmap = SKBitmap.Decode(stream);
                        var newWidth = (int)(originalBitmap.Width * scale);
                        var newHeight = (int)(originalBitmap.Height * scale);

                        using var scaledBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                        using var fileStream = await file.OpenStreamForWriteAsync();
                        using var data = scaledBitmap.Encode(SKEncodedImageFormat.Png, 100);
                        data.SaveTo(fileStream);
                    }
                    else
                    {
                        using var fileStream = await file.OpenStreamForWriteAsync();
                        await stream.CopyToAsync(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to capture WebView2 screenshot: {ex.Message}", ex);
                    await ExportPngFallback(file, scale);
                }
            }
        }

        private async Task ExportPngFallback(StorageFile file, float scale)
        {
            try
            {
                var svgJson = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync("getSvg()");
                var svgString = System.Text.Json.JsonSerializer.Deserialize<string>(svgJson);

                if (string.IsNullOrEmpty(svgString)) return;

                using var svg = new SKSvg();
                using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgString));
                if (svg.Load(svgStream) is { } picture)
                {
                    var dimensions = new SKSizeI((int)(picture.CullRect.Width * scale), (int)(picture.CullRect.Height * scale));
                    using var bitmap = new SKBitmap(new SKImageInfo(dimensions.Width, dimensions.Height));
                    using var canvas = new SKCanvas(bitmap);
                    canvas.Clear(SKColor.Parse("#222222"));
                    var matrix = SKMatrix.CreateScale(scale, scale);
                    canvas.DrawPicture(picture, ref matrix);
                    canvas.Flush();

                    using var fileStream = await file.OpenStreamForWriteAsync();
                    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                    data.SaveTo(fileStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fallback PNG export also failed: {ex.Message}", ex);
            }
        }

        private string AddBackgroundToSvg(string svgContent)
        {
            return _exportService.AddBackgroundToSvg(svgContent);
        }

        #endregion

        #region Mermaid Update

        private async Task CheckForMermaidUpdatesAsync()
        {
            try
            {
                var versionInfo = await _mermaidUpdateService.CheckForUpdatesAsync();
                _logger.LogInformation($"Mermaid update check: Current={versionInfo.CurrentVersion}, Latest={versionInfo.LatestVersion}, UpdateAvailable={versionInfo.UpdateAvailable}");

                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    try
                    {
                        if (UpdateInfoBar == null)
                        {
                            _logger.LogError("Error: UpdateInfoBar is null");
                            return;
                        }

                        if (UpdateInfoBar.ActionButton is Button button)
                        {
                            if (versionInfo.UpdateAvailable)
                            {
                                _logger.LogInformation("Showing update notification in UI");
                                button.Visibility = Visibility.Visible;
                                button.IsEnabled = true;
                                UpdateInfoBar.Message = $"A new version of Mermaid.js ({versionInfo.LatestVersion}) is available. You are using version {versionInfo.CurrentVersion}.";
                                UpdateInfoBar.Severity = InfoBarSeverity.Informational;
                                UpdateInfoBar.IsOpen = true;
                            }
                            else
                            {
                                _logger.LogInformation("No update available, hiding notification");
                                button.Visibility = Visibility.Collapsed;
                                UpdateInfoBar.IsOpen = false;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Warning: Update button not found in UpdateInfoBar");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error updating UI: {ex.Message}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to check for Mermaid.js updates: {ex.Message}", ex);
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    if (UpdateInfoBar.ActionButton is Button button)
                    {
                        button.Visibility = Visibility.Collapsed;
                        UpdateInfoBar.IsOpen = false;
                    }
                });
            }
        }

        private async void UpdateMermaid_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateInfoBar.ActionButton is Button updateButton)
            {
                updateButton.IsEnabled = false;
            }
            UpdateInfoBar.Message = "Updating Mermaid.js...";
            UpdateInfoBar.Severity = InfoBarSeverity.Informational;

            try
            {
                var versionInfo = await _mermaidUpdateService.CheckForUpdatesAsync();
                
                if (string.IsNullOrEmpty(versionInfo.LatestVersion))
                {
                    UpdateInfoBar.Message = "Could not determine the latest version.";
                    UpdateInfoBar.Severity = InfoBarSeverity.Error;
                    if (UpdateInfoBar.ActionButton is Button btn)
                    {
                        btn.IsEnabled = true;
                    }
                    return;
                }

                var success = await _mermaidUpdateService.DownloadAndInstallUpdateAsync(versionInfo.LatestVersion);
                
                if (!success)
                {
                    UpdateInfoBar.Message = "Failed to download and install the update.";
                    UpdateInfoBar.Severity = InfoBarSeverity.Error;
                    if (UpdateInfoBar.ActionButton is Button btn)
                    {
                        btn.IsEnabled = true;
                    }
                    return;
                }

                if (PreviewBrowser?.CoreWebView2 != null)
                {
                    try 
                    {
                        await PreviewBrowser.CoreWebView2.Profile.ClearBrowsingDataAsync();
                        _logger.LogInformation("Successfully cleared WebView2 cache");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to clear WebView2 cache: {ex.Message}");
                    }
                }

                UpdateInfoBar.Message = $"Mermaid.js has been successfully updated to version {versionInfo.LatestVersion}. Restarting application...";
                UpdateInfoBar.Severity = InfoBarSeverity.Success;
                UpdateInfoBar.IsOpen = true;
                
                await Task.Delay(1000);
                
                _logger.LogInformation("Restarting application to apply Mermaid.js update...");
                AppInstance.Restart("");
                Application.Current.Exit();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update Mermaid.js: {ex.Message}", ex);
                UpdateInfoBar.Message = $"Update failed: {ex.Message}";
                UpdateInfoBar.Severity = InfoBarSeverity.Error;
                if (UpdateInfoBar.ActionButton is Button button)
                {
                    button.IsEnabled = true;
                }
            }
        }

        #endregion

        #region About and Log Handlers

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var package = Windows.ApplicationModel.Package.Current;
            var version = package.Id.Version;
            var versionString = $"App Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var installDate = package.InstalledDate;
            var installDateString = $"Installed: {installDate.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

            string mermaidVersionString = "Mermaid.js Version: Checking...";
            string versionContent = string.Empty;

            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var mermaidFolder = await localFolder.CreateFolderAsync("Mermaid", CreationCollisionOption.OpenIfExists);
                var versionFile = await mermaidFolder.CreateFileAsync("mermaid-version.txt", CreationCollisionOption.OpenIfExists);
                
                try
                {
                    versionContent = await FileIO.ReadTextAsync(versionFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading version file: {ex.Message}", ex);
                }

                if (string.IsNullOrWhiteSpace(versionContent))
                {
                    versionContent = "10.9.0";
                    try
                    {
                        await FileIO.WriteTextAsync(versionFile, versionContent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error writing default version: {ex.Message}", ex);
                    }
                }

                mermaidVersionString = $"Mermaid.js Version: {versionContent.Trim()}";
                _logger.LogInformation($"Mermaid version: {versionContent.Trim()}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error determining Mermaid.js version: {ex.Message}", ex);
                mermaidVersionString = "Mermaid.js Version: Error checking version";
            }

            string markdownVersionString = "markdown-it Version: Checking...";
            try
            {
                if (_isWebViewReady && PreviewBrowser?.CoreWebView2 != null)
                {
                    var markdownVersionJson = await PreviewBrowser.CoreWebView2.ExecuteScriptAsync(
                        "window.md && window.md.constructor && window.md.constructor.version ? window.md.constructor.version : 'Unknown'");
                    var markdownVersion = System.Text.Json.JsonSerializer.Deserialize<string>(markdownVersionJson);
                    
                    if (!string.IsNullOrEmpty(markdownVersion) && markdownVersion != "Unknown")
                    {
                        markdownVersionString = $"markdown-it Version: {markdownVersion}";
                    }
                    else
                    {
                        markdownVersionString = "markdown-it Version: 13.0.1";
                    }
                }
                else
                {
                    markdownVersionString = "markdown-it Version: 13.0.1";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error determining markdown-it version: {ex.Message}", ex);
                markdownVersionString = "markdown-it Version: 13.0.1";
            }

            VersionTextBlock.Text = versionString;
            InstallDateTextBlock.Text = installDateString;
            MermaidVersionTextBlock.Text = mermaidVersionString;
            MarkdownVersionTextBlock.Text = markdownVersionString;

            AboutDialog.XamlRoot = this.Content.XamlRoot;
            await AboutDialog.ShowAsync();
        }

        private async void OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            var provider = LoggingService.Instance.LogFileProvider;
            if (provider == null)
            {
                _logger.LogWarning("Log file provider is not available.");
                await ShowMessageAsync("Logs unavailable", "Logging has not been initialised yet. Please try again after restarting the application.");
                return;
            }

            try
            {
                Directory.CreateDirectory(provider.LogsDirectory);

                if (!File.Exists(provider.CurrentLogFilePath))
                {
                    using (File.Create(provider.CurrentLogFilePath)) { }
                }

                var storageFile = await StorageFile.GetFileFromPathAsync(provider.CurrentLogFilePath);
                var launched = await Windows.System.Launcher.LaunchFileAsync(storageFile);
                if (!launched)
                {
                    _logger.LogWarning("Windows failed to open the log file automatically.");
                    await ShowMessageAsync("Open Log File", "Windows could not open the log file automatically. It is located in the Logs folder inside the app's LocalState.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open log file: {ex.Message}", ex);
                await ShowMessageAsync("Open Log File Failed", $"Could not open the log file.\n\n{ex.Message}");
            }
        }

        private async void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            var provider = LoggingService.Instance.LogFileProvider;
            if (provider == null)
            {
                _logger.LogWarning("Log file provider is not available.");
                await ShowMessageAsync("Logs unavailable", "Logging has not been initialised yet. Please try again after restarting the application.");
                return;
            }

            try
            {
                Directory.CreateDirectory(provider.LogsDirectory);

                var storageFolder = await StorageFolder.GetFolderFromPathAsync(provider.LogsDirectory);
                var launched = await Windows.System.Launcher.LaunchFolderAsync(storageFolder);
                if (!launched)
                {
                    _logger.LogWarning("Windows failed to open the log folder automatically.");
                    await ShowMessageAsync("Open Log Folder", "Windows could not open the log folder automatically. Please browse to the folder manually.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open log folder: {ex.Message}", ex);
                await ShowMessageAsync("Open Log Folder Failed", $"Could not open the log folder.\n\n{ex.Message}");
            }
        }

        #endregion
    }
}
