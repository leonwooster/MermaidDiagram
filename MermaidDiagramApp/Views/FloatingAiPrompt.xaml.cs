using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MermaidDiagramApp.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace MermaidDiagramApp.Views
{
    public sealed partial class FloatingAiPrompt : UserControl
    {
        private bool _isDragging;
        private Point _lastPoint;
        private Point _pressOffsetInSelf;
        private const double PopOutThreshold = 48; // px beyond edges triggers pop-out request

        public FloatingAiPrompt()
        {
            this.InitializeComponent();
        }

        public event EventHandler<string>? InsertRequested;
        public event EventHandler<string>? ImportToCanvasRequested;
        public event EventHandler? ConfigureRequested;
        public event EventHandler? PopOutRequested;

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AiDiagramGeneratorViewModel vm)
            {
                await vm.GenerateDiagramAsync();
            }
        }

        private async void SuggestTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AiDiagramGeneratorViewModel vm)
            {
                await vm.DetermineDiagramTypeAsync();
            }
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AiDiagramGeneratorViewModel vm)
            {
                InsertRequested?.Invoke(this, vm.GeneratedCode);
            }
        }

        private void ImportToCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AiDiagramGeneratorViewModel vm)
            {
                ImportToCanvasRequested?.Invoke(this, vm.GeneratedCode);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AiDiagramGeneratorViewModel vm)
            {
                try
                {
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(vm.GeneratedCode);
                    Clipboard.SetContent(dataPackage);
                    vm.StatusMessage = "Code copied to clipboard!";
                }
                catch (Exception ex)
                {
                    vm.StatusMessage = $"Failed to copy: {ex.Message}";
                }
            }
        }

        private void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigureRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RootBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = true;
            _lastPoint = e.GetCurrentPoint(this).Position;
            _pressOffsetInSelf = _lastPoint;
            (sender as UIElement)?.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void RootBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            (sender as UIElement)?.ReleasePointerCaptures();
            e.Handled = true;
        }

        private void RootBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging) return;
            var parentCanvas = this.Parent as Canvas;
            if (parentCanvas == null)
            {
                // Fallback to transform if not hosted in Canvas
                var current = e.GetCurrentPoint(this).Position;
                _lastPoint = current;
                return;
            }

            var posInCanvas = e.GetCurrentPoint(parentCanvas).Position;
            var desiredLeft = posInCanvas.X - _pressOffsetInSelf.X;
            var desiredTop = posInCanvas.Y - _pressOffsetInSelf.Y;

            // Clamp within canvas bounds
            var maxLeft = Math.Max(0, parentCanvas.ActualWidth - this.ActualWidth);
            var maxTop = Math.Max(0, parentCanvas.ActualHeight - this.ActualHeight);

            var clampedLeft = Math.Min(Math.Max(0, desiredLeft), maxLeft);
            var clampedTop = Math.Min(Math.Max(0, desiredTop), maxTop);

            Canvas.SetLeft(this, clampedLeft);
            Canvas.SetTop(this, clampedTop);

            // If the desired position is significantly beyond edges, request pop-out
            if (desiredLeft < -PopOutThreshold || desiredTop < -PopOutThreshold ||
                desiredLeft > maxLeft + PopOutThreshold || desiredTop > maxTop + PopOutThreshold)
            {
                PopOutRequested?.Invoke(this, EventArgs.Empty);
                _isDragging = false;
                (sender as UIElement)?.ReleasePointerCaptures();
            }

            e.Handled = true;
        }

        private void PopOutButton_Click(object sender, RoutedEventArgs e)
        {
            PopOutRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Switches the control UI to standalone mode when hosted in its own Window.
        /// Updates the Pop Out button to read "Dock".
        /// </summary>
        public void SetIsStandalone(bool isStandalone)
        {
            if (PopOutButton != null)
            {
                PopOutButton.Content = isStandalone ? "Dock" : "Pop out";
            }
        }
    }
}
