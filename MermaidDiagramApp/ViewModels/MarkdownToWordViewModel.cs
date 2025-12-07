using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MermaidDiagramApp.Commands;
using MermaidDiagramApp.Services.Export;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Markdown to Word export workflow.
    /// </summary>
    public class MarkdownToWordViewModel : INotifyPropertyChanged
    {
        private readonly MarkdownToWordExportService _exportService;
        private readonly ILogger _logger;
        private CancellationTokenSource? _cancellationTokenSource;

        private string? _markdownFilePath;
        private string? _outputPath;
        private bool _isExporting;
        private int _progressPercentage;
        private string _progressMessage = string.Empty;
        private string? _markdownContent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MarkdownToWordViewModel(MarkdownToWordExportService exportService, ILogger logger)
        {
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            OpenMarkdownFileCommand = new RelayCommand(
                execute: async _ => await OpenMarkdownFileAsync(),
                canExecute: _ => !IsExporting);

            ExportToWordCommand = new RelayCommand(
                execute: async _ => await ExportToWordAsync(),
                canExecute: _ => CanExport);

            CancelExportCommand = new RelayCommand(
                execute: _ => CancelExport(),
                canExecute: _ => IsExporting);
        }

        #region Properties

        /// <summary>
        /// Gets or sets the path to the loaded Markdown file.
        /// </summary>
        public string? MarkdownFilePath
        {
            get => _markdownFilePath;
            set
            {
                if (_markdownFilePath != value)
                {
                    _markdownFilePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanExport));
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the output path for the Word document.
        /// </summary>
        public string? OutputPath
        {
            get => _outputPath;
            set
            {
                if (_outputPath != value)
                {
                    _outputPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether an export operation is currently in progress.
        /// </summary>
        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                if (_isExporting != value)
                {
                    _isExporting = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanExport));
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current export progress percentage (0-100).
        /// </summary>
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                if (_progressPercentage != value)
                {
                    _progressPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current progress message.
        /// </summary>
        public string ProgressMessage
        {
            get => _progressMessage;
            set
            {
                if (_progressMessage != value)
                {
                    _progressMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets whether the export command can be executed.
        /// </summary>
        public bool CanExport => !string.IsNullOrWhiteSpace(MarkdownFilePath) && 
                                 !string.IsNullOrWhiteSpace(_markdownContent) && 
                                 !IsExporting;

        #endregion

        #region Commands

        /// <summary>
        /// Command to open a Markdown file.
        /// </summary>
        public ICommand OpenMarkdownFileCommand { get; }

        /// <summary>
        /// Command to export the loaded Markdown file to Word.
        /// </summary>
        public ICommand ExportToWordCommand { get; }

        /// <summary>
        /// Command to cancel the current export operation.
        /// </summary>
        public ICommand CancelExportCommand { get; }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Opens and loads a Markdown file.
        /// </summary>
        public async Task OpenMarkdownFileAsync()
        {
            try
            {
                // In a real implementation, this would show a file dialog
                // For now, this is a placeholder that can be called with a file path
                if (string.IsNullOrWhiteSpace(MarkdownFilePath))
                {
                    _logger.Log(LogLevel.Warning, "No file path specified for opening");
                    return;
                }

                _logger.Log(LogLevel.Information, $"Loading Markdown file: {MarkdownFilePath}");

                // Validate UTF-8 encoding by attempting to read the file
                try
                {
                    // Read file content with UTF-8 encoding
                    _markdownContent = await File.ReadAllTextAsync(MarkdownFilePath, System.Text.Encoding.UTF8);
                    
                    // Additional validation: check if the content is valid UTF-8
                    // by attempting to encode it back to bytes
                    var bytes = System.Text.Encoding.UTF8.GetBytes(_markdownContent);
                    var decoded = System.Text.Encoding.UTF8.GetString(bytes);
                    
                    if (decoded != _markdownContent)
                    {
                        throw new InvalidDataException("File contains invalid UTF-8 encoding");
                    }
                }
                catch (DecoderFallbackException ex)
                {
                    _logger.Log(LogLevel.Error, "File contains invalid UTF-8 encoding", ex);
                    throw new InvalidDataException("The file contains invalid UTF-8 encoding. Please ensure the file is saved as UTF-8.", ex);
                }

                _logger.Log(LogLevel.Information, "Markdown file loaded successfully");

                // Notify that CanExport may have changed
                OnPropertyChanged(nameof(CanExport));
                RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Failed to load Markdown file", ex);
                _markdownContent = null;
                OnPropertyChanged(nameof(CanExport));
                RaiseCanExecuteChanged();
                throw;
            }
        }

        /// <summary>
        /// Exports the loaded Markdown file to Word format.
        /// </summary>
        public async Task ExportToWordAsync()
        {
            if (!CanExport)
            {
                return;
            }

            try
            {
                IsExporting = true;
                _cancellationTokenSource = new CancellationTokenSource();

                _logger.Log(LogLevel.Information, $"Starting export to {OutputPath}");

                // Create progress reporter
                var progress = new Progress<ExportProgress>(p =>
                {
                    ProgressPercentage = p.PercentComplete;
                    ProgressMessage = p.CurrentOperation;
                });

                // Perform export
                var result = await _exportService.ExportToWordAsync(
                    _markdownContent!,
                    MarkdownFilePath!,
                    OutputPath!,
                    progress,
                    _cancellationTokenSource.Token);

                if (result.Success)
                {
                    _logger.Log(LogLevel.Information, 
                        $"Export completed successfully in {result.Duration.TotalSeconds:F2} seconds");
                }
                else
                {
                    _logger.Log(LogLevel.Error, $"Export failed: {result.ErrorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log(LogLevel.Information, "Export cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Export failed with exception", ex);
                throw;
            }
            finally
            {
                IsExporting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Cancels the current export operation.
        /// </summary>
        public void CancelExport()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _logger.Log(LogLevel.Information, "Cancelling export operation");
                _cancellationTokenSource.Cancel();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Loads a Markdown file from the specified path.
        /// </summary>
        /// <param name="filePath">The path to the Markdown file.</param>
        public async Task LoadMarkdownFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty", nameof(filePath));
            }

            MarkdownFilePath = filePath;
            await OpenMarkdownFileAsync();
        }

        /// <summary>
        /// Sets the output path for the Word document.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        public void SetOutputPath(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be empty", nameof(outputPath));
            }

            OutputPath = outputPath;
        }

        private void RaiseCanExecuteChanged()
        {
            (OpenMarkdownFileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ExportToWordCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelExportCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
