// MainWindow.Builder.cs
// Partial class for MainWindow containing visual builder panel visibility management,
// canvas wiring, toolbox/properties panel setup, and builder-related event handlers.

using System.ComponentModel;
using Microsoft.UI.Xaml;

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
            if (_isBuilderVisible)
            {
                // Show NEW Visual Builder (three-panel layout)
                ToolboxColumn.Width = new GridLength(300, GridUnitType.Pixel);
                ToolboxPanel.Visibility = Visibility.Visible;
                ToolboxSplitter.Visibility = Visibility.Visible;

                BuilderColumn.Width = new GridLength(1, GridUnitType.Star);
                CanvasPanel.Visibility = Visibility.Visible;
                CanvasSplitter.Visibility = Visibility.Visible;

                PropertiesColumn.Width = new GridLength(300, GridUnitType.Pixel);
                PropertiesPanel.Visibility = Visibility.Visible;
                PropertiesSplitter.Visibility = Visibility.Visible;

                // Hide old builder
                BuilderPanel.Visibility = Visibility.Collapsed;
                
                // Hide code editor and preview in builder mode
                EditorColumn.Width = new GridLength(0);
                CodeEditor.Visibility = Visibility.Collapsed;
                EditorPreviewSplitter.Visibility = Visibility.Collapsed;
                PreviewColumn.Width = new GridLength(0);
            }
            else
            {
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
            }
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
