using Microsoft.UI.Xaml.Controls;
using MermaidDiagramApp.ViewModels;

namespace MermaidDiagramApp.Views
{
    public sealed partial class PropertiesPanel : UserControl
    {
        public PropertiesPanelViewModel ViewModel { get; }

        public PropertiesPanel()
        {
            this.InitializeComponent();
            ViewModel = new PropertiesPanelViewModel();
        }
    }
}
