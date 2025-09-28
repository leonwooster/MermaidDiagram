using System;
using System.Collections.Generic;

namespace MermaidDiagramApp.Services.Logging
{
    public static class LoggerExtensions
    {
        public static void LogTrace(this ILogger logger, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
            => logger.Log(LogLevel.Trace, message, exception, state);

        public static void LogDebug(this ILogger logger, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
            => logger.Log(LogLevel.Debug, message, exception, state);

        public static void LogInformation(this ILogger logger, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
            => logger.Log(LogLevel.Information, message, exception, state);

        public static void LogWarning(this ILogger logger, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
            => logger.Log(LogLevel.Warning, message, exception, state);

        public static void LogError(this ILogger logger, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
            => logger.Log(LogLevel.Error, message, exception, state);

        public static void LogCritical(this ILogger logger, string message, Exception? exception = null, IReadOnlyDictionary<string, object?>? state = null)
            => logger.Log(LogLevel.Critical, message, exception, state);
    }
}
