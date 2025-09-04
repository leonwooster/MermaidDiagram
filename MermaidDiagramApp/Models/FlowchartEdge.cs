using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MermaidDiagramApp.Models
{
    public enum ArrowType
    {
        Arrow,      // -->
        Open,       // ---
        Dotted,     // -.->
        Thick,      // ==>
    }

    public class FlowchartEdge : INotifyPropertyChanged
    {
        private string _startNodeId;
        private string _endNodeId;
        private string _text;
        private ArrowType _arrowType;

        public string StartNodeId
        {
            get => _startNodeId;
            set
            {
                if (SetProperty(ref _startNodeId, value))
                {
                    OnPropertyChanged(nameof(Label));
                }
            }
        }

        public string EndNodeId
        {
            get => _endNodeId;
            set
            {
                if (SetProperty(ref _endNodeId, value))
                {
                    OnPropertyChanged(nameof(Label));
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (SetProperty(ref _text, value))
                {
                    OnPropertyChanged(nameof(Label));
                }
            }
        }

        public ArrowType ArrowType
        {
            get => _arrowType;
            set => SetProperty(ref _arrowType, value);
        }

        public string Label => string.IsNullOrWhiteSpace(Text) ? $"{StartNodeId} -> {EndNodeId}" : $"{StartNodeId} -- {Text} --> {EndNodeId}";

        public FlowchartEdge()
        {
            _startNodeId = string.Empty;
            _endNodeId = string.Empty;
            _text = string.Empty;
            _arrowType = ArrowType.Arrow;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static IEnumerable<string> GetArrowTypeNames()
        {
            return Enum.GetNames(typeof(ArrowType));
        }
    }
}
