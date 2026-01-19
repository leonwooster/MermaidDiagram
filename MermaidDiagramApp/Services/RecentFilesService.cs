using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.Storage;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Service for managing recently opened files
    /// </summary>
    public class RecentFilesService
    {
        private const int MaxRecentFiles = 100;
        private const string RecentFilesFileName = "recent-files.json";
        private readonly List<RecentFileEntry> _recentFiles = new();
        private readonly string _settingsFilePath;

        public RecentFilesService()
        {
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _settingsFilePath = Path.Combine(localFolder, RecentFilesFileName);
            LoadRecentFiles();
        }

        /// <summary>
        /// Gets the list of recent files (most recent first)
        /// </summary>
        public IReadOnlyList<RecentFileEntry> RecentFiles => _recentFiles.AsReadOnly();

        /// <summary>
        /// Adds a file to the recent files list
        /// </summary>
        public void AddRecentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            // Normalize path
            filePath = Path.GetFullPath(filePath);

            // Check if file exists
            if (!File.Exists(filePath))
                return;

            // Remove if already exists (to move to top)
            _recentFiles.RemoveAll(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            // Add to beginning of list
            _recentFiles.Insert(0, new RecentFileEntry
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                LastOpened = DateTime.Now
            });

            // Trim to max size
            if (_recentFiles.Count > MaxRecentFiles)
            {
                _recentFiles.RemoveRange(MaxRecentFiles, _recentFiles.Count - MaxRecentFiles);
            }

            SaveRecentFiles();
        }

        /// <summary>
        /// Removes a file from the recent files list
        /// </summary>
        public void RemoveRecentFile(string filePath)
        {
            _recentFiles.RemoveAll(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            SaveRecentFiles();
        }

        /// <summary>
        /// Clears all recent files
        /// </summary>
        public void ClearRecentFiles()
        {
            _recentFiles.Clear();
            SaveRecentFiles();
        }

        /// <summary>
        /// Cleans up recent files list by removing non-existent files
        /// </summary>
        public void CleanupRecentFiles()
        {
            _recentFiles.RemoveAll(f => !File.Exists(f.FilePath));
            SaveRecentFiles();
        }

        private void LoadRecentFiles()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var entries = JsonSerializer.Deserialize<List<RecentFileEntry>>(json);
                    if (entries != null)
                    {
                        _recentFiles.Clear();
                        _recentFiles.AddRange(entries.Where(e => File.Exists(e.FilePath)).Take(MaxRecentFiles));
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error loading recent files: {ex.Message}");
            }
        }

        private void SaveRecentFiles()
        {
            try
            {
                var json = JsonSerializer.Serialize(_recentFiles, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error saving recent files: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Represents a recent file entry
    /// </summary>
    public class RecentFileEntry
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; }
    }
}
