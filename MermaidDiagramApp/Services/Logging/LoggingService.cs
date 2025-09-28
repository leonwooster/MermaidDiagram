using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MermaidDiagramApp.Services.Logging
{
    public sealed class LoggingService
    {
        public static LoggingService Instance { get; } = new LoggingService();

        private readonly object _syncRoot = new object();
        private readonly NullLogger _nullLogger = new NullLogger();

        private bool _initialized;
        private LoggingConfiguration _configuration = new LoggingConfiguration();
        private IReadOnlyList<ILogSink> _sinks = Array.Empty<ILogSink>();
        private LoggerTraceListener? _traceListener;

        private LoggingService()
        {
        }

        public LoggingConfiguration Configuration => _configuration;

        public ILogFileProvider? LogFileProvider => _sinks.OfType<ILogFileProvider>().FirstOrDefault();

        public void Initialize(LoggingConfiguration configuration, IEnumerable<ILogSink>? sinks = null)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            lock (_syncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                var sinkList = sinks?.ToList() ?? new List<ILogSink>();

                if (sinkList.Count == 0)
                {
                    var targetDirectory = !string.IsNullOrWhiteSpace(configuration.CustomLogDirectory)
                        ? configuration.CustomLogDirectory!
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), configuration.LogsFolderName);

                    Directory.CreateDirectory(targetDirectory);
                    sinkList.Add(new RollingFileLogSink(targetDirectory, configuration.LogFileName, configuration.FileSizeLimitBytes, configuration.MaxRetainedFiles));
                }

                _configuration = configuration;
                _sinks = sinkList.AsReadOnly();
                _initialized = true;

                AttachTraceListener();
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                throw new ArgumentException("Category name cannot be null or empty", nameof(categoryName));
            }

            if (!_initialized)
            {
                return _nullLogger;
            }

            return new Logger(categoryName, _configuration.MinimumLevel, _sinks);
        }

        public ILogger GetLogger<T>() => CreateLogger(typeof(T).FullName ?? typeof(T).Name);

        private void AttachTraceListener()
        {
            if (!_initialized)
            {
                return;
            }

            _traceListener = new LoggerTraceListener(CreateLogger("System.Diagnostics"));

            System.Diagnostics.Trace.Listeners.Add(_traceListener);
        }

        private sealed class NullLogger : ILogger
        {
            public void Log(LogLevel level, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
            {
                // Intentionally left blank
            }
        }

        private sealed class LoggerTraceListener : TraceListener
        {
            private readonly ILogger _logger;
            private bool _isWriting;

            public LoggerTraceListener(ILogger logger)
            {
                _logger = logger;
            }

            public override void Write(string? message)
            {
                WriteInternal(message);
            }

            public override void WriteLine(string? message)
            {
                WriteInternal(message);
            }

            private void WriteInternal(string? message)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                if (_isWriting)
                {
                    return;
                }

                try
                {
                    _isWriting = true;
                    _logger.Log(LogLevel.Debug, message);
                }
                finally
                {
                    _isWriting = false;
                }
            }
        }
    }
}
