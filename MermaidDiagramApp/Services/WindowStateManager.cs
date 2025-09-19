using Microsoft.UI.Windowing;
using System;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace MermaidDiagramApp.Services
{
    public class WindowState
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMaximized { get; set; }
    }

    public static class WindowStateManager
    {
        private const string SettingsFileName = "window-state.json";

        public static async void SaveWindowState(AppWindow appWindow)
        {
            try
            {
                var windowState = new WindowState
                {
                    X = appWindow.Position.X,
                    Y = appWindow.Position.Y,
                    Width = appWindow.Size.Width,
                    Height = appWindow.Size.Height,
                    IsMaximized = appWindow.Presenter.Kind == AppWindowPresenterKind.Overlapped && 
                                 ((OverlappedPresenter)appWindow.Presenter).State == OverlappedPresenterState.Maximized
                };

                var json = JsonSerializer.Serialize(windowState, new JsonSerializerOptions { WriteIndented = true });
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync(SettingsFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save window state: {ex.Message}");
            }
        }

        public static async System.Threading.Tasks.Task<WindowState?> LoadWindowStateAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.GetFileAsync(SettingsFileName);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<WindowState>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load window state: {ex.Message}");
                return null;
            }
        }
    }
}
