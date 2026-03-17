using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Services.Logging;
using Windows.Storage;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Manages Mermaid.js version checking, downloading, and installation.
/// Extracted from MainWindow update logic.
/// </summary>
public class MermaidUpdateService : IMermaidUpdateService
{
    private const string DefaultVersion = "10.9.0";
    private const string NpmRegistryUrl = "https://registry.npmjs.org/mermaid";
    private const string CdnUrlTemplate = "https://cdn.jsdelivr.net/npm/mermaid@{0}/dist/mermaid.min.js";
    private const string MermaidFolderName = "Mermaid";
    private const string VersionFileName = "mermaid-version.txt";
    private const string MermaidJsFileName = "mermaid.min.js";

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public MermaidUpdateService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// Constructor that accepts an HttpClient for testability.
    /// </summary>
    internal MermaidUpdateService(ILogger logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string GetCurrentVersion()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var versionFilePath = Path.Combine(localFolder.Path, MermaidFolderName, VersionFileName);

            if (File.Exists(versionFilePath))
            {
                var version = File.ReadAllText(versionFilePath).Trim();
                if (!string.IsNullOrEmpty(version))
                {
                    _logger.LogInformation($"Current Mermaid.js version from file: {version}");
                    return version;
                }
            }

            _logger.LogWarning("Version file not found or empty, returning default version");
            return DefaultVersion;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to read current Mermaid.js version: {ex.Message}", ex);
            return DefaultVersion;
        }
    }

    public async Task<MermaidVersionInfo> CheckForUpdatesAsync()
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            var response = await _httpClient.GetStringAsync(NpmRegistryUrl);

            if (string.IsNullOrEmpty(response))
            {
                _logger.LogWarning("Received empty response from npm registry");
                return new MermaidVersionInfo(currentVersion, currentVersion, false);
            }

            string latestVersionStr;
            using (var jsonDoc = JsonDocument.Parse(response))
            {
                if (!jsonDoc.RootElement.TryGetProperty("dist-tags", out var distTags) ||
                    !distTags.TryGetProperty("latest", out var latestVersion))
                {
                    _logger.LogWarning("Could not find latest version in npm registry response");
                    return new MermaidVersionInfo(currentVersion, currentVersion, false);
                }

                latestVersionStr = latestVersion.GetString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(latestVersionStr))
            {
                _logger.LogWarning("Latest version string is null or empty");
                return new MermaidVersionInfo(currentVersion, currentVersion, false);
            }

            _logger.LogInformation($"Latest Mermaid.js version from npm: {latestVersionStr}, Current version: {currentVersion}");

            var updateAvailable = CompareVersions(currentVersion, latestVersionStr);
            return new MermaidVersionInfo(currentVersion, latestVersionStr, updateAvailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP error checking for Mermaid.js updates: {ex.Message}", ex);
            return new MermaidVersionInfo(currentVersion, currentVersion, false);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError($"Timeout checking for Mermaid.js updates: {ex.Message}", ex);
            return new MermaidVersionInfo(currentVersion, currentVersion, false);
        }
        catch (JsonException ex)
        {
            _logger.LogError($"JSON parsing error checking for Mermaid.js updates: {ex.Message}", ex);
            return new MermaidVersionInfo(currentVersion, currentVersion, false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error checking for Mermaid.js updates: {ex.Message}", ex);
            return new MermaidVersionInfo(currentVersion, currentVersion, false);
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            _logger.LogError("Cannot download update: version string is null or empty");
            return false;
        }

        try
        {
            var downloadUrl = string.Format(CdnUrlTemplate, version);
            var newMermaidJsContent = await _httpClient.GetStringAsync(downloadUrl);

            var localFolder = ApplicationData.Current.LocalFolder;
            var mermaidFolder = await localFolder.CreateFolderAsync(MermaidFolderName, CreationCollisionOption.OpenIfExists);

            // Save the mermaid.min.js file
            var localFile = await mermaidFolder.CreateFileAsync(MermaidJsFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(localFile, newMermaidJsContent);
            _logger.LogInformation($"Successfully saved mermaid.min.js to {localFile.Path}");

            // Save the version file
            var versionFile = await mermaidFolder.CreateFileAsync(VersionFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(versionFile, version);
            _logger.LogInformation($"Successfully saved version {version} to {versionFile.Path}");

            // Write to temporary folder for immediate use
            try
            {
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var tempMermaidFile = await tempFolder.CreateFileAsync(MermaidJsFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(tempMermaidFile, newMermaidJsContent);
                _logger.LogInformation($"Saved updated Mermaid.js to temporary folder: {tempMermaidFile.Path}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to copy Mermaid.js to temp folder: {ex.Message}");
                // Non-fatal — continue even if temp copy fails
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to download and install Mermaid.js update: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Compares two version strings. Returns true if latest is newer than current.
    /// </summary>
    internal static bool CompareVersions(string currentVersionStr, string latestVersionStr)
    {
        // Clean up version strings (remove any non-numeric or dot characters)
        var cleanCurrent = new string(currentVersionStr.Where(c => char.IsDigit(c) || c == '.').ToArray());
        var cleanLatest = new string(latestVersionStr.Trim().Where(c => char.IsDigit(c) || c == '.').ToArray());

        if (string.IsNullOrEmpty(cleanCurrent) || string.IsNullOrEmpty(cleanLatest))
        {
            return false;
        }

        // Ensure version strings have at least two parts (major.minor)
        if (cleanCurrent.Count(c => c == '.') < 1) cleanCurrent += ".0";
        if (cleanLatest.Count(c => c == '.') < 1) cleanLatest += ".0";

        try
        {
            var current = new Version(cleanCurrent);
            var latest = new Version(cleanLatest);
            return current < latest;
        }
        catch
        {
            return false;
        }
    }
}
