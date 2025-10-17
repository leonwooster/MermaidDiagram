using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Models.Canvas;

namespace MermaidDiagramApp.ViewModels
{
    /// <summary>
    /// ViewModel for the visual diagram canvas
    /// </summary>
    public class DiagramCanvasViewModel : INotifyPropertyChanged
    {
        private DiagramType _diagramType;
        private double _zoomLevel;
        private Point _panOffset;
        private bool _showGrid;
        private bool _snapToGrid;
        private double _gridSize;
        private CanvasNode? _selectedNode;
        private CanvasConnector? _selectedConnector;
        private bool _isDrawingConnection;
        private string _generatedMermaidCode;
        private bool _hasUnsavedChanges;

        public ObservableCollection<CanvasNode> Nodes { get; }
        public ObservableCollection<CanvasConnector> Connectors { get; }
        public ObservableCollection<object> SelectedElements { get; }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public DiagramType DiagramType
        {
            get => _diagramType;
            set => SetProperty(ref _diagramType, value);
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0.25, 4.0));
        }

        public Point PanOffset
        {
            get => _panOffset;
            set => SetProperty(ref _panOffset, value);
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set => SetProperty(ref _showGrid, value);
        }

        public bool SnapToGrid
        {
            get => _snapToGrid;
            set => SetProperty(ref _snapToGrid, value);
        }

        public double GridSize
        {
            get => _gridSize;
            set => SetProperty(ref _gridSize, value);
        }

        public CanvasNode? SelectedNode
        {
            get => _selectedNode;
            set => SetProperty(ref _selectedNode, value);
        }

        public CanvasConnector? SelectedConnector
        {
            get => _selectedConnector;
            set => SetProperty(ref _selectedConnector, value);
        }

        public bool IsDrawingConnection
        {
            get => _isDrawingConnection;
            set => SetProperty(ref _isDrawingConnection, value);
        }

        public string GeneratedMermaidCode
        {
            get => _generatedMermaidCode;
            private set => SetProperty(ref _generatedMermaidCode, value);
        }

        public DiagramCanvasViewModel()
        {
            Nodes = new ObservableCollection<CanvasNode>();
            Connectors = new ObservableCollection<CanvasConnector>();
            SelectedElements = new ObservableCollection<object>();

            _diagramType = DiagramType.Flowchart;
            _zoomLevel = 1.0;
            _panOffset = new Point(0, 0);
            _showGrid = true;
            _snapToGrid = true;
            _gridSize = 20;
            _isDrawingConnection = false;
            _generatedMermaidCode = string.Empty;
            _hasUnsavedChanges = false;

            Nodes.CollectionChanged += (s, e) => { RegenerateMermaidCode(); HasUnsavedChanges = true; };
            Connectors.CollectionChanged += (s, e) => { RegenerateMermaidCode(); HasUnsavedChanges = true; };
        }

        public void AddNode(CanvasNode node)
        {
            if (SnapToGrid)
            {
                node.Position = SnapPointToGrid(node.Position);
            }

            // Subscribe to property changes
            node.PropertyChanged += OnNodePropertyChanged;
            Nodes.Add(node);
            
            // Select the new node
            ClearSelection();
            node.IsSelected = true;
            SelectedNode = node;
            SelectedElements.Add(node);
        }

        public void RemoveNode(CanvasNode node)
        {
            // Remove all connectors attached to this node
            var connectorsToRemove = Connectors
                .Where(c => c.StartNodeId == node.Id || c.EndNodeId == node.Id)
                .ToList();

            foreach (var connector in connectorsToRemove)
            {
                RemoveConnector(connector);
            }

            node.PropertyChanged -= OnNodePropertyChanged;
            Nodes.Remove(node);
            SelectedElements.Remove(node);
            
            if (SelectedNode == node)
            {
                SelectedNode = null;
            }
        }

        public void AddConnector(CanvasConnector connector)
        {
            connector.PropertyChanged += OnConnectorPropertyChanged;
            Connectors.Add(connector);
            
            ClearSelection();
            connector.IsSelected = true;
            SelectedConnector = connector;
            SelectedElements.Add(connector);
        }

        public void RemoveConnector(CanvasConnector connector)
        {
            connector.PropertyChanged -= OnConnectorPropertyChanged;
            Connectors.Remove(connector);
            SelectedElements.Remove(connector);
            
            if (SelectedConnector == connector)
            {
                SelectedConnector = null;
            }
        }

        public void ClearSelection()
        {
            foreach (var node in Nodes)
            {
                node.IsSelected = false;
            }
            foreach (var connector in Connectors)
            {
                connector.IsSelected = false;
            }
            SelectedElements.Clear();
            SelectedNode = null;
            SelectedConnector = null;
        }

        public void SelectNode(CanvasNode node, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                ClearSelection();
            }

            node.IsSelected = true;
            SelectedNode = node;
            if (!SelectedElements.Contains(node))
            {
                SelectedElements.Add(node);
            }
        }

        public Point SnapPointToGrid(Point point)
        {
            if (!SnapToGrid) return point;

            return new Point(
                Math.Round(point.X / GridSize) * GridSize,
                Math.Round(point.Y / GridSize) * GridSize
            );
        }

        public void ZoomIn()
        {
            ZoomLevel = Math.Min(ZoomLevel * 1.25, 4.0);
        }

        public void ZoomOut()
        {
            ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.25);
        }

        public void ResetZoom()
        {
            ZoomLevel = 1.0;
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RegenerateMermaidCode();
            HasUnsavedChanges = true;
        }

        private void OnConnectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RegenerateMermaidCode();
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// Mark the canvas as saved (no unsaved changes)
        /// </summary>
        public void MarkAsSaved()
        {
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// Clear all canvas content for a new session
        /// </summary>
        public void ClearCanvas()
        {
            Nodes.Clear();
            Connectors.Clear();
            SelectedElements.Clear();
            SelectedNode = null;
            SelectedConnector = null;
            HasUnsavedChanges = false;
        }

        private void RegenerateMermaidCode()
        {
            var sb = new StringBuilder();
            
            switch (DiagramType)
            {
                case DiagramType.Flowchart:
                    GenerateFlowchartCode(sb);
                    break;
                case DiagramType.ClassDiagram:
                    GenerateClassDiagramCode(sb);
                    break;
                default:
                    GenerateFlowchartCode(sb);
                    break;
            }

            GeneratedMermaidCode = sb.ToString();
        }

        private void GenerateFlowchartCode(StringBuilder sb)
        {
            sb.AppendLine("flowchart TD");
            sb.AppendLine();

            // Generate node definitions
            foreach (var node in Nodes.OrderBy(n => n.Id))
            {
                sb.AppendLine($"    {GetNodeSyntax(node)}");
            }

            if (Nodes.Any() && Connectors.Any())
            {
                sb.AppendLine();
            }

            // Generate connections
            foreach (var connector in Connectors)
            {
                sb.AppendLine($"    {GetConnectorSyntax(connector)}");
            }

            // Add canvas metadata as comments
            if (Nodes.Any())
            {
                sb.AppendLine();
                sb.AppendLine("%% Canvas Metadata");
                foreach (var node in Nodes)
                {
                    sb.AppendLine($"%% {node.Id}: pos={node.Position.X:F0},{node.Position.Y:F0} size={node.Size.Width:F0},{node.Size.Height:F0}");
                }
            }
        }

        private void GenerateClassDiagramCode(StringBuilder sb)
        {
            sb.AppendLine("classDiagram");
            sb.AppendLine();

            foreach (var node in Nodes)
            {
                sb.AppendLine($"    class {node.Id} {{");
                sb.AppendLine($"        {node.Text}");
                sb.AppendLine("    }");
            }
        }

        private string GetNodeSyntax(CanvasNode node)
        {
            var text = EscapeText(node.Text);
            
            return node.Shape switch
            {
                NodeShape.Rectangle => $"{node.Id}[\"{text}\"]",
                NodeShape.RoundEdges => $"{node.Id}(\"{text}\")",
                NodeShape.Stadium => $"{node.Id}([\"{text}\"])",
                NodeShape.Subroutine => $"{node.Id}[[\"{text}\"]]",
                NodeShape.Cylindrical => $"{node.Id}[(\"{text}\")]",
                NodeShape.Circle => $"{node.Id}((\"{text}\"))",
                NodeShape.Asymmetric => $"{node.Id}>\"{text}\"]",
                NodeShape.Rhombus => $"{node.Id}{{\"{text}\"}}",
                NodeShape.Hexagon => $"{node.Id}{{{{\"{text}\"}}}}",
                NodeShape.Parallelogram => $"{node.Id}[/\"{text}\"/]",
                NodeShape.ParallelogramAlt => $"{node.Id}[\\\"{text}\\\"]",
                NodeShape.Trapezoid => $"{node.Id}[/\\\"{text}\\\"/]",
                NodeShape.TrapezoidAlt => $"{node.Id}[\\/\"{text}\"/]",
                _ => $"{node.Id}[\"{text}\"]"
            };
        }

        private string GetConnectorSyntax(CanvasConnector connector)
        {
            var arrow = connector.LineStyle switch
            {
                LineStyle.Solid => connector.EndArrowType == ArrowHeadType.Arrow ? "-->" : "---",
                LineStyle.Dashed => "-.->",
                LineStyle.Dotted => "-.->",
                LineStyle.Thick => "==>",
                _ => "-->"
            };

            if (!string.IsNullOrWhiteSpace(connector.Label))
            {
                return $"{connector.StartNodeId} {arrow}|\"{EscapeText(connector.Label)}\"| {connector.EndNodeId}";
            }

            return $"{connector.StartNodeId} {arrow} {connector.EndNodeId}";
        }

        private string EscapeText(string text)
        {
            return text.Replace("\"", "\\\"");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
