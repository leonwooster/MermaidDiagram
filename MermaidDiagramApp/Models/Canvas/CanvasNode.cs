using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace MermaidDiagramApp.Models.Canvas
{
    /// <summary>
    /// Represents a visual node on the diagram canvas
    /// </summary>
    public class CanvasNode : INotifyPropertyChanged
    {
        private string _id;
        private Point _position;
        private Size _size;
        private string _text;
        private NodeShape _shape;
        private bool _isSelected;
        private string _fillColor;
        private string _borderColor;
        private double _borderWidth;
        private string _fontFamily;
        private double _fontSize;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public Point Position
        {
            get => _position;
            set
            {
                if (SetProperty(ref _position, value))
                {
                    OnPropertyChanged(nameof(PositionX));
                    OnPropertyChanged(nameof(PositionY));
                }
            }
        }

        public double PositionX
        {
            get => _position.X;
            set
            {
                if (_position.X != value)
                {
                    _position.X = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        public double PositionY
        {
            get => _position.Y;
            set
            {
                if (_position.Y != value)
                {
                    _position.Y = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        public Size Size
        {
            get => _size;
            set
            {
                if (SetProperty(ref _size, value))
                {
                    OnPropertyChanged(nameof(SizeWidth));
                    OnPropertyChanged(nameof(SizeHeight));
                }
            }
        }

        public double SizeWidth
        {
            get => _size.Width;
            set
            {
                if (_size.Width != value)
                {
                    _size.Width = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Size));
                }
            }
        }

        public double SizeHeight
        {
            get => _size.Height;
            set
            {
                if (_size.Height != value)
                {
                    _size.Height = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Size));
                }
            }
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

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string FillColor
        {
            get => _fillColor;
            set => SetProperty(ref _fillColor, value);
        }

        public string BorderColor
        {
            get => _borderColor;
            set => SetProperty(ref _borderColor, value);
        }

        public double BorderWidth
        {
            get => _borderWidth;
            set => SetProperty(ref _borderWidth, value);
        }

        public string FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public CanvasNode()
        {
            _id = $"node{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            _text = "Node";
            _shape = NodeShape.Rectangle;
            _position = new Point(0, 0);
            _size = new Size(120, 60);
            _isSelected = false;
            _fillColor = "#FFFFFF";
            _borderColor = "#000000";
            _borderWidth = 2;
            _fontFamily = "Segoe UI";
            _fontSize = 14;
        }

        public CanvasNode Clone()
        {
            return new CanvasNode
            {
                Id = $"node{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                Text = Text,
                Shape = Shape,
                Position = new Point(Position.X + 20, Position.Y + 20),
                Size = Size,
                FillColor = FillColor,
                BorderColor = BorderColor,
                BorderWidth = BorderWidth,
                FontFamily = FontFamily,
                FontSize = FontSize
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
