using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MermaidDiagramApp.Models.Canvas;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.ViewModels;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Service that encapsulates file operations by delegating to existing
    /// DiagramFileService, RecentFilesService, and MermaidTextOptimizer.
    /// </summary>
    public class FileOperationsService : IFileOperationsService
    {
        private readonly DiagramFileService _diagramFileService;
        private readonly RecentFilesService _recentFilesService;
        private readonly MermaidTextOptimizer _mermaidTextOptimizer;
        private readonly ILogger _logger;

        public FileOperationsService(
            DiagramFileService diagramFileService,
            RecentFilesService recentFilesService,
            MermaidTextOptimizer mermaidTextOptimizer,
            ILogger logger)
        {
            _diagramFileService = diagramFileService ?? throw new ArgumentNullException(nameof(diagramFileService));
            _recentFilesService = recentFilesService ?? throw new ArgumentNullException(nameof(recentFilesService));
            _mermaidTextOptimizer = mermaidTextOptimizer ?? throw new ArgumentNullException(nameof(mermaidTextOptimizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string?> ReadFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    _logger.LogWarning($"Cannot read file: path is empty or file does not exist: {filePath}");
                    return null;
                }

                var content = await File.ReadAllTextAsync(filePath);
                _logger.LogInformation($"File read successfully: {filePath}");
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading file: {filePath}", ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SaveFileAsync(string filePath, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be empty", nameof(filePath));
                }

                await File.WriteAllTextAsync(filePath, content);
                _logger.LogInformation($"File saved successfully: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving file: {filePath}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<DiagramBuilderFile?> LoadDiagramAsync(string filePath)
        {
            return _diagramFileService.LoadDiagramAsync(filePath);
        }

        /// <inheritdoc />
        public Task<bool> SaveDiagramAsync(string filePath, DiagramCanvasViewModel viewModel)
        {
            return _diagramFileService.SaveDiagramAsync(filePath, viewModel);
        }

        /// <inheritdoc />
        public string OptimizeMermaidContent(string content)
        {
            return _mermaidTextOptimizer.OptimizeDiagram(content);
        }

        /// <inheritdoc />
        public bool NeedsMermaidOptimization(string content)
        {
            return _mermaidTextOptimizer.NeedsOptimization(content);
        }

        /// <inheritdoc />
        public void AddRecentFile(string filePath)
        {
            _recentFilesService.AddRecentFile(filePath);
        }

        /// <inheritdoc />
        public IReadOnlyList<RecentFileEntry> GetRecentFiles()
        {
            return _recentFilesService.RecentFiles;
        }

        /// <inheritdoc />
        public void RemoveRecentFile(string filePath)
        {
            _recentFilesService.RemoveRecentFile(filePath);
        }

        /// <inheritdoc />
        public void ClearRecentFiles()
        {
            _recentFilesService.ClearRecentFiles();
        }

        /// <inheritdoc />
        public string GetWindowTitle(string? filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileName = Path.GetFileName(filePath);
                return $"{fileName} - Mermaid Diagram Editor";
            }

            return "Mermaid Diagram Editor";
        }
    }
}
 /// 