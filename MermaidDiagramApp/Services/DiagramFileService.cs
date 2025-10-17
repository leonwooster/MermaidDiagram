using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Models.Canvas;
using MermaidDiagramApp.ViewModels;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Service for saving and loading Diagram Builder files (.mmdx)
    /// </summary>
    public class DiagramFileService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Save diagram to .mmdx file
        /// </summary>
        public async Task<bool> SaveDiagramAsync(string filePath, DiagramCanvasViewModel viewModel)
        {
            try
            {
                var file = new DiagramBuilderFile
                {
                    Version = "1.0",
                    Metadata = new FileMetadata
                    {
                        Created = DateTime.Now,
                        Modified = DateTime.Now,
                        Author = Environment.UserName,
                        Application = "Mermaid Diagram Builder"
                    },
                    Diagram = SerializeDiagram(viewModel),
                    MermaidCode = viewModel.GeneratedMermaidCode
                };

                var json = JsonSerializer.Serialize(file, JsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FILE SERVICE] Error saving diagram: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load diagram from .mmdx file
        /// </summary>
        public async Task<DiagramBuilderFile?> LoadDiagramAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[FILE SERVICE] File not found: {filePath}");
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var file = JsonSerializer.Deserialize<DiagramBuilderFile>(json, JsonOptions);
                return file;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FILE SERVICE] Error loading diagram: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Export diagram to plain .mmd file (Mermaid code only)
        /// </summary>
        public async Task<bool> ExportToMermaidAsync(string filePath, string mermaidCode)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, mermaidCode);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FILE SERVICE] Error exporting to Mermaid: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restore canvas state from loaded file
        /// </summary>
        public void RestoreDiagram(DiagramBuilderFile file, DiagramCanvasViewModel viewModel)
        {
            if (file?.Diagram == null) return;

            try
            {
                // Clear existing diagram
                viewModel.Nodes.Clear();
                viewModel.Connectors.Clear();
                viewModel.SelectedElements.Clear();

                // Restore settings
                viewModel.ShowGrid = file.Diagram.Settings.ShowGrid;
                viewModel.SnapToGrid = file.Diagram.Settings.SnapToGrid;
                viewModel.GridSize = file.Diagram.Settings.GridSize;
                viewModel.ZoomLevel = file.Diagram.Settings.ZoomLevel;

                // Restore nodes
                foreach (var nodeData in file.Diagram.Nodes)
                {
                    var node = new CanvasNode
                    {
                        Id = nodeData.Id,
                        Text = nodeData.Text,
                        Shape = Enum.TryParse<NodeShape>(nodeData.Shape, out var shape) ? shape : NodeShape.Rectangle,
                        PositionX = nodeData.Position.X,
                        PositionY = nodeData.Position.Y,
                        SizeWidth = nodeData.Size.Width,
                        SizeHeight = nodeData.Size.Height
                    };

                    viewModel.Nodes.Add(node);
                }

                // Restore connectors
                foreach (var connectorData in file.Diagram.Connectors)
                {
                    var connector = new CanvasConnector
                    {
                        Id = connectorData.Id,
                        StartNodeId = connectorData.StartNodeId,
                        EndNodeId = connectorData.EndNodeId,
                        Label = connectorData.Label,
                        StartAnchor = connectorData.StartAnchor,
                        EndAnchor = connectorData.EndAnchor,
                        LineStyle = Enum.TryParse<LineStyle>(connectorData.LineStyle, out var lineStyle) ? lineStyle : LineStyle.Solid,
                        LineWidth = connectorData.LineWidth,
                        StartArrowType = Enum.TryParse<ArrowHeadType>(connectorData.StartArrowType, out var startArrow) ? startArrow : ArrowHeadType.None,
                        EndArrowType = Enum.TryParse<ArrowHeadType>(connectorData.EndArrowType, out var endArrow) ? endArrow : ArrowHeadType.Arrow,
                        LineColor = connectorData.LineColor
                    };

                    viewModel.Connectors.Add(connector);
                }

                System.Diagnostics.Debug.WriteLine($"[FILE SERVICE] Restored {file.Diagram.Nodes.Count} nodes and {file.Diagram.Connectors.Count} connectors");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FILE SERVICE] Error restoring diagram: {ex.Message}");
            }
        }

        /// <summary>
        /// Serialize current diagram state
        /// </summary>
        private DiagramData SerializeDiagram(DiagramCanvasViewModel viewModel)
        {
            var data = new DiagramData
            {
                Type = viewModel.DiagramType.ToString().ToLower(),
                Settings = new DiagramSettings
                {
                    ShowGrid = viewModel.ShowGrid,
                    SnapToGrid = viewModel.SnapToGrid,
                    GridSize = viewModel.GridSize,
                    ZoomLevel = viewModel.ZoomLevel
                }
            };

            // Serialize nodes
            foreach (var node in viewModel.Nodes)
            {
                data.Nodes.Add(new NodeData
                {
                    Id = node.Id,
                    Text = node.Text,
                    Shape = node.Shape.ToString(),
                    Position = new PositionData { X = node.PositionX, Y = node.PositionY },
                    Size = new SizeData { Width = node.SizeWidth, Height = node.SizeHeight },
                    Style = new NodeStyleData
                    {
                        FillColor = "#ffffff",
                        StrokeColor = "#000000",
                        StrokeWidth = 2
                    }
                });
            }

            // Serialize connectors
            foreach (var connector in viewModel.Connectors)
            {
                data.Connectors.Add(new ConnectorData
                {
                    Id = connector.Id,
                    StartNodeId = connector.StartNodeId,
                    EndNodeId = connector.EndNodeId,
                    Label = connector.Label,
                    StartAnchor = connector.StartAnchor,
                    EndAnchor = connector.EndAnchor,
                    LineStyle = connector.LineStyle.ToString(),
                    LineWidth = connector.LineWidth,
                    StartArrowType = connector.StartArrowType.ToString(),
                    EndArrowType = connector.EndArrowType.ToString(),
                    LineColor = connector.LineColor
                });
            }

            return data;
        }
    }
}
