using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MermaidDiagramApp.Models.Canvas;

namespace MermaidDiagramApp.ViewModels
{
    /// <summary>
    /// ViewModel for the properties panel
    /// </summary>
    public class PropertiesPanelViewModel : INotifyPropertyChanged
    {
        private object? _selectedElement;
        private bool _hasSelection;

        public object? SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (SetProperty(ref _selectedElement, value))
                {
                    HasSelection = value != null;
                    OnPropertyChanged(nameof(SelectedNode));
                    OnPropertyChanged(nameof(SelectedConnector));
                    OnPropertyChanged(nameof(IsNodeSelected));
                    OnPropertyChanged(nameof(IsConnectorSelected));
                }
            }
        }

        public bool HasSelection
        {
            get => _hasSelection;
            private set => SetProperty(ref _hasSelection, value);
        }

        public CanvasNode? SelectedNode => SelectedElement as CanvasNode;
        public CanvasConnector? SelectedConnector => SelectedElement as CanvasConnector;

        public bool IsNodeSelected => SelectedNode != null;
        public bool IsConnectorSelected => SelectedConnector != null;

        public PropertiesPanelViewModel()
        {
            _selectedElement = null;
            _hasSelection = false;
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
