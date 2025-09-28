using System;
using System.Collections.Generic;

namespace MermaidDiagramApp.Services.Logging
{
    public sealed class LogEntry
    {
        public LogEntry(DateTimeOffset timestamp,
                        LogLevel level,
                        string category,
                        string message,
                        Exception? exception = null,
                        IReadOnlyDictionary<string, object?>? state = null)
        {
            Timestamp = timestamp;
            Level = level;
            Category = category;
            Message = message;
            Exception = exception;
            State = state;
        }

        public DateTimeOffset Timestamp { get; }

        public LogLevel Level { get; }

        public string Category { get; }

        public string Message { get; }

        public Exception? Exception { get; }

        public IReadOnlyDictionary<string, object?>? State { get; }
    }
}
