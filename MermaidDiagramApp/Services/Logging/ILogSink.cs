using System.Threading;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.Logging
{
    public interface ILogSink
    {
        Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);
    }
}
