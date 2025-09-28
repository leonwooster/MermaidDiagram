namespace MermaidDiagramApp.Services.Logging
{
    public interface ILogFileProvider
    {
        string LogsDirectory { get; }

        string CurrentLogFilePath { get; }
    }
}
