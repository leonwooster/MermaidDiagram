using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using MermaidDiagramApp.Commands;
using System;
using System.Collections.Generic;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.ViewModels
{
    public class DiagramBuilderViewModel : INotifyPropertyChanged
    {
        private FlowchartNode? _selectedNode;
        private FlowchartEdge? _selectedEdge;
        private string _generatedMermaidCode = string.Empty;
        private bool _isParsing;

        public ObservableCollection<FlowchartNode> Nodes { get; } = new();
        public ObservableCollection<FlowchartEdge> Edges { get; } = new();

        public FlowchartNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    ((RelayCommand)RemoveNodeCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public FlowchartEdge? SelectedEdge
        {
            get => _selectedEdge;
            set
            {
                if (SetProperty(ref _selectedEdge, value))
                {
                    ((RelayCommand)RemoveEdgeCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string GeneratedMermaidCode
        {
            get => _generatedMermaidCode;
            private set => SetProperty(ref _generatedMermaidCode, value);
        }

        public ICommand AddNodeCommand { get; }
        public ICommand RemoveNodeCommand { get; }
        public ICommand AddEdgeCommand { get; }
        public ICommand RemoveEdgeCommand { get; }

        public DiagramBuilderViewModel()
        {
            AddNodeCommand = new RelayCommand(_ => AddNode());
            RemoveNodeCommand = new RelayCommand(_ => RemoveSelectedNode(), _ => SelectedNode != null);
            AddEdgeCommand = new RelayCommand(_ => AddEdge(), _ => Nodes.Count >= 2);
            RemoveEdgeCommand = new RelayCommand(_ => RemoveSelectedEdge(), _ => SelectedEdge != null);

            Nodes.CollectionChanged += OnNodesCollectionChanged;
            Edges.CollectionChanged += OnEdgesCollectionChanged;
        }

        private void AddNode()
        {
            var newNode = new FlowchartNode();
            Nodes.Add(newNode);
            SelectedNode = newNode;
        }

        private void RemoveSelectedNode()
        {
            if (SelectedNode is null) return;

            var edgesToRemove = Edges.Where(e => e.StartNodeId == SelectedNode.Id || e.EndNodeId == SelectedNode.Id).ToList();
            foreach (var edge in edgesToRemove)
            {
                Edges.Remove(edge);
            }
            Nodes.Remove(SelectedNode);
            SelectedNode = null;
        }

        private void AddEdge()
        {
            if (Nodes.Count < 2) return;

            var newEdge = new FlowchartEdge
            {
                StartNodeId = Nodes[0].Id,
                EndNodeId = Nodes[1].Id
            };
            Edges.Add(newEdge);
            SelectedEdge = newEdge;
        }

        private void RemoveSelectedEdge()
        {
            if (SelectedEdge is null) return;
            Edges.Remove(SelectedEdge);
            SelectedEdge = null;
        }

        public void ParseMermaidCode(string code)
        {
            if (_isParsing) return;

            _isParsing = true;

            try
            {
                Nodes.CollectionChanged -= OnNodesCollectionChanged;
                Edges.CollectionChanged -= OnEdgesCollectionChanged;

                var newNodes = new ObservableCollection<FlowchartNode>();
                var newEdges = new ObservableCollection<FlowchartEdge>();

                var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (TryParseEdge(trimmedLine, newEdges, newNodes) || TryParseNode(trimmedLine, newNodes))
                    {
                        continue;
                    }
                }

                // Batch update the collections to minimize UI flicker and change notifications
                Nodes.Clear();
                foreach (var node in newNodes) Nodes.Add(node);

                Edges.Clear();
                foreach (var edge in newEdges) Edges.Add(edge);
            }
            finally
            {
                Nodes.CollectionChanged += OnNodesCollectionChanged;
                Edges.CollectionChanged += OnEdgesCollectionChanged;
                _isParsing = false;
                RegenerateMermaidCode(); // Regenerate code to normalize formatting
            }
        }

        private void RegenerateMermaidCode()
        {
            if (_isParsing) return; // Prevent regeneration while parsing from text

            var sb = new StringBuilder();
            sb.AppendLine("graph TD");

            foreach (var node in Nodes)
            {
                sb.AppendLine($"    {GetNodeSyntax(node)}");
                node.PropertyChanged -= OnNodePropertyChanged;
                node.PropertyChanged += OnNodePropertyChanged;
            }

            foreach (var edge in Edges)
            {
                sb.AppendLine($"    {GetEdgeSyntax(edge)}");
                edge.PropertyChanged -= OnEdgePropertyChanged;
                edge.PropertyChanged += OnEdgePropertyChanged;
            }

            GeneratedMermaidCode = sb.ToString();
        }

        private void OnNodesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RegenerateMermaidCode();
            ((RelayCommand)AddEdgeCommand).RaiseCanExecuteChanged();
        }

        private void OnEdgesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RegenerateMermaidCode();
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RegenerateMermaidCode();
        }

        private void OnEdgePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RegenerateMermaidCode();
        }

        private static string GetNodeSyntax(FlowchartNode node)
        {
            var text = $"\"{node.Text}\"";
            return node.Shape switch
            {
                NodeShape.Rectangle => $"{node.Id}[{text}]",
                NodeShape.RoundEdges => $"{node.Id}({text})",
                NodeShape.Stadium => $"{node.Id}([{text}])",
                NodeShape.Subroutine => $"{node.Id}[[{text}]]",
                NodeShape.Cylindrical => $"{node.Id}[({text})]",
                NodeShape.Circle => $"{node.Id}(({text}))",
                NodeShape.Asymmetric => $"{node.Id}>{text}]",
                NodeShape.Rhombus => $"{node.Id}{{{text}}}",
                NodeShape.Hexagon => $"{node.Id}{{{{{text}}}}}",
                NodeShape.Parallelogram => $"{node.Id}[/{text}/]",
                NodeShape.ParallelogramAlt => $"{node.Id}[\\{text}\\]",
                NodeShape.Trapezoid => $"{node.Id}[/\\{text}/]",
                NodeShape.TrapezoidAlt => $"{node.Id}[\\/{text}/]",
                _ => $"{node.Id}[{text}]",
            };
        }

        private static string GetEdgeSyntax(FlowchartEdge edge)
        {
            var arrow = edge.ArrowType switch
            {
                ArrowType.Arrow => "-->",
                ArrowType.Open => "---",
                ArrowType.Dotted => "-.->",
                ArrowType.Thick => "==>",
                _ => "-->",
            };

            return string.IsNullOrWhiteSpace(edge.Text)
                ? $"{edge.StartNodeId} {arrow} {edge.EndNodeId}"
                : $"{edge.StartNodeId} {arrow}|{edge.Text}| {edge.EndNodeId}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        private bool TryParseNode(string line, ObservableCollection<FlowchartNode> nodes)
        {
            var nodeRegex = new System.Text.RegularExpressions.Regex(@"^\s*(\w+)(?:\s*\[(.*?)\]|\s*\((.*?)\)|\s*\{(.*?)\})?\s*$");
            var match = nodeRegex.Match(line);

            if (!match.Success) return false;

            var id = match.Groups[1].Value;
            if (nodes.Any(n => n.Id == id)) return true; // Already exists

            var node = new FlowchartNode { Id = id };
            string text = id;
            NodeShape shape = NodeShape.Rectangle; // Default

            if (match.Groups[2].Success) { text = match.Groups[2].Value; shape = NodeShape.Rectangle; }
            else if (match.Groups[3].Success) { text = match.Groups[3].Value; shape = NodeShape.RoundEdges; }
            else if (match.Groups[4].Success) { text = match.Groups[4].Value; shape = NodeShape.Rhombus; }

            node.Text = text.Trim('"');
            node.Shape = shape;
            nodes.Add(node);
            return true;
        }

        private bool TryParseEdge(string line, ObservableCollection<FlowchartEdge> edges, ObservableCollection<FlowchartNode> nodes)
        {
            var edgeRegex = new System.Text.RegularExpressions.Regex(@"^\s*(\w+)\s*(?<arrow>-->|---|-.->|==>)\s*(?:\|(.*?)\|\s*)?(\w+)\s*$");
            var match = edgeRegex.Match(line);
            if (!match.Success) return false;

            var startId = match.Groups[1].Value;
            var endId = match.Groups[4].Value;

            // Ensure nodes exist, create if they don't
            if (!nodes.Any(n => n.Id == startId)) nodes.Add(new FlowchartNode { Id = startId, Text = startId });
            if (!nodes.Any(n => n.Id == endId)) nodes.Add(new FlowchartNode { Id = endId, Text = endId });

            var edge = new FlowchartEdge
            {
                StartNodeId = startId,
                EndNodeId = endId,
                Text = match.Groups[3].Success ? match.Groups[3].Value : string.Empty,
                ArrowType = match.Groups["arrow"].Value switch
                {
                    "---" => ArrowType.Open,
                    "-.->" => ArrowType.Dotted,
                    "==>" => ArrowType.Thick,
                    _ => ArrowType.Arrow
                }
            };

            edges.Add(edge);
            return true;
        }
    }
}
