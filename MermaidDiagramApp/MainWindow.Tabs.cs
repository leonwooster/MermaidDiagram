// MainWindow.Tabs.cs
// Partial class for MainWindow containing tab-related UI logic:
// tab selection changes, tab close with dirty-check dialog,
// tab bar synchronization, dirty indicators, and scroll position save/restore.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        /// <summary>
        /// Guard flag to prevent re-entrant handling of SelectionChanged
        /// while we are programmatically rebuilding the tab bar.
        /// </summary>
        private bool _isSyncingTabs;

        /// <summary>
        /// Handles tab selection changes. Saves outgoing tab state and loads
        /// the incoming tab state into the CodeEditor and preview.
        /// </summary>
        private async void PreviewTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingTabs) return;

            // Determine the outgoing tab from the removed selection
            TabState? outgoingTab = null;
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewItem removedItem
                && removedItem.Tag is Guid outgoingId)
            {
                outgoingTab = _tabService.Tabs.FirstOrDefault(t => t.Id == outgoingId);
            }

            // Save outgoing tab state
            if (outgoingTab != null)
            {
                _tabService.UpdateTabContent(outgoingTab.Id, CodeEditor.Text);
                await SaveCurrentTabScrollPosition();
            }

            // Determine the incoming tab from the added selection
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabViewItem addedItem
                && addedItem.Tag is Guid incomingId)
            {
                var incomingTab = _tabService.Tabs.FirstOrDefault(t => t.Id == incomingId);
                if (incomingTab != null)
                {
                    _tabService.SetActiveTab(incomingId);

                    // Load incoming tab state into the editor
                    CodeEditor.Text = incomingTab.EditorContent;
                    _currentContentType = incomingTab.ContentType;
                    _currentFilePath = incomingTab.FilePath;

                    // Trigger re-render by resetting the last previewed code
                    _lastPreviewedCode = null;
                    await UpdatePreview();

                    // Restore scroll position after render
                    await RestoreTabScrollPosition();
                }
            }
        }

        /// <summary>
        /// Handles the tab close request. If the tab is dirty, shows a
        /// Save / Discard / Cancel dialog. Otherwise closes immediately.
        /// </summary>
        private async void PreviewTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (args.Tab?.Tag is not Guid tabId) return;

            var tab = _tabService.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab == null) return;

            if (tab.IsDirty)
            {
                var dialog = new ContentDialog
                {
                    Title = "Unsaved Changes",
                    Content = $"'{tab.FileName}' has unsaved changes. Do you want to save before closing?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Discard",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.None)
                {
                    // Cancel — keep the tab open
                    return;
                }

                if (result == ContentDialogResult.Primary)
                {
                    // Save — save the file, then close
                    // Ensure the editor has the latest content for this tab
                    if (_tabService.ActiveTab?.Id == tabId)
                    {
                        _tabService.UpdateTabContent(tabId, CodeEditor.Text);
                    }
                    Save_Click(this, new RoutedEventArgs());
                    // Allow a moment for the save picker to complete
                    await Task.Delay(200);
                }
                // Discard falls through to removal below
            }

            _tabService.RemoveTab(tabId);
            SyncTabBarFromService();

            if (_tabService.Tabs.Count == 0)
            {
                // No tabs remain — create a fresh Untitled tab
                var untitledTab = _tabService.AddTab(string.Empty, string.Empty, ContentType.Unknown);
                _tabService.SetActiveTab(untitledTab.Id);
                SyncTabBarFromService();

                CodeEditor.Text = string.Empty;
                _currentFilePath = string.Empty;
                _currentContentType = ContentType.Unknown;
                _lastPreviewedCode = null;

                if (_isWebViewReady && PreviewBrowser?.CoreWebView2 != null)
                {
                    try
                    {
                        await PreviewBrowser.ExecuteScriptAsync(
                            "document.getElementById('content-container').innerHTML = '';");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to clear preview: {ex.Message}");
                    }
                }
            }
            else
            {
                // Load the newly active tab's content into editor and preview.
                // SyncTabBarFromService sets _isSyncingTabs which blocks
                // SelectionChanged, so we must do this explicitly.
                var newActive = _tabService.ActiveTab;
                if (newActive != null)
                {
                    CodeEditor.Text = newActive.EditorContent;
                    _currentContentType = newActive.ContentType;
                    _currentFilePath = newActive.FilePath;
                    _lastPreviewedCode = null;
                    await UpdatePreview();
                    await RestoreTabScrollPosition();
                }
            }
        }

        /// <summary>
        /// Rebuilds the TabView items from the current <see cref="ITabService.Tabs"/> collection.
        /// Sets the selected item to match the active tab.
        /// </summary>
        internal void SyncTabBarFromService()
        {
            _isSyncingTabs = true;
            try
            {
                PreviewTabView.TabItems.Clear();

                foreach (var tab in _tabService.Tabs)
                {
                    var header = tab.IsDirty ? $"● {tab.FileName}" : tab.FileName;
                    var tabItem = new TabViewItem
                    {
                        Header = header,
                        Tag = tab.Id,
                        IsClosable = true
                    };
                    PreviewTabView.TabItems.Add(tabItem);
                }

                // Select the active tab
                var activeTab = _tabService.ActiveTab;
                if (activeTab != null)
                {
                    var activeItem = PreviewTabView.TabItems
                        .OfType<TabViewItem>()
                        .FirstOrDefault(item => item.Tag is Guid id && id == activeTab.Id);

                    if (activeItem != null)
                    {
                        PreviewTabView.SelectedItem = activeItem;
                    }
                }
            }
            finally
            {
                _isSyncingTabs = false;
            }
        }

        /// <summary>
        /// Updates the header of a single tab to reflect its dirty state.
        /// </summary>
        internal void UpdateTabDirtyIndicator(Guid tabId)
        {
            var tab = _tabService.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab == null) return;

            var tabItem = PreviewTabView.TabItems
                .OfType<TabViewItem>()
                .FirstOrDefault(item => item.Tag is Guid id && id == tabId);

            if (tabItem != null)
            {
                tabItem.Header = tab.IsDirty ? $"● {tab.FileName}" : tab.FileName;
            }
        }

        /// <summary>
        /// Reads the current scroll position from the WebView2 preview via JavaScript
        /// and stores it in the active tab's state.
        /// </summary>
        private async Task SaveCurrentTabScrollPosition()
        {
            var activeTab = _tabService.ActiveTab;
            if (activeTab == null || !_isWebViewReady) return;

            try
            {
                var scrollXResult = await PreviewBrowser.ExecuteScriptAsync("window.scrollX");
                var scrollYResult = await PreviewBrowser.ExecuteScriptAsync("window.scrollY");

                double.TryParse(scrollXResult, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var scrollX);
                double.TryParse(scrollYResult, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var scrollY);

                _tabService.UpdateScrollPosition(activeTab.Id, scrollY, scrollX);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to save scroll position: {ex.Message}");
                _tabService.UpdateScrollPosition(activeTab.Id, 0, 0);
            }
        }

        /// <summary>
        /// Restores the active tab's saved scroll position in the WebView2 preview.
        /// </summary>
        private async Task RestoreTabScrollPosition()
        {
            var activeTab = _tabService.ActiveTab;
            if (activeTab == null || !_isWebViewReady) return;

            try
            {
                var scrollX = activeTab.ScrollLeft;
                var scrollY = activeTab.ScrollTop;
                var script = $"window.scrollTo({scrollX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {scrollY.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
                await PreviewBrowser.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to restore scroll position: {ex.Message}");
            }
        }
    }
}
