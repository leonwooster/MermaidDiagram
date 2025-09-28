using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using MermaidDiagramApp.Services.Logging;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MermaidDiagramApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            InitializeLogging();
            _logger = LoggingService.Instance.GetLogger<App>();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _logger.Log(LogLevel.Information, "Application launching");
            _window = new MainWindow();
            _window.Activate();
            _logger.Log(LogLevel.Information, "Main window activated");
        }

        private void InitializeLogging()
        {
            var config = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Debug,
                CustomLogDirectory = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "Logs"),
                LogFileName = "app.log",
                FileSizeLimitBytes = 2 * 1024 * 1024,
                MaxRetainedFiles = 5
            };

            LoggingService.Instance.Initialize(config);
        }
    }
}
