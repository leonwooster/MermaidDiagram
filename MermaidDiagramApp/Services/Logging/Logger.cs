using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.Logging
{
    internal sealed class Logger : ILogger
    {
        private readonly string _category;
        private readonly LogLevel _minimumLevel;
        private readonly IReadOnlyList<ILogSink> _sinks;

        public Logger(string category, LogLevel minimumLevel, IReadOnlyList<ILogSink> sinks)
        {
            _category = category;
            _minimumLevel = minimumLevel;
            _sinks = sinks;
        }

        public void Log(LogLevel level, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
        {
            if (level < _minimumLevel)
            {
                return;
            }

            var entry = new LogEntry(DateTimeOffset.Now, level, _category, message, exception, state);

            foreach (var sink in _sinks)
            {
                try
                {
                    var task = sink.WriteAsync(entry);
                    _ = task.ContinueWith(static t =>
                    {
                        if (t.Exception != null)
                        {
                            // Swallow sink exceptions to protect callers; failures will surface in debugger
                            System.Diagnostics.Debug.WriteLine($"Log sink error: {t.Exception}");
                        }
                    }, TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to write log entry: {ex}");
                }
            }
        }
    }
}
