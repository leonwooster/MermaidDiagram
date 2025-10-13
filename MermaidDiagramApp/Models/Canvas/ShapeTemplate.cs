using Windows.Foundation;

namespace MermaidDiagramApp.Models.Canvas
{
    /// <summary>
    /// Represents a template for creating shapes from the toolbox
    /// </summary>
    public class ShapeTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public NodeShape ShapeType { get; set; }
        public Size DefaultSize { get; set; }
        public string DefaultText { get; set; }
        public string IconGlyph { get; set; }
        public string MermaidSyntaxPattern { get; set; }

        public ShapeTemplate()
        {
            Id = string.Empty;
            Name = string.Empty;
            Description = string.Empty;
            Category = "General";
            ShapeType = NodeShape.Rectangle;
            DefaultSize = new Size(120, 60);
            DefaultText = "Node";
            IconGlyph = "\uE8A5"; // Default icon
            MermaidSyntaxPattern = "[{text}]";
        }

        public CanvasNode CreateNode(Point position)
        {
            return new CanvasNode
            {
                Text = DefaultText,
                Shape = ShapeType,
                Position = position,
                Size = DefaultSize
            };
        }
    }
}
