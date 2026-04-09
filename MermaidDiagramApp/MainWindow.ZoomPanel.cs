// MainWindow.ZoomPanel.cs
// Partial class for MainWindow containing zoom panel layout management,
// service event wiring, and show/hide logic with column width save/restore.

using System;
using Microsoft.UI.Xaml;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        #region Zoom Panel

        private GridLength _savedEditorWidth;
        private GridLength _savedPreviewWidth;

        /// <summary>
        /// Subscribes to IZoomPanelService.StateChanged and wires the
        /// ZoomPanelViewModel.RequestClose callback. Called from the constructor.
        /// </summary>
        private void InitializeZoomPanel()
        {
            _zoomPanelService.StateChanged += OnZoomPanelStateChanged;

            ZoomPanelControl.ViewModel.RequestClose = () =>
            {
                HideZoomPanel();
            };
        }

        /// <summary>
        /// Saves current editor/preview column widths, expands the zoom panel column,
        /// shows the splitter and border, and loads the diagram into the ZoomPanel WebView.
        /// </summary>
        private void ShowZoomPanel(string svgContent)
        {
            // Save current column widths so we can restore them later
            _savedEditorWidth = EditorColumn.Width;
            _savedPreviewWidth = PreviewColumn.Width;

            // Pin the editor at its current pixel width so it doesn't participate
            // in the star-sizing with the preview and zoom columns.
            var editorActualWidth = EditorColumn.ActualWidth;
            if (editorActualWidth > 0)
            {
                EditorColumn.Width = new GridLength(editorActualWidth, GridUnitType.Pixel);
            }

            // Give preview and zoom panel equal star widths so the
            // ZoomSplitter can resize them (same pattern as Editor ↔ Preview).
            // ZoomSplitter is in col 7, ZoomPanelBorder is in col 8 (PropertiesColumn).
            PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
            PropertiesColumn.Width = new GridLength(1, GridUnitType.Star);
            PropertiesColumn.MinWidth = 200;

            // Show the splitter and border
            ZoomSplitter.Visibility = Visibility.Visible;
            ZoomPanelBorder.Visibility = Visibility.Visible;

            // Load the diagram into the zoom panel WebView
            _ = ZoomPanelControl.LoadDiagramAsync(svgContent);
        }

        /// <summary>
        /// Collapses the zoom panel column, hides the splitter and border,
        /// restores saved column widths, and navigates the WebView to about:blank.
        /// </summary>
        private void HideZoomPanel()
        {
            // Collapse the zoom panel column (PropertiesColumn, col 8)
            PropertiesColumn.Width = new GridLength(0);
            PropertiesColumn.MinWidth = 0;

            // Hide the splitter and border
            ZoomSplitter.Visibility = Visibility.Collapsed;
            ZoomPanelBorder.Visibility = Visibility.Collapsed;

            // Restore saved column widths
            EditorColumn.Width = _savedEditorWidth;
            PreviewColumn.Width = _savedPreviewWidth;

            // Navigate to about:blank to release SVG memory and reset for next use
            ZoomPanelControl.NavigateToBlank();
        }

        /// <summary>
        /// Handles IZoomPanelService.StateChanged events. Dispatches to the UI thread
        /// and calls ShowZoomPanel/HideZoomPanel based on state, or updates zoom level.
        /// </summary>
        private void OnZoomPanelStateChanged(object? sender, ZoomPanelStateChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (e.IsOpen && e.SvgContent != null)
                {
                    if (!_zoomPanelService.IsOpen)
                        return; // State may have changed between dispatch

                    // If the panel is already visible, replace the diagram content
                    if (ZoomPanelBorder.Visibility == Visibility.Visible)
                    {
                        _ = ZoomPanelControl.LoadDiagramAsync(e.SvgContent);
                        _ = ZoomPanelControl.SetZoomLevel(e.ZoomLevel);
                    }
                    else
                    {
                        ShowZoomPanel(e.SvgContent);
                        _ = ZoomPanelControl.SetZoomLevel(e.ZoomLevel);
                    }
                }
                else if (!e.IsOpen)
                {
                    if (ZoomPanelBorder.Visibility == Visibility.Visible)
                    {
                        HideZoomPanel();
                    }
                }
            });
        }

        #endregion
    }
}
