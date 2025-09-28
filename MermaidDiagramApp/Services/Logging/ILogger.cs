using System;
using System.Collections.Generic;

namespace MermaidDiagramApp.Services.Logging
{
    public interface ILogger
    {
        void Log(LogLevel level,
                 string message,
                 Exception? exception = null,
                 IReadOnlyDictionary<string, object?>? state = null);
    }
}
