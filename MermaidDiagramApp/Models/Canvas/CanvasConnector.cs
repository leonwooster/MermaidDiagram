using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace MermaidDiagramApp.Models.Canvas
{
    public enum LineStyle
    {
        Solid,
        Dashed,
        Dotted,
        Thick
    }

    public enum ArrowHeadType
    {
        None,
        Arrow,
        OpenArrow,
        Diamond,
        Circle,
        Cross
    }

    /// <summary>
    /// Represents a connection between two nodes on the canvas
    /// </summary>
    public class CanvasConnector : INotifyPropertyChanged
    {
        private string _id;
        private string _startNodeId;
        private string _endNodeId;
        private string _label;
        private LineStyle _lineStyle;
        private ArrowHeadType _startArrowType;
        private ArrowHeadType _endArrowType;
        private bool _isSelected;
        private string _lineColor;
        private double _lineWidth;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string StartNodeId
        {
            get => _startNodeId;
            set => SetProperty(ref _startNodeId, value);
        }

        public string EndNodeId
        {
            get => _endNodeId;
            set => SetProperty(ref _endNodeId, value);
        }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public LineStyle LineStyle
        {
            get => _lineStyle;
            set => SetProperty(ref _lineStyle, value);
        }

        public ArrowHeadType StartArrowType
        {
            get => _startArrowType;
            set => SetProperty(ref _startArrowType, value);
        }

        public ArrowHeadType EndArrowType
        {
            get => _endArrowType;
            set => SetProperty(ref _endArrowType, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string LineColor
        {
            get => _lineColor;
            set => SetProperty(ref _lineColor, value);
        }

        public double LineWidth
        {
            get => _lineWidth;
            set => SetProperty(ref _lineWidth, value);
        }

        public CanvasConnector()
        {
            _id = $"conn{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            _startNodeId = string.Empty;
            _endNodeId = string.Empty;
            _label = string.Empty;
            _lineStyle = LineStyle.Solid;
            _startArrowType = ArrowHeadType.None;
            _endArrowType = ArrowHeadType.Arrow;
            _isSelected = false;
            _lineColor = "#000000";
            _lineWidth = 2;
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
