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
using MermaidDiagramApp.Models;
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
        private Dictionary<CanvasNode, Microsoft.UI.Xaml.Shapes.Rectangle> _nodeBoundingBoxes = new Dictionary<CanvasNode, Microsoft.UI.Xaml.Shapes.Rectangle>();
        private Dictionary<CanvasNode, Grid> _nodeResizeHandles = new Dictionary<CanvasNode, Grid>();
        private CanvasConnector? _selectedConnector;
        private Dictionary<Line, CanvasConnector> _connectorLines = new Dictionary<Line, CanvasConnector>();
        private Dictionary<CanvasConnector, (Ellipse start, Ellipse end)> _connectorHandles = new Dictionary<CanvasConnector, (Ellipse, Ellipse)>();
        private bool _isDraggingConnectorEndpoint;
        private Ellipse? _draggedEndpoint;
        private CanvasConnector? _reconnectingConnector;
        private bool _isStartPoint;

        public DiagramCanvas()
        {
            this.InitializeComponent();
            ViewModel = new DiagramCanvasViewModel();
            
            Loaded += DiagramCanvas_Loaded;
            
            // Enable keyboard input
            this.KeyDown += DiagramCanvas_KeyDown;
        }

        private void DiagramCanvas_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Delete)
            {
                DeleteSelectedNodes();
                e.Handled = true;
            }
        }

        private void DeleteSelectedNodes()
        {
            // Check if a connector is selected
            if (_selectedConnector != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DELETE] Deleting connector: {_selectedConnector.Id}");
                DeleteConnector(_selectedConnector);
                return;
            }
            
            // Otherwise delete selected nodes
            var selectedNodes = ViewModel.Nodes.Where(n => n.IsSelected).ToList();
            if (selectedNodes.Count == 0)
                return;

            System.Diagnostics.Debug.WriteLine($"[DELETE] Deleting {selectedNodes.Count} node(s)");
            
            foreach (var node in selectedNodes)
            {
                ViewModel.RemoveNode(node);
                RemoveNodeVisual(node);
            }
        }
        
        private void DeleteConnector(CanvasConnector connector)
        {
            // Find and remove the line visual
            var lineToRemove = _connectorLines.FirstOrDefault(kvp => kvp.Value == connector).Key;
            if (lineToRemove != null)
            {
                NodesCanvas.Children.Remove(lineToRemove);
                _connectorLines.Remove(lineToRemove);
            }
            
            // Remove endpoint handles
            if (_connectorHandles.TryGetValue(connector, out var handles))
            {
                NodesCanvas.Children.Remove(handles.start);
                NodesCanvas.Children.Remove(handles.end);
                _connectorHandles.Remove(connector);
            }
            
            // Remove from view model
            ViewModel.RemoveConnector(connector);
            
            // Clear selection
            _selectedConnector = null;
            
            System.Diagnostics.Debug.WriteLine($"[DELETE] Connector deleted");
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
                ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY,
                Tag = node
            };
            
            // Apply shape-specific styling
            ApplyNodeShape(border, node.Shape);
            
            System.Diagnostics.Debug.WriteLine($"[CREATE] Border created with ManipulationMode: {border.ManipulationMode}, Shape: {node.Shape}");

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
                    
                    // Update bounding box and resize handles position
                    var padding = 10.0;
                    if (_nodeBoundingBoxes.TryGetValue(node, out var bbox))
                    {
                        Canvas.SetLeft(bbox, node.PositionX - padding);
                    }
                    if (_nodeResizeHandles.TryGetValue(node, out var handles))
                    {
                        Canvas.SetLeft(handles, node.PositionX - padding);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] PositionX changed: {oldLeft} -> {node.PositionX}");
                }
                else if (e.PropertyName == nameof(CanvasNode.PositionY))
                {
                    var oldTop = Canvas.GetTop(border);
                    Canvas.SetTop(border, node.PositionY);
                    
                    // Update bounding box and resize handles position
                    var padding = 10.0;
                    if (_nodeBoundingBoxes.TryGetValue(node, out var bbox))
                    {
                        Canvas.SetTop(bbox, node.PositionY - padding);
                    }
                    if (_nodeResizeHandles.TryGetValue(node, out var handles))
                    {
                        Canvas.SetTop(handles, node.PositionY - padding);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] PositionY changed: {oldTop} -> {node.PositionY}");
                }
                else if (e.PropertyName == nameof(CanvasNode.IsSelected))
                {
                    selectionBorder.Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                    
                    // Update bounding box and resize handles visibility
                    if (_nodeBoundingBoxes.TryGetValue(node, out var bbox))
                    {
                        bbox.Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                    }
                    if (_nodeResizeHandles.TryGetValue(node, out var handles))
                    {
                        handles.Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] IsSelected changed: {node.IsSelected}");
                }
                else if (e.PropertyName == nameof(CanvasNode.SizeWidth) || e.PropertyName == nameof(CanvasNode.SizeHeight))
                {
                    border.Width = node.SizeWidth;
                    border.Height = node.SizeHeight;
                    
                    // Update bounding box and resize handles size/position
                    var padding = 10.0;
                    if (_nodeBoundingBoxes.TryGetValue(node, out var bbox))
                    {
                        bbox.Width = node.SizeWidth + (padding * 2);
                        bbox.Height = node.SizeHeight + (padding * 2);
                        Canvas.SetLeft(bbox, node.PositionX - padding);
                        Canvas.SetTop(bbox, node.PositionY - padding);
                    }
                    if (_nodeResizeHandles.TryGetValue(node, out var handles))
                    {
                        handles.Width = node.SizeWidth + (padding * 2);
                        handles.Height = node.SizeHeight + (padding * 2);
                        Canvas.SetLeft(handles, node.PositionX - padding);
                        Canvas.SetTop(handles, node.PositionY - padding);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] Size changed: {node.SizeWidth}x{node.SizeHeight}");
                }
                else if (e.PropertyName == nameof(CanvasNode.Text))
                {
                    textBlock.Text = node.Text;
                    System.Diagnostics.Debug.WriteLine($"[PROP CHANGE] Text changed: {node.Text}");
                }
            };

            NodesCanvas.Children.Add(border);
            System.Diagnostics.Debug.WriteLine($"[CREATE] Node visual added to canvas. Total nodes: {NodesCanvas.Children.Count}");

            // Create bounding box and resize handles OUTSIDE the node, directly on canvas
            CreateBoundingBoxAndHandles(node, border);
        }

        private void RemoveNodeVisual(CanvasNode node)
        {
            var borderToRemove = NodesCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == node);
            if (borderToRemove != null)
            {
                _nodeHandlesMap.Remove(borderToRemove);
                NodesCanvas.Children.Remove(borderToRemove);
            }

            // Remove bounding box and resize handles
            if (_nodeBoundingBoxes.TryGetValue(node, out var bbox))
            {
                NodesCanvas.Children.Remove(bbox);
                _nodeBoundingBoxes.Remove(node);
            }
            if (_nodeResizeHandles.TryGetValue(node, out var handles))
            {
                NodesCanvas.Children.Remove(handles);
                _nodeResizeHandles.Remove(node);
            }
        }

        private void CreateBoundingBoxAndHandles(CanvasNode node, Border nodeBorder)
        {
            var padding = 10.0;

            // Create bounding box as a separate Rectangle on the canvas
            var boundingBox = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray) { Opacity = 0.5 },
                StrokeThickness = 1,
                Fill = null,
                Width = node.SizeWidth + (padding * 2),
                Height = node.SizeHeight + (padding * 2),
                Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(boundingBox, node.PositionX - padding);
            Canvas.SetTop(boundingBox, node.PositionY - padding);
            Canvas.SetZIndex(boundingBox, 50);
            NodesCanvas.Children.Add(boundingBox);
            _nodeBoundingBoxes[node] = boundingBox;

            // Create resize handles grid on the canvas
            var resizeHandlesGrid = new Grid
            {
                Width = node.SizeWidth + (padding * 2),
                Height = node.SizeHeight + (padding * 2),
                Visibility = node.IsSelected ? Visibility.Visible : Visibility.Collapsed
            };
            Canvas.SetLeft(resizeHandlesGrid, node.PositionX - padding);
            Canvas.SetTop(resizeHandlesGrid, node.PositionY - padding);
            Canvas.SetZIndex(resizeHandlesGrid, 100);

            // Create 4 corner resize handles
            var handleSize = 12.0;
            var positions = new[]
            {
                new { V = VerticalAlignment.Top, H = HorizontalAlignment.Left, Type = "nw", Cursor = Microsoft.UI.Input.InputSystemCursorShape.SizeNorthwestSoutheast },
                new { V = VerticalAlignment.Top, H = HorizontalAlignment.Right, Type = "ne", Cursor = Microsoft.UI.Input.InputSystemCursorShape.SizeNortheastSouthwest },
                new { V = VerticalAlignment.Bottom, H = HorizontalAlignment.Right, Type = "se", Cursor = Microsoft.UI.Input.InputSystemCursorShape.SizeNorthwestSoutheast },
                new { V = VerticalAlignment.Bottom, H = HorizontalAlignment.Left, Type = "sw", Cursor = Microsoft.UI.Input.InputSystemCursorShape.SizeNortheastSouthwest }
            };

            foreach (var pos in positions)
            {
                var handle = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                    Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
                    StrokeThickness = 3,
                    VerticalAlignment = pos.V,
                    HorizontalAlignment = pos.H,
                    Tag = pos.Type,
                    IsHitTestVisible = true,
                    RadiusX = 2,
                    RadiusY = 2
                };

                // Set cursor for resize handle
                handle.PointerEntered += (s, e) =>
                {
                    this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(pos.Cursor);
                };
                handle.PointerExited += (s, e) =>
                {
                    this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
                };

                handle.PointerPressed += (s, e) => ResizeHandle_PointerPressed(s, e, node, nodeBorder, pos.Type);
                handle.PointerMoved += ResizeHandle_PointerMoved;
                handle.PointerReleased += ResizeHandle_PointerReleased;

                resizeHandlesGrid.Children.Add(handle);
            }

            NodesCanvas.Children.Add(resizeHandlesGrid);
            _nodeResizeHandles[node] = resizeHandlesGrid;

            System.Diagnostics.Debug.WriteLine($"[CREATE] Bounding box and resize handles created on canvas for {node.Id}");
        }

        private void ApplyNodeShape(Border border, MermaidDiagramApp.Models.NodeShape shape)
        {
            // Remove any existing transform to avoid conflicts
            border.RenderTransform = null;
            
            switch (shape)
            {
                case NodeShape.Rectangle:
                    border.CornerRadius = new CornerRadius(0);
                    break;
                case NodeShape.RoundEdges:
                case NodeShape.Stadium:
                    border.CornerRadius = new CornerRadius(border.Height / 2);
                    break;
                case NodeShape.Circle:
                    border.CornerRadius = new CornerRadius(Math.Min(border.Width, border.Height) / 2);
                    break;
                case NodeShape.Rhombus:
                    // Rhombus shown as rounded rectangle for now (transform conflicts with handles)
                    border.CornerRadius = new CornerRadius(8);
                    break;
                case NodeShape.Hexagon:
                    border.CornerRadius = new CornerRadius(6);
                    break;
                case NodeShape.Parallelogram:
                case NodeShape.ParallelogramAlt:
                case NodeShape.Trapezoid:
                case NodeShape.TrapezoidAlt:
                    border.CornerRadius = new CornerRadius(4);
                    break;
                default:
                    border.CornerRadius = new CornerRadius(4);
                    break;
            }
        }

        private Grid CreateResizeHandles(CanvasNode node, Border nodeBorder)
        {
            var handlesGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(-10, -10, -10, -10) // Align with bounding box
            };
            var handleSize = 12.0; // Larger for better visibility
            var handleColor = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue); // Blue fill for high visibility
            var handleBorder = new SolidColorBrush(Microsoft.UI.Colors.White); // White border for contrast

            // Create 4 corner resize handles only (to avoid overlap with connection handles)
            var positions = new[]
            {
                new { V = VerticalAlignment.Top, H = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 0), Type = "nw" },
                new { V = VerticalAlignment.Top, H = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 0, 0), Type = "ne" },
                new { V = VerticalAlignment.Bottom, H = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 0, 0), Type = "se" },
                new { V = VerticalAlignment.Bottom, H = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 0), Type = "sw" }
            };

            foreach (var pos in positions)
            {
                var handle = new Rectangle
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = handleColor,
                    Stroke = handleBorder,
                    StrokeThickness = 3, // Thicker border for visibility
                    VerticalAlignment = pos.V,
                    HorizontalAlignment = pos.H,
                    Margin = pos.Margin,
                    Tag = pos.Type,
                    IsHitTestVisible = true,
                    RadiusX = 2,
                    RadiusY = 2 // Slightly rounded corners
                };

                // Wire up pointer events for resizing
                handle.PointerPressed += (s, e) => ResizeHandle_PointerPressed(s, e, node, nodeBorder, pos.Type);
                handle.PointerMoved += ResizeHandle_PointerMoved;
                handle.PointerReleased += ResizeHandle_PointerReleased;

                handlesGrid.Children.Add(handle);
                System.Diagnostics.Debug.WriteLine($"[RESIZE HANDLE] Created {pos.Type} handle at {pos.V}, {pos.H}");
            }

            System.Diagnostics.Debug.WriteLine($"[CREATE RESIZE HANDLES] Created {handlesGrid.Children.Count} handles for node {node.Id}");
            return handlesGrid;
        }

        private Point _resizeStartPoint;
        private Size _resizeStartSize;
        private Point _resizeStartPosition;
        private string? _resizeHandleType;
        private CanvasNode? _resizingNode;

        private void ResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e, CanvasNode node, Border nodeBorder, string handleType)
        {
            if (sender is Rectangle handle)
            {
                _resizingNode = node;
                _resizeHandleType = handleType;
                _resizeStartPoint = e.GetCurrentPoint(NodesCanvas).Position;
                _resizeStartSize = new Size(node.SizeWidth, node.SizeHeight);
                _resizeStartPosition = new Point(node.PositionX, node.PositionY);
                handle.CapturePointer(e.Pointer);
                System.Diagnostics.Debug.WriteLine($"[RESIZE] Started resizing {node.Id} from handle: {handleType}");
                e.Handled = true;
            }
        }

        private void ResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_resizingNode != null && _resizeHandleType != null)
            {
                var currentPoint = e.GetCurrentPoint(NodesCanvas).Position;
                var deltaX = currentPoint.X - _resizeStartPoint.X;
                var deltaY = currentPoint.Y - _resizeStartPoint.Y;

                var newWidth = _resizeStartSize.Width;
                var newHeight = _resizeStartSize.Height;
                var newX = _resizeStartPosition.X;
                var newY = _resizeStartPosition.Y;

                // Apply delta based on handle type (corners only)
                switch (_resizeHandleType)
                {
                    case "nw":
                        newWidth = Math.Max(50, _resizeStartSize.Width - deltaX);
                        newHeight = Math.Max(30, _resizeStartSize.Height - deltaY);
                        newX = _resizeStartPosition.X + (_resizeStartSize.Width - newWidth);
                        newY = _resizeStartPosition.Y + (_resizeStartSize.Height - newHeight);
                        break;
                    case "ne":
                        newWidth = Math.Max(50, _resizeStartSize.Width + deltaX);
                        newHeight = Math.Max(30, _resizeStartSize.Height - deltaY);
                        newY = _resizeStartPosition.Y + (_resizeStartSize.Height - newHeight);
                        break;
                    case "se":
                        newWidth = Math.Max(50, _resizeStartSize.Width + deltaX);
                        newHeight = Math.Max(30, _resizeStartSize.Height + deltaY);
                        break;
                    case "sw":
                        newWidth = Math.Max(50, _resizeStartSize.Width - deltaX);
                        newHeight = Math.Max(30, _resizeStartSize.Height + deltaY);
                        newX = _resizeStartPosition.X + (_resizeStartSize.Width - newWidth);
                        break;
                }

                // Apply snap to grid if enabled
                if (ViewModel.SnapToGrid)
                {
                    newWidth = Math.Round(newWidth / ViewModel.GridSize) * ViewModel.GridSize;
                    newHeight = Math.Round(newHeight / ViewModel.GridSize) * ViewModel.GridSize;
                    newX = Math.Round(newX / ViewModel.GridSize) * ViewModel.GridSize;
                    newY = Math.Round(newY / ViewModel.GridSize) * ViewModel.GridSize;
                }

                _resizingNode.SizeWidth = newWidth;
                _resizingNode.SizeHeight = newHeight;
                _resizingNode.PositionX = newX;
                _resizingNode.PositionY = newY;

                e.Handled = true;
            }
        }

        private void ResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_resizingNode != null && sender is Rectangle handle)
            {
                handle.ReleasePointerCapture(e.Pointer);
                System.Diagnostics.Debug.WriteLine($"[RESIZE] Completed resizing {_resizingNode.Id} to {_resizingNode.SizeWidth}x{_resizingNode.SizeHeight}");
                _resizingNode = null;
                _resizeHandleType = null;
                e.Handled = true;
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

                // Set cursor for connection handle
                handle.PointerEntered += (s, e) =>
                {
                    if (!_isDrawingConnection)
                    {
                        this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Cross);
                    }
                    ConnectionHandle_PointerEntered(s, e, node, pos.Anchor);
                };
                handle.PointerExited += (s, e) =>
                {
                    if (!_isDrawingConnection)
                    {
                        this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
                    }
                    ConnectionHandle_PointerExited(s, e);
                };
                
                // Wire up pointer events for dragging connections
                handle.PointerPressed += (s, e) => ConnectionHandle_PointerPressed(s, e, node, nodeBorder, pos.Anchor);
                handle.PointerMoved += ConnectionHandle_PointerMoved;
                handle.PointerReleased += ConnectionHandle_PointerReleased;

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
            // Deselect all nodes when clicking on empty canvas
            foreach (var node in ViewModel.Nodes)
            {
                node.IsSelected = false;
            }
            
            // Deselect connector
            if (_selectedConnector != null)
            {
                var line = _connectorLines.FirstOrDefault(kvp => kvp.Value == _selectedConnector).Key;
                if (line != null)
                {
                    line.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black);
                    line.StrokeThickness = 2;
                }
                
                // Hide handles
                if (_connectorHandles.TryGetValue(_selectedConnector, out var handles))
                {
                    handles.start.Visibility = Visibility.Collapsed;
                    handles.end.Visibility = Visibility.Collapsed;
                }
                
                _selectedConnector = null;
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
                Tag = connector,
                StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
                StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round
            };
            
            // Make line selectable
            line.PointerPressed += Connector_PointerPressed;
            line.PointerEntered += (s, e) =>
            {
                this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
                if (_selectedConnector != connector)
                {
                    line.StrokeThickness = 3; // Highlight on hover
                }
            };
            line.PointerExited += (s, e) =>
            {
                this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
                if (_selectedConnector != connector)
                {
                    line.StrokeThickness = 2; // Normal thickness
                }
            };
            
            // Store connector-line mapping
            _connectorLines[line] = connector;
            
            // Create drag handles at endpoints
            var startHandle = CreateEndpointHandle();
            var endHandle = CreateEndpointHandle();
            _connectorHandles[connector] = (startHandle, endHandle);
            
            // Wire up drag events for reconnection
            startHandle.PointerPressed += (s, e) => EndpointHandle_PointerPressed(s, e, connector, true);
            startHandle.PointerMoved += EndpointHandle_PointerMoved;
            startHandle.PointerReleased += EndpointHandle_PointerReleased;
            
            endHandle.PointerPressed += (s, e) => EndpointHandle_PointerPressed(s, e, connector, false);
            endHandle.PointerMoved += EndpointHandle_PointerMoved;
            endHandle.PointerReleased += EndpointHandle_PointerReleased;
            
            // Add handles to canvas (initially hidden)
            NodesCanvas.Children.Add(startHandle);
            NodesCanvas.Children.Add(endHandle);
            startHandle.Visibility = Visibility.Collapsed;
            endHandle.Visibility = Visibility.Collapsed;
            
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
        
        private void Connector_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Line line && line.Tag is CanvasConnector connector)
            {
                // Deselect all nodes
                foreach (var node in ViewModel.Nodes)
                {
                    node.IsSelected = false;
                }
                
                // Select this connector
                SelectConnector(connector);
                
                System.Diagnostics.Debug.WriteLine($"[CONNECTOR] Selected connector: {connector.Id}");
                e.Handled = true;
            }
        }
        
        private void SelectConnector(CanvasConnector connector)
        {
            // Deselect previous connector
            if (_selectedConnector != null)
            {
                var prevLine = _connectorLines.FirstOrDefault(kvp => kvp.Value == _selectedConnector).Key;
                if (prevLine != null)
                {
                    prevLine.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black);
                    prevLine.StrokeThickness = 2;
                }
                
                // Hide previous handles
                if (_connectorHandles.TryGetValue(_selectedConnector, out var prevHandles))
                {
                    prevHandles.start.Visibility = Visibility.Collapsed;
                    prevHandles.end.Visibility = Visibility.Collapsed;
                }
            }
            
            // Select new connector
            _selectedConnector = connector;
            var line = _connectorLines.FirstOrDefault(kvp => kvp.Value == connector).Key;
            if (line != null)
            {
                line.Stroke = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                line.StrokeThickness = 3;
            }
            
            // Show handles for selected connector
            if (_connectorHandles.TryGetValue(connector, out var handles))
            {
                handles.start.Visibility = Visibility.Visible;
                handles.end.Visibility = Visibility.Visible;
                UpdateEndpointHandlePositions(connector);
            }
        }
        
        private Ellipse CreateEndpointHandle()
        {
            return new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.White),
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                StrokeThickness = 2,
                IsHitTestVisible = true
            };
        }
        
        private void UpdateEndpointHandlePositions(CanvasConnector connector)
        {
            if (!_connectorHandles.TryGetValue(connector, out var handles))
                return;
                
            var startNode = ViewModel.Nodes.FirstOrDefault(n => n.Id == connector.StartNodeId);
            var endNode = ViewModel.Nodes.FirstOrDefault(n => n.Id == connector.EndNodeId);
            
            if (startNode == null || endNode == null)
                return;
                
            var startPoint = GetAnchorPoint(startNode, connector.StartAnchor);
            var endPoint = GetAnchorPoint(endNode, connector.EndAnchor);
            
            Canvas.SetLeft(handles.start, startPoint.X - 5);
            Canvas.SetTop(handles.start, startPoint.Y - 5);
            Canvas.SetZIndex(handles.start, 200);
            
            Canvas.SetLeft(handles.end, endPoint.X - 5);
            Canvas.SetTop(handles.end, endPoint.Y - 5);
            Canvas.SetZIndex(handles.end, 200);
        }
        
        private void EndpointHandle_PointerPressed(object sender, PointerRoutedEventArgs e, CanvasConnector connector, bool isStartPoint)
        {
            if (sender is Ellipse handle)
            {
                _isDraggingConnectorEndpoint = true;
                _draggedEndpoint = handle;
                _reconnectingConnector = connector;
                _isStartPoint = isStartPoint;
                
                handle.CapturePointer(e.Pointer);
                
                // Show connection handles on all nodes
                foreach (var node in ViewModel.Nodes)
                {
                    if (_nodeHandlesMap.TryGetValue(NodesCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == node), out var handleGrid))
                    {
                        handleGrid.Visibility = Visibility.Visible;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[RECONNECT] Started dragging {(isStartPoint ? "start" : "end")} point of connector {connector.Id}");
                e.Handled = true;
            }
        }
        
        private void EndpointHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingConnectorEndpoint && _draggedEndpoint != null && _reconnectingConnector != null)
            {
                var currentPos = e.GetCurrentPoint(NodesCanvas).Position;
                
                // Move the handle
                Canvas.SetLeft(_draggedEndpoint, currentPos.X - 5);
                Canvas.SetTop(_draggedEndpoint, currentPos.Y - 5);
                
                // Update the line endpoint
                var line = _connectorLines.FirstOrDefault(kvp => kvp.Value == _reconnectingConnector).Key;
                if (line != null)
                {
                    if (_isStartPoint)
                    {
                        line.X1 = currentPos.X;
                        line.Y1 = currentPos.Y;
                    }
                    else
                    {
                        line.X2 = currentPos.X;
                        line.Y2 = currentPos.Y;
                    }
                }
                
                // Check if hovering over a connection handle
                var hoveredNode = FindNodeAtPosition(currentPos);
                if (hoveredNode != null)
                {
                    _hoveredTargetNode = hoveredNode;
                }
                
                e.Handled = true;
            }
        }
        
        private void EndpointHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDraggingConnectorEndpoint && _draggedEndpoint != null && _reconnectingConnector != null)
            {
                var releasePos = e.GetCurrentPoint(NodesCanvas).Position;
                var targetNode = FindNodeAtPosition(releasePos);
                
                if (targetNode != null)
                {
                    // Determine which anchor point to connect to
                    var sourceNode = _isStartPoint 
                        ? ViewModel.Nodes.FirstOrDefault(n => n.Id == _reconnectingConnector.EndNodeId)
                        : ViewModel.Nodes.FirstOrDefault(n => n.Id == _reconnectingConnector.StartNodeId);
                    
                    if (sourceNode != null)
                    {
                        var targetAnchor = DetermineTargetAnchor(sourceNode, targetNode, releasePos);
                        
                        // Update the connector
                        if (_isStartPoint)
                        {
                            _reconnectingConnector.StartNodeId = targetNode.Id;
                            _reconnectingConnector.StartAnchor = targetAnchor;
                        }
                        else
                        {
                            _reconnectingConnector.EndNodeId = targetNode.Id;
                            _reconnectingConnector.EndAnchor = targetAnchor;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[RECONNECT] Reconnected {(_isStartPoint ? "start" : "end")} to node {targetNode.Id}({targetAnchor})");
                    }
                }
                
                // Refresh the connector visual
                var startNode = ViewModel.Nodes.FirstOrDefault(n => n.Id == _reconnectingConnector.StartNodeId);
                var endNode = ViewModel.Nodes.FirstOrDefault(n => n.Id == _reconnectingConnector.EndNodeId);
                var line = _connectorLines.FirstOrDefault(kvp => kvp.Value == _reconnectingConnector).Key;
                
                if (startNode != null && endNode != null && line != null)
                {
                    UpdateConnectorLine(line, startNode, endNode);
                    UpdateEndpointHandlePositions(_reconnectingConnector);
                }
                
                // Hide all connection handles
                HideAllNodeHandles();
                
                _draggedEndpoint.ReleasePointerCapture(e.Pointer);
                _isDraggingConnectorEndpoint = false;
                _draggedEndpoint = null;
                _reconnectingConnector = null;
                _hoveredTargetNode = null;
                
                e.Handled = true;
            }
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
                
                // Update endpoint handles if connector is selected
                if (_selectedConnector == connector)
                {
                    UpdateEndpointHandlePositions(connector);
                }
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
