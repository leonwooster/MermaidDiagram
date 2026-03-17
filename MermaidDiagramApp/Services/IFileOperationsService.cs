using System.Collections.Generic;
using System.Threading.Tasks;
using MermaidDiagramApp.Models.Canvas;
using MermaidDiagramApp.ViewModels;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Service interface for file operations including reading, saving, recent files management,
    /// and Mermaid content optimization.
    /// </summary>
    public interface IFileOperationsService
    {
        /// <summary>
        /// Reads the content of a file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the file to read.</param>
        /// <returns>The file content, or null if the file could not be read.</returns>
        Task<string?> ReadFileAsync(string filePath);

        /// <summary>
        /// Saves content to a file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to save the file to.</param>
        /// <param name="content">The content to write.</param>
        Task SaveFileAsync(string filePath, string content);

        /// <summary>
        /// Loads a diagram builder file (.mmdx) asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the diagram file.</param>
        /// <returns>The loaded diagram file, or null if loading failed.</returns>
        Task<DiagramBuilderFile?> LoadDiagramAsync(string filePath);

        /// <summary>
        /// Saves a diagram to a .mmdx file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to save the diagram to.</param>
        /// <param name="viewModel">The canvas view model containing the diagram data.</param>
        /// <returns>True if save was successful, false otherwise.</returns>
        Task<bool> SaveDiagramAsync(string filePath, DiagramCanvasViewModel viewModel);

        /// <summary>
        /// Optimizes Mermaid content by adding line breaks to long labels.
        /// </summary>
        /// <param name="content">The Mermaid content to optimize.</param>
        /// <returns>The optimized content.</returns>
        string OptimizeMermaidContent(string content);

        /// <summary>
        /// Checks if Mermaid content would benefit from optimization.
        /// </summary>
        /// <param name="content">The Mermaid content to check.</param>
        /// <returns>True if optimization is recommended, false otherwise.</returns>
        bool NeedsMermaidOptimization(string content);

        /// <summary>
        /// Adds a file to the recent files list.
        /// </summary>
        /// <param name="filePath">The path of the file to add.</param>
        void AddRecentFile(string filePath);

        /// <summary>
        /// Gets the list of recent files.
        /// </summary>
        /// <returns>A read-only list of recent file entries.</returns>
        IReadOnlyList<RecentFileEntry> GetRecentFiles();

        /// <summary>
        /// Removes a file from the recent files list.
        /// </summary>
        /// <param name="filePath">The path of the file to remove.</param>
        void RemoveRecentFile(string filePath);

        /// <summary>
        /// Clears all recent files.
        /// </summary>
        void ClearRecentFiles();

        /// <summary>
        /// Gets the window title based on the current file path.
        /// </summary>
        /// <param name="filePath">The current file path, or null if no file is open.</param>
        /// <returns>The formatted window title.</returns>
        string GetWindowTitle(string? filePath);
    }
}
