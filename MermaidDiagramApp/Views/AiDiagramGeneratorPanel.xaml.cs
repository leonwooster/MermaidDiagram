using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MermaidDiagramApp.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace MermaidDiagramApp.Views
{
    /// <summary>
    /// Panel for generating diagrams using AI
    /// </summary>
    public sealed partial class AiDiagramGeneratorPanel : UserControl
    {
        public AiDiagramGeneratorViewModel? ViewModel { get; }

        public AiDiagramGeneratorPanel()
        {
            this.InitializeComponent();
        }

        public AiDiagramGeneratorPanel(AiDiagramGeneratorViewModel viewModel) : this()
        {
            ViewModel = viewModel;
            this.DataContext = ViewModel;
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.GenerateDiagramAsync();
            }
        }

        private async void SuggestTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.DetermineDiagramTypeAsync();
            }
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            // This will be handled by the parent window
            if (ViewModel != null)
            {
                InsertRequested?.Invoke(this, ViewModel.GeneratedCode);
            }
        }

        private void ImportToCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            // This will be handled by the parent window
            if (ViewModel != null)
            {
                ImportToCanvasRequested?.Invoke(this, ViewModel.GeneratedCode);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(ViewModel.GeneratedCode);
                Clipboard.SetContent(dataPackage);
                ViewModel.StatusMessage = "Code copied to clipboard!";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Failed to copy: {ex.Message}";
            }
        }

        /// <summary>
        /// Event raised when user wants to insert generated code into the main editor
        /// </summary>
        public event EventHandler<string>? InsertRequested;

        /// <summary>
        /// Event raised when user wants to import generated code into the visual canvas
        /// </summary>
        public event EventHandler<string>? ImportToCanvasRequested;
    }
}
