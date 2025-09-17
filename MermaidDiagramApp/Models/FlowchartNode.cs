using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MermaidDiagramApp.Models
{
    public enum NodeShape
    {
        Rectangle,      // id[text]
        RoundEdges,     // id(text)
        Stadium,        // id([text])
        Subroutine,     // id[[text]]
        Cylindrical,    // id[(text)]
        Circle,         // id((text))
        Asymmetric,     // id>text]
        Rhombus,        // id{text}
        Hexagon,        // id{{text}}
        Parallelogram,  // id[/text/]
        ParallelogramAlt,// id[\text\]
        Trapezoid,      // id[/\text/\]
        TrapezoidAlt    // id[\/text/\]
    }

    public class FlowchartNode : INotifyPropertyChanged
    {
        private string _id;
        private string _text;
        private NodeShape _shape;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public NodeShape Shape
        {
            get => _shape;
            set => SetProperty(ref _shape, value);
        }

        public FlowchartNode()
        {
            _id = $"node{new Random().Next(1000)}";
            _text = "Node";
            _shape = NodeShape.Rectangle;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public static IEnumerable<string> GetShapeNames()
        {
            return Enum.GetNames(typeof(NodeShape));
        }
    }
}
