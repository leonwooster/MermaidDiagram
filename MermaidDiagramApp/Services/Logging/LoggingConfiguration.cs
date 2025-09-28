namespace MermaidDiagramApp.Services.Logging
{
    public sealed class LoggingConfiguration
    {
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        public string LogsFolderName { get; set; } = "Logs";

        public string? CustomLogDirectory { get; set; }

        public string LogFileName { get; set; } = "app.log";

        public long FileSizeLimitBytes { get; set; } = 2 * 1024 * 1024; // 2 MB

        public int MaxRetainedFiles { get; set; } = 5;
    }
}
