using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.Logging
{
    public sealed class RollingFileLogSink : ILogSink, ILogFileProvider
    {
        private readonly string _logDirectory;
        private readonly string _baseFileName;
        private readonly long _fileSizeLimitBytes;
        private readonly int _maxRetainedFiles;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public RollingFileLogSink(string logDirectory,
                                  string baseFileName,
                                  long fileSizeLimitBytes,
                                  int maxRetainedFiles)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                throw new ArgumentException("Log directory cannot be null or empty", nameof(logDirectory));
            }

            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                throw new ArgumentException("Base file name cannot be null or empty", nameof(baseFileName));
            }

            if (fileSizeLimitBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileSizeLimitBytes));
            }

            if (maxRetainedFiles <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetainedFiles));
            }

            _logDirectory = logDirectory;
            _baseFileName = baseFileName;
            _fileSizeLimitBytes = fileSizeLimitBytes;
            _maxRetainedFiles = maxRetainedFiles;

            Directory.CreateDirectory(_logDirectory);
            CurrentLogFilePath = Path.Combine(_logDirectory, _baseFileName);
        }

        public string LogsDirectory => _logDirectory;

        public string CurrentLogFilePath { get; private set; }

        public async Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var payload = FormatEntry(entry);
            var payloadBytes = Encoding.UTF8.GetByteCount(payload);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(_logDirectory);

                await RotateIfNeededAsync(payloadBytes).ConfigureAwait(false);

                await File.AppendAllTextAsync(CurrentLogFilePath, payload, Encoding.UTF8, cancellationToken)
                          .ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static string FormatEntry(LogEntry entry)
        {
            var builder = new StringBuilder();
            builder.Append('[')
                   .Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
                   .Append("] [")
                   .Append(entry.Level)
                   .Append("] [")
                   .Append(entry.Category)
                   .Append("] ")
                   .Append(entry.Message);

            if (entry.State is { Count: > 0 })
            {
                builder.Append(" | ");
                builder.Append(string.Join(", ", entry.State.Select(kv => $"{kv.Key}={kv.Value}")));
            }

            if (entry.Exception is not null)
            {
                builder.AppendLine();
                builder.Append(entry.Exception);
            }

            builder.AppendLine();
            return builder.ToString();
        }

        private Task RotateIfNeededAsync(int payloadBytes)
        {
            var fileInfo = new FileInfo(CurrentLogFilePath);
            if (!fileInfo.Exists)
            {
                using var stream = File.Create(CurrentLogFilePath);
                return Task.CompletedTask;
            }

            if (fileInfo.Length + payloadBytes <= _fileSizeLimitBytes)
            {
                return Task.CompletedTask;
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
            var baseName = Path.GetFileNameWithoutExtension(_baseFileName);
            var extension = Path.GetExtension(_baseFileName);
            var archiveName = $"{baseName}-{timestamp}{extension}";
            var archivePath = Path.Combine(_logDirectory, archiveName);

            fileInfo.MoveTo(archivePath, overwrite: false);

            CurrentLogFilePath = Path.Combine(_logDirectory, _baseFileName);
            using (File.Create(CurrentLogFilePath))
            {
                // create empty file
            }

            TrimOldFiles();

            return Task.CompletedTask;
        }

        private void TrimOldFiles()
        {
            var files = Directory
                .EnumerateFiles(_logDirectory, Path.GetFileNameWithoutExtension(_baseFileName) + "-*" + Path.GetExtension(_baseFileName))
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.CreationTimeUtc)
                .ToList();

            if (files.Count <= _maxRetainedFiles)
            {
                return;
            }

            foreach (var file in files.Skip(_maxRetainedFiles))
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // Swallow clean-up errors to avoid impacting logging pipeline
                }
            }
        }
    }
}
