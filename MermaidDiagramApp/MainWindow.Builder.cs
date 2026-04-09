// MainWindow.Builder.cs
// Partial class for MainWindow containing visual builder panel visibility management,
// canvas wiring, toolbox/properties panel setup, and builder-related event handlers.

using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        #region Builder Panel

        /// <summary>
        /// Wires up the visual builder components (canvas, properties panel, toolbox).
        /// Called from MainWindow_Loaded after WebView initialization.
        /// </summary>
        private void InitializeBuilderWiring()
        {
            if (DiagramCanvasControl != null && PropertiesPanelControl != null && ShapeToolboxControl != null)
            {
                // Connect canvas ViewModel to properties panel
                DiagramCanvasControl.ViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(DiagramCanvasControl.ViewModel.SelectedNode))
                    {
                        PropertiesPanelControl.ViewModel.SelectedElement = DiagramCanvasControl.ViewModel.SelectedNode;
                    }
                    else if (args.PropertyName == nameof(DiagramCanvasControl.ViewModel.SelectedConnector))
                    {
                        PropertiesPanelControl.ViewModel.SelectedElement = DiagramCanvasControl.ViewModel.SelectedConnector;
                    }
                };

                // Connect canvas ViewModel to toolbox for code view
                ShapeToolboxControl.WireUpCanvasViewModel(DiagramCanvasControl.ViewModel);

                // Handle apply code to editor
                ShapeToolboxControl.ApplyCodeRequested += (s, code) =>
                {
                    CodeEditor.Text = code;
                };
            }
        }

        private void BuilderTool_Click(object sender, RoutedEventArgs e)
        {
            _isBuilderVisible = BuilderTool.IsChecked;
            UpdateBuilderVisibility();
        }

        private void UpdateBuilderVisibility()
        {
            _logger.Log(LogLevel.Information, $"[Builder] UpdateBuilderVisibility called, _isBuilderVisible={_isBuilderVisible}");

            if (_isBuilderVisible)
            {
                // Collapse unused columns
                ToolboxColumn.Width = new GridLength(0);
                ToolboxSplitter.Visibility = Visibility.Collapsed;

                // Move ToolboxPanel into BuilderColumn (col 2)
                Grid.SetColumn(ToolboxPanel, 2);
                BuilderColumn.Width = new GridLength(1, GridUnitType.Star);
                ToolboxPanel.Visibility = Visibility.Visible;

                // CanvasSplitter (col 3) — directly between col 2 and col 4
                CanvasSplitter.Visibility = Visibility.Visible;

                // Move CanvasPanel into EditorColumn (col 4)
                Grid.SetColumn(CanvasPanel, 4);
                EditorColumn.Width = new GridLength(3, GridUnitType.Star);
                CanvasPanel.Visibility = Visibility.Visible;
                CodeEditor.Visibility = Visibility.Collapsed;

                // EditorPreviewSplitter (col 5) — the proven working splitter
                EditorPreviewSplitter.Visibility = Visibility.Visible;

                // Move PropertiesPanel into PreviewColumn (col 6)
                Grid.SetColumn(PropertiesPanel, 6);
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                PropertiesPanel.Visibility = Visibility.Visible;

                // Collapse original properties column
                PropertiesColumn.Width = new GridLength(0);
                PropertiesSplitter.Visibility = Visibility.Collapsed;

                // Hide old builder
                BuilderPanel.Visibility = Visibility.Collapsed;

                // Log final state after layout
                DispatcherQueue.TryEnqueue(() =>
                {
                    LogBuilderColumnState("AFTER SHOW");
                });
            }
            else
            {
                // Restore panels to their original columns
                Grid.SetColumn(ToolboxPanel, 0);
                Grid.SetColumn(CanvasPanel, 2);
                Grid.SetColumn(PropertiesPanel, 8);

                // Hide all builder panels
                ToolboxColumn.Width = new GridLength(0);
                ToolboxPanel.Visibility = Visibility.Collapsed;
                ToolboxSplitter.Visibility = Visibility.Collapsed;

                BuilderColumn.Width = new GridLength(0);
                CanvasPanel.Visibility = Visibility.Collapsed;
                CanvasSplitter.Visibility = Visibility.Collapsed;
                BuilderPanel.Visibility = Visibility.Collapsed;

                PropertiesColumn.Width = new GridLength(0);
                PropertiesPanel.Visibility = Visibility.Collapsed;
                PropertiesSplitter.Visibility = Visibility.Collapsed;

                // Show code editor and preview
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                CodeEditor.Visibility = Visibility.Visible;
                EditorPreviewSplitter.Visibility = Visibility.Visible;
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);

                DispatcherQueue.TryEnqueue(() =>
                {
                    LogBuilderColumnState("AFTER HIDE");
                });
            }
        }

        private void LogBuilderColumnState(string context)
        {
            _logger.Log(LogLevel.Information, $"[Builder] === Column State: {context} ===");
            _logger.Log(LogLevel.Information, $"[Builder] Col0 ToolboxColumn:    Width={ToolboxColumn.Width}, ActualWidth={ToolboxColumn.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col1 ToolboxSplitter:  Vis={ToolboxSplitter.Visibility}, ActualW={ToolboxSplitter.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col2 BuilderColumn:    Width={BuilderColumn.Width}, ActualWidth={BuilderColumn.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col3 CanvasSplitter:   Vis={CanvasSplitter.Visibility}, ActualW={CanvasSplitter.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col4 EditorColumn:     Width={EditorColumn.Width}, ActualWidth={EditorColumn.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col5 EditorPrevSplit:  Vis={EditorPreviewSplitter.Visibility}, ActualW={EditorPreviewSplitter.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col6 PreviewColumn:    Width={PreviewColumn.Width}, ActualWidth={PreviewColumn.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col7 PropSplitter:     Vis={PropertiesSplitter.Visibility}, ActualW={PropertiesSplitter.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] Col8 PropertiesColumn: Width={PropertiesColumn.Width}, ActualWidth={PropertiesColumn.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] ToolboxPanel:    Col={Grid.GetColumn(ToolboxPanel)}, Vis={ToolboxPanel.Visibility}, ActualW={ToolboxPanel.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] CanvasPanel:     Col={Grid.GetColumn(CanvasPanel)}, Vis={CanvasPanel.Visibility}, ActualW={CanvasPanel.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] PropertiesPanel: Col={Grid.GetColumn(PropertiesPanel)}, Vis={PropertiesPanel.Visibility}, ActualW={PropertiesPanel.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] CodeEditor:      Vis={CodeEditor.Visibility}, ActualW={CodeEditor.ActualWidth}");
            _logger.Log(LogLevel.Information, $"[Builder] CanvasSplitter ResizeBehavior={CanvasSplitter.ResizeBehavior}, ResizeDirection={CanvasSplitter.ResizeDirection}");
            _logger.Log(LogLevel.Information, $"[Builder] EditorPreviewSplitter ResizeBehavior={EditorPreviewSplitter.ResizeBehavior}, ResizeDirection={EditorPreviewSplitter.ResizeDirection}");
            _logger.Log(LogLevel.Information, $"[Builder] === End Column State ===");
        }

        private void DiagramBuilderViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Commented out to disable buggy visual builder logic that overwrites user's code.
            /*
            if (e.PropertyName == nameof(DiagramBuilderViewModel.GeneratedMermaidCode))
            {
                if (CodeEditor.Text != BuilderViewModel.GeneratedMermaidCode)
                {
                    CodeEditor.Text = BuilderViewModel.GeneratedMermaidCode;
                }
            }
            */
        }

        #endregion
    }
}
