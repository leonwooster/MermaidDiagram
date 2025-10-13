using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using MermaidDiagramApp.ViewModels;
using MermaidDiagramApp.Models.Canvas;

namespace MermaidDiagramApp.Views
{
    public sealed partial class ShapeToolbox : UserControl
    {
        public ShapeToolboxViewModel ViewModel { get; }
        public DiagramCanvasViewModel? CanvasViewModel { get; set; }

        public ShapeToolbox()
        {
            this.InitializeComponent();
            ViewModel = new ShapeToolboxViewModel();
        }

        public void WireUpCanvasViewModel(DiagramCanvasViewModel canvasViewModel)
        {
            CanvasViewModel = canvasViewModel;
            
            // Update code view when canvas changes
            CanvasViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(CanvasViewModel.GeneratedMermaidCode))
                {
                    UpdateCodeView();
                }
            };
            
            UpdateCodeView();
        }

        private void UpdateCodeView()
        {
            if (CanvasViewModel != null && GeneratedCodeTextBox != null)
            {
                GeneratedCodeTextBox.Text = CanvasViewModel.GeneratedMermaidCode;
            }
        }

        private void Shape_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            if (sender is FrameworkElement element && element.Tag is ShapeTemplate template)
            {
                args.Data.Properties.Add("ShapeTemplate", template);
                args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
        }

        private void CopyCode_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(GeneratedCodeTextBox.Text);
            Clipboard.SetContent(dataPackage);
        }

        private void ApplyToEditor_Click(object sender, RoutedEventArgs e)
        {
            // This will be handled by MainWindow
            ApplyCodeRequested?.Invoke(this, GeneratedCodeTextBox.Text);
        }

        public event EventHandler<string>? ApplyCodeRequested;
    }
}
