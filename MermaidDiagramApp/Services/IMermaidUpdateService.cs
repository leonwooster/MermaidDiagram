using System.Threading.Tasks;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Encapsulates Mermaid.js version checking, downloading, and installation.
/// </summary>
public interface IMermaidUpdateService
{
    /// <summary>
    /// Checks the npm registry for a newer version of Mermaid.js.
    /// Returns UpdateAvailable=false on any network or parsing failure.
    /// </summary>
    Task<MermaidVersionInfo> CheckForUpdatesAsync();

    /// <summary>
    /// Downloads the specified Mermaid.js version from the CDN and installs it
    /// to the local application data folder.
    /// </summary>
    /// <param name="version">The version string to download (e.g. "11.4.0").</param>
    /// <returns>True if the update was installed successfully; false otherwise.</returns>
    Task<bool> DownloadAndInstallUpdateAsync(string version);

    /// <summary>
    /// Returns the currently installed Mermaid.js version string.
    /// Falls back to "10.9.0" if the version file cannot be read.
    /// </summary>
    string GetCurrentVersion();
}
