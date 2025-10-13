using System;
using System.Collections.Generic;
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
        private bool _isDrawingConnection;
        private CanvasNode? _connectionSourceNode;
        private Line? _temporaryConnectionLine;
        private string _connectionStartAnchor = "center";
        private string? _hoveredTargetAnchor;
        private CanvasNode? _hoveredTargetNode;
        private Dictionary<Border, Grid> _nodeHandlesMap = new Dictionary<Border, Grid>();

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

            var grid = new Grid();
            
            var textBlock = new TextBlock
            {
                Text = node.Text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(8),
                IsHitTestVisible = false
            };
            grid.Children.Add(textBlock);

            var selectionBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = false
            };
            grid.Children.Add(selectionBorder);

            // Add connection handles (draw.io style)
            var connectionHandlesGrid = CreateConnectionHandles(node, border);
            connectionHandlesGrid.Visibility = Visibility.Collapsed;
            grid.Children.Add(connectionHandlesGrid);

            // Store handles for later access
            _nodeHandlesMap[border] = connectionHandlesGrid;

            // Show/hide connection handles on hover
            border.PointerEntered += (s, e) =>
            {
                if (!_isDrawingConnection)
                {
                    connectionHandlesGrid.Visibility = Visibility.Visible;
                }
            };

            border.PointerExited += (s, e) =>
            {
                if (!_isDrawingConnection)
                {
                    connectionHandlesGrid.Visibility = Visibility.Collapsed;
                }
            };

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
                _nodeHandlesMap.Remove(borderToRemove);
                NodesCanvas.Children.Remove(borderToRemove);
            }
        }

        private Grid CreateConnectionHandles(CanvasNode node, Border nodeBorder)
        {
            var handlesGrid = new Grid();
            var handleSize = 12.0;
            var handleColor = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);

            // Create 4 connection handles (top, right, bottom, left)
            var positions = new[]
            {
                new { V = VerticalAlignment.Top, H = HorizontalAlignment.Center, Margin = new Thickness(0, -handleSize/2, 0, 0), Anchor = "top" },
                new { V = VerticalAlignment.Center, H = HorizontalAlignment.Right, Margin = new Thickness(0, 0, -handleSize/2, 0), Anchor = "right" },
                new { V = VerticalAlignment.Bottom, H = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, -handleSize/2), Anchor = "bottom" },
                new { V = VerticalAlignment.Center, H = HorizontalAlignment.Left, Margin = new Thickness(-handleSize/2, 0, 0, 0), Anchor = "left" }
            };

            foreach (var pos in positions)
            {
                var handle = new Ellipse
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = handleColor,
                    Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
                    StrokeThickness = 2,
                    VerticalAlignment = pos.V,
                    HorizontalAlignment = pos.H,
                    Margin = pos.Margin
                };

                // Store anchor position in Tag
                handle.Tag = pos.Anchor;

                // Wire up pointer events for dragging connections
                handle.PointerPressed += (s, e) => ConnectionHandle_PointerPressed(s, e, node, nodeBorder, pos.Anchor);
                handle.PointerMoved += ConnectionHandle_PointerMoved;
                handle.PointerReleased += ConnectionHandle_PointerReleased;
                handle.PointerEntered += (s, e) => ConnectionHandle_PointerEntered(s, e, node, pos.Anchor);
                handle.PointerExited += ConnectionHandle_PointerExited;

                handlesGrid.Children.Add(handle);
            }

            return handlesGrid;
        }

        private void ConnectionHandle_PointerPressed(object sender, PointerRoutedEventArgs e, CanvasNode sourceNode, Border sourceBorder, string anchor)
        {
            if (sender is Ellipse handle)
            {
                _isDrawingConnection = true;
                _connectionSourceNode = sourceNode;
                _connectionStartAnchor = anchor;
                handle.CapturePointer(e.Pointer);

                // Calculate start position based on anchor
                var startPoint = GetAnchorPoint(sourceNode, anchor);
                var currentPos = e.GetCurrentPoint(NodesCanvas).Position;

                // Create temporary line for visual feedback
                _temporaryConnectionLine = new Line
                {
                    Stroke = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 3 },
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = currentPos.X,
                    Y2 = currentPos.Y
                };

                NodesCanvas.Children.Add(_temporaryConnectionLine);
                
                // Show handles on all other nodes
                ShowAllNodeHandles(sourceNode);
                
                System.Diagnostics.Debug.WriteLine($"[CONNECTION] Started drawing connection from {sourceNode.Id} anchor: {anchor}");
                e.Handled = true;
            }
        }

        private void ShowAllNodeHandles(CanvasNode excludeNode)
        {
            foreach (var kvp in _nodeHandlesMap)
            {
                var border = kvp.Key;
                var handlesGrid = kvp.Value;
                
                if (border.Tag is CanvasNode node && node != excludeNode)
                {
                    handlesGrid.Visibility = Visibility.Visible;
                }
            }
        }

        private void HideAllNodeHandles()
        {
            foreach (var handlesGrid in _nodeHandlesMap.Values)
            {
                handlesGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void ConnectionHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDrawingConnection && _temporaryConnectionLine != null && _connectionSourceNode != null)
            {
                var currentPos = e.GetCurrentPoint(NodesCanvas).Position;
                
                // If hovering over a target handle, snap to it
                if (_hoveredTargetNode != null && _hoveredTargetAnchor != null)
                {
                    var snapPoint = GetAnchorPoint(_hoveredTargetNode, _hoveredTargetAnchor);
                    _temporaryConnectionLine.X2 = snapPoint.X;
                    _temporaryConnectionLine.Y2 = snapPoint.Y;
                }
                else
                {
                    _temporaryConnectionLine.X2 = currentPos.X;
                    _temporaryConnectionLine.Y2 = currentPos.Y;
                }
                
                e.Handled = true;
            }
        }

        private void ConnectionHandle_PointerEntered(object sender, PointerRoutedEventArgs e, CanvasNode node, string anchor)
        {
            if (_isDrawingConnection && _connectionSourceNode != null && node != _connectionSourceNode)
            {
                // Highlight this handle and track it
                if (sender is Ellipse handle)
                {
                    handle.Fill = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                    _hoveredTargetNode = node;
                    _hoveredTargetAnchor = anchor;
                    System.Diagnostics.Debug.WriteLine($"[CONNECTION] Hovering over {node.Id} anchor: {anchor}");
                }
            }
        }

        private void ConnectionHandle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_isDrawingConnection && sender is Ellipse handle)
            {
                // Reset handle color
                handle.Fill = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                _hoveredTargetNode = null;
                _hoveredTargetAnchor = null;
            }
        }

        private void ConnectionHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDrawingConnection && _connectionSourceNode != null)
            {
                if (sender is Ellipse handle)
                {
                    handle.ReleasePointerCapture(e.Pointer);
                }

                // Find if we released over a node
                var releasePos = e.GetCurrentPoint(NodesCanvas).Position;
                var targetNode = FindNodeAtPosition(releasePos);

                if (targetNode != null && targetNode != _connectionSourceNode)
                {
                    // Check if connection already exists
                    var existingConnection = ViewModel.Connectors.FirstOrDefault(c => 
                        (c.StartNodeId == _connectionSourceNode.Id && c.EndNodeId == targetNode.Id) ||
                        (c.StartNodeId == targetNode.Id && c.EndNodeId == _connectionSourceNode.Id));
                    
                    if (existingConnection != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CONNECTION] Connection already exists between {_connectionSourceNode.Id} and {targetNode.Id}");
                    }
                    else
                    {
                        // Use hovered anchor if available, otherwise determine best anchor
                        var targetAnchor = _hoveredTargetAnchor ?? DetermineTargetAnchor(_connectionSourceNode, targetNode, releasePos);

                        // Create the connection
                        var connector = new CanvasConnector
                        {
                            StartNodeId = _connectionSourceNode.Id,
                            EndNodeId = targetNode.Id,
                            Label = string.Empty,
                            StartAnchor = _connectionStartAnchor,
                            EndAnchor = targetAnchor
                        };

                        ViewModel.AddConnector(connector);
                        CreateConnectorVisual(connector);
                        System.Diagnostics.Debug.WriteLine($"[CONNECTION] Created connector: {_connectionSourceNode.Id}({_connectionStartAnchor}) -> {targetNode.Id}({targetAnchor})");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CONNECTION] Connection cancelled - no valid target");
                }

                // Clean up temporary line
                if (_temporaryConnectionLine != null)
                {
                    NodesCanvas.Children.Remove(_temporaryConnectionLine);
                    _temporaryConnectionLine = null;
                }

                // Hide all node handles
                HideAllNodeHandles();
                
                _isDrawingConnection = false;
                _connectionSourceNode = null;
                _hoveredTargetNode = null;
                _hoveredTargetAnchor = null;
                e.Handled = true;
            }
        }

        private CanvasNode? FindNodeAtPosition(Point position)
        {
            foreach (var node in ViewModel.Nodes)
            {
                var left = node.PositionX;
                var top = node.PositionY;
                var right = left + node.SizeWidth;
                var bottom = top + node.SizeHeight;

                if (position.X >= left && position.X <= right &&
                    position.Y >= top && position.Y <= bottom)
                {
                    return node;
                }
            }
            return null;
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
                
                // Normal selection mode
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

        private void CreateConnectorVisual(CanvasConnector connector)
        {
            System.Diagnostics.Debug.WriteLine($"[CONNECTOR] Creating visual for connector: {connector.Id} from {connector.StartNodeId} to {connector.EndNodeId}");
            
            // Find the start and end nodes
            var startNode = ViewModel.Nodes.FirstOrDefault(n => n.Id == connector.StartNodeId);
            var endNode = ViewModel.Nodes.FirstOrDefault(n => n.Id == connector.EndNodeId);
            
            if (startNode == null || endNode == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CONNECTOR] Failed to find nodes");
                return;
            }
            
            // Create a line to represent the connector
            var line = new Line
            {
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black),
                StrokeThickness = 2,
                Tag = connector
            };
            
            // Calculate line position
            UpdateConnectorLine(line, startNode, endNode);
            
            // Subscribe to node position changes to update the line
            startNode.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CanvasNode.PositionX) || e.PropertyName == nameof(CanvasNode.PositionY))
                {
                    UpdateConnectorLine(line, startNode, endNode);
                }
            };
            
            endNode.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CanvasNode.PositionX) || e.PropertyName == nameof(CanvasNode.PositionY))
                {
                    UpdateConnectorLine(line, startNode, endNode);
                }
            };
            
            // Add line to canvas (behind nodes)
            NodesCanvas.Children.Insert(0, line);
            System.Diagnostics.Debug.WriteLine($"[CONNECTOR] Connector visual added");
        }

        private void UpdateConnectorLine(Line line, CanvasNode startNode, CanvasNode endNode)
        {
            if (line.Tag is CanvasConnector connector)
            {
                var startPoint = GetAnchorPoint(startNode, connector.StartAnchor);
                var endPoint = GetAnchorPoint(endNode, connector.EndAnchor);
                
                line.X1 = startPoint.X;
                line.Y1 = startPoint.Y;
                line.X2 = endPoint.X;
                line.Y2 = endPoint.Y;
            }
        }

        private Point GetAnchorPoint(CanvasNode node, string anchor)
        {
            return anchor switch
            {
                "top" => new Point(node.PositionX + node.SizeWidth / 2, node.PositionY),
                "right" => new Point(node.PositionX + node.SizeWidth, node.PositionY + node.SizeHeight / 2),
                "bottom" => new Point(node.PositionX + node.SizeWidth / 2, node.PositionY + node.SizeHeight),
                "left" => new Point(node.PositionX, node.PositionY + node.SizeHeight / 2),
                _ => new Point(node.PositionX + node.SizeWidth / 2, node.PositionY + node.SizeHeight / 2) // center
            };
        }

        private string DetermineTargetAnchor(CanvasNode sourceNode, CanvasNode targetNode, Point releasePos)
        {
            // Calculate which edge of the target node is closest to the release position
            var targetCenter = new Point(
                targetNode.PositionX + targetNode.SizeWidth / 2,
                targetNode.PositionY + targetNode.SizeHeight / 2
            );

            var dx = releasePos.X - targetCenter.X;
            var dy = releasePos.Y - targetCenter.Y;

            // Determine which edge based on the angle
            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            if (angle >= -45 && angle < 45)
                return "right";
            else if (angle >= 45 && angle < 135)
                return "bottom";
            else if (angle >= -135 && angle < -45)
                return "top";
            else
                return "left";
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
