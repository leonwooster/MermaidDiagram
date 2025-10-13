using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Models.Canvas;
using System.Linq;

namespace MermaidDiagramApp.Views
{
    public sealed partial class DiagramCanvas : UserControl
    {
        public DiagramCanvasViewModel ViewModel { get; }

        private CanvasNode? _manipulatedNode;

        public DiagramCanvas()
        {
            this.InitializeComponent();
            ViewModel = new DiagramCanvasViewModel();
            
            Loaded += DiagramCanvas_Loaded;
        }

        private void DiagramCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            DrawGrid();
            
            // Subscribe to collection changes to wire up events for new nodes
            ViewModel.Nodes.CollectionChanged += Nodes_CollectionChanged;
        }

        private void Nodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // When nodes are added, create visual elements
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (CanvasNode node in e.NewItems)
                {
                    CreateNodeVisual(node);
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (CanvasNode node in e.OldItems)
                {
                    RemoveNodeVisual(node);
                }
            }
        }

        private void CreateNodeVisual(CanvasNode node)
        {
            System.Diagnostics.Debug.WriteLine($"[CREATE] Creating visual for node: {node.Id} at ({node.PositionX}, {node.PositionY})");
            
            var border = new Border
            {
                Width = node.SizeWidth,
                Height = node.SizeHeight,
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY,
                Tag = node
            };
            
            System.Diagnostics.Debug.WriteLine($"[CREATE] Border created with ManipulationMode: {border.ManipulationMode}");

            var grid = new Grid { IsHitTestVisible = false };
            
            var textBlock = new TextBlock
            {
                Text = node.Text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(8)
            };
            grid.Children.Add(textBlock);

            var selectionBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed
            };
            grid.Children.Add(selectionBorder);

            border.Child = grid;

            // Set initial position
            Canvas.SetLeft(border, node.PositionX);
            Canvas.SetTop(border, node.PositionY);

            // Wire up events
            border.ManipulationStarted += Node_ManipulationStarted;
            border.ManipulationDelta += Node_ManipulationDelta;
            border.ManipulationCompleted += Node_ManipulationCompleted;
            border.Tapped += Node_Tapped;

            // Subscribe to property changes
            node.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CanvasNode.PositionX))
                {
                    var oldLeft = Canvas.GetLeft(border);
                    Canvas.SetLeft(border, node.PositionX);
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] PositionX changed: {oldLeft} -> {node.PositionX}");
                }
                else if (e.PropertyName == nameof(CanvasNode.PositionY))
                {
                    var oldTop = Canvas.GetTop(border);
                    Canvas.SetTop(border, node.PositionY);
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] PositionY changed: {oldTop} -> {node.PositionY}");
                }
                else if (e.PropertyName == nameof(CanvasNode.IsSelected))
                {
                    selectionBorder.Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] IsSelected changed: {node.IsSelected}");
                }
                else if (e.PropertyName == nameof(CanvasNode.Text))
                {
                    textBlock.Text = node.Text;
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] Text changed: {node.Text}");
                }
            };

            NodesCanvas.Children.Add(border);
            System.Diagnostics.Debug.WriteLine($"[CREATE] Node visual added to canvas. Total nodes: {NodesCanvas.Children.Count}");
        }

        private void RemoveNodeVisual(CanvasNode node)
        {
            var borderToRemove = NodesCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == node);
            if (borderToRemove != null)
            {
                NodesCanvas.Children.Remove(borderToRemove);
            }
        }

        private void DrawGrid()
        {
            if (!ViewModel.ShowGrid) return;

            GridCanvas.Children.Clear();

            var gridSize = ViewModel.GridSize;
            var width = CanvasContainer.Width;
            var height = CanvasContainer.Height;

            // Draw vertical lines
            for (double x = 0; x <= width; x += gridSize)
            {
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    StrokeThickness = 0.5,
                    Opacity = 0.3
                };
                GridCanvas.Children.Add(line);
            }

            // Draw horizontal lines
            for (double y = 0; y <= height; y += gridSize)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    StrokeThickness = 0.5,
                    Opacity = 0.3
                };
                GridCanvas.Children.Add(line);
            }
        }

        private void CanvasContainer_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.TryGetValue("ShapeTemplate", out var templateObj))
            {
                var template = templateObj as ShapeTemplate;
                if (template == null) return;

                var dropPosition = e.GetPosition(CanvasContainer);
                var node = template.CreateNode(dropPosition);
                
                ViewModel.AddNode(node);
            }
        }

        private void CanvasContainer_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }

        private void CanvasContainer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Clear selection when clicking on empty canvas
            if (ReferenceEquals(e.OriginalSource, CanvasContainer))
            {
                ViewModel.ClearSelection();
            }
        }

        private void Node_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is CanvasNode node)
            {
                System.Diagnostics.Debug.WriteLine($"[TAP] Node tapped: {node.Id}");
                
                // Check if Ctrl key is pressed using keyboard state
                var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
                var addToSelection = ctrlState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                
                ViewModel.SelectNode(node, addToSelection);
                e.Handled = true;
                
                System.Diagnostics.Debug.WriteLine($"[TAP] Selection complete. IsSelected: {node.IsSelected}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TAP] Failed - sender type: {sender?.GetType().Name}, has tag: {(sender as Border)?.Tag != null}");
            }
        }

        private void Node_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is CanvasNode node)
            {
                _manipulatedNode = node;
                var currentPos = new Point(Canvas.GetLeft(border), Canvas.GetTop(border));
                System.Diagnostics.Debug.WriteLine($"[MANIP START] Node: {node.Id}, Current Canvas Pos: ({currentPos.X}, {currentPos.Y}), Model Pos: ({node.PositionX}, {node.PositionY})");
                System.Diagnostics.Debug.WriteLine($"[MANIP START] Border ManipulationMode: {border.ManipulationMode}, IsHitTestVisible: {border.IsHitTestVisible}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MANIP START] Failed - sender type: {sender?.GetType().Name}");
            }
        }

        private void Node_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (_manipulatedNode != null && sender is Border border)
            {
                var delta = e.Delta.Translation;
                System.Diagnostics.Debug.WriteLine($"[MANIP DELTA] Delta: ({delta.X}, {delta.Y}), Cumulative: ({e.Cumulative.Translation.X}, {e.Cumulative.Translation.Y})");
                
                var oldPos = new Point(_manipulatedNode.PositionX, _manipulatedNode.PositionY);
                
                // Don't snap during drag - apply smooth movement
                var newPosition = new Point(
                    _manipulatedNode.PositionX + delta.X,
                    _manipulatedNode.PositionY + delta.Y
                );

                _manipulatedNode.PositionX = newPosition.X;
                _manipulatedNode.PositionY = newPosition.Y;
                
                var actualCanvasPos = new Point(Canvas.GetLeft(border), Canvas.GetTop(border));
                System.Diagnostics.Debug.WriteLine($"[MANIP DELTA] Old: ({oldPos.X}, {oldPos.Y}) -> New: ({newPosition.X}, {newPosition.Y}), Canvas: ({actualCanvasPos.X}, {actualCanvasPos.Y})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MANIP DELTA] Skipped - _manipulatedNode null: {_manipulatedNode == null}, sender is Border: {sender is Border}");
            }
        }

        private void Node_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_manipulatedNode != null && sender is Border border)
            {
                // Apply snap-to-grid only when drag completes
                if (ViewModel.SnapToGrid)
                {
                    var snappedPos = ViewModel.SnapPointToGrid(new Point(_manipulatedNode.PositionX, _manipulatedNode.PositionY));
                    _manipulatedNode.PositionX = snappedPos.X;
                    _manipulatedNode.PositionY = snappedPos.Y;
                    System.Diagnostics.Debug.WriteLine($"[MANIP COMPLETE] Snapped to grid: ({snappedPos.X}, {snappedPos.Y})");
                }
                
                var finalPos = new Point(Canvas.GetLeft(border), Canvas.GetTop(border));
                System.Diagnostics.Debug.WriteLine($"[MANIP COMPLETE] Node: {_manipulatedNode.Id}, Final Canvas Pos: ({finalPos.X}, {finalPos.Y}), Model Pos: ({_manipulatedNode.PositionX}, {_manipulatedNode.PositionY})");
                System.Diagnostics.Debug.WriteLine($"[MANIP COMPLETE] Cumulative: ({e.Cumulative.Translation.X}, {e.Cumulative.Translation.Y}), Velocities: ({e.Velocities.Linear.X}, {e.Velocities.Linear.Y})");
                _manipulatedNode = null;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MANIP COMPLETE] _manipulatedNode was null or sender not Border");
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ZoomIn();
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ZoomOut();
            ApplyZoom();
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetZoom();
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            var scaleTransform = new ScaleTransform
            {
                ScaleX = ViewModel.ZoomLevel,
                ScaleY = ViewModel.ZoomLevel
            };
            DiagramElementsCanvas.RenderTransform = scaleTransform;
        }
    }
}
