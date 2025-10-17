using System;
using System.Collections.Generic;

namespace MermaidDiagramApp.Models.Canvas
{
    /// <summary>
    /// Represents the complete file format for Diagram Builder files (.mmdx)
    /// </summary>
    public class DiagramBuilderFile
    {
        public string Version { get; set; } = "1.0";
        public FileMetadata Metadata { get; set; } = new FileMetadata();
        public DiagramData Diagram { get; set; } = new DiagramData();
        public string MermaidCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// File metadata information
    /// </summary>
    public class FileMetadata
    {
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Modified { get; set; } = DateTime.Now;
        public string Author { get; set; } = string.Empty;
        public string Application { get; set; } = "Mermaid Diagram Builder";
    }

    /// <summary>
    /// Complete diagram data including nodes, connectors, and settings
    /// </summary>
    public class DiagramData
    {
        public string Type { get; set; } = "flowchart";
        public DiagramSettings Settings { get; set; } = new DiagramSettings();
        public List<NodeData> Nodes { get; set; } = new List<NodeData>();
        public List<ConnectorData> Connectors { get; set; } = new List<ConnectorData>();
    }

    /// <summary>
    /// Diagram canvas settings
    /// </summary>
    public class DiagramSettings
    {
        public bool ShowGrid { get; set; } = true;
        public bool SnapToGrid { get; set; } = true;
        public double GridSize { get; set; } = 20;
        public double ZoomLevel { get; set; } = 1.0;
    }

    /// <summary>
    /// Serializable node data
    /// </summary>
    public class NodeData
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Shape { get; set; } = "Rectangle";
        public PositionData Position { get; set; } = new PositionData();
        public SizeData Size { get; set; } = new SizeData();
        public NodeStyleData Style { get; set; } = new NodeStyleData();
    }

    /// <summary>
    /// Position data
    /// </summary>
    public class PositionData
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Size data
    /// </summary>
    public class SizeData
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// Node styling data
    /// </summary>
    public class NodeStyleData
    {
        public string FillColor { get; set; } = "#ffffff";
        public string StrokeColor { get; set; } = "#000000";
        public double StrokeWidth { get; set; } = 2;
    }

    /// <summary>
    /// Serializable connector data
    /// </summary>
    public class ConnectorData
    {
        public string Id { get; set; } = string.Empty;
        public string StartNodeId { get; set; } = string.Empty;
        public string EndNodeId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string StartAnchor { get; set; } = "center";
        public string EndAnchor { get; set; } = "center";
        public string LineStyle { get; set; } = "Solid";
        public double LineWidth { get; set; } = 2;
        public string StartArrowType { get; set; } = "None";
        public string EndArrowType { get; set; } = "Arrow";
        public string LineColor { get; set; } = "#000000";
    }
}
