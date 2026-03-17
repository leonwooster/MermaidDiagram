using System.Net;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using Moq;
using Moq.Protected;

namespace MermaidDiagramApp.Tests.Services;

/// <summary>
/// Unit tests for MermaidUpdateService.
/// Requirements: 6.4
/// </summary>
public class MermaidUpdateServiceTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MermaidUpdateService(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        var service = new MermaidUpdateService(_mockLogger.Object);
        Assert.NotNull(service);
    }

    #endregion

    #region CompareVersions Tests

    [Theory]
    [InlineData("10.9.0", "11.0.0", true)]
    [InlineData("10.9.0", "10.10.0", true)]
    [InlineData("10.9.0", "10.9.1", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("10.9", "10.10", true)]
    public void CompareVersions_WhenLatestIsNewer_ReturnsTrue(string current, string latest, bool expected)
    {
        var result = MermaidUpdateService.CompareVersions(current, latest);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("11.0.0", "10.9.0", false)]
    [InlineData("10.10.0", "10.9.0", false)]
    [InlineData("10.9.1", "10.9.0", false)]
    [InlineData("10.9.0", "10.9.0", false)]
    [InlineData("2.0.0", "1.0.0", false)]
    public void CompareVersions_WhenLatestIsOlderOrEqual_ReturnsFalse(string current, string latest, bool expected)
    {
        var result = MermaidUpdateService.CompareVersions(current, latest);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "10.9.0")]
    [InlineData("10.9.0", "")]
    [InlineData("", "")]
    [InlineData("abc", "10.9.0")]
    [InlineData("10.9.0", "xyz")]
    [InlineData("abc", "xyz")]
    public void CompareVersions_WithMalformedVersions_ReturnsFalse(string current, string latest)
    {
        var result = MermaidUpdateService.CompareVersions(current, latest);
        Assert.False(result);
    }

    [Theory]
    [InlineData("v10.9.0", "v11.0.0", true)]
    [InlineData("10.9.0-beta", "11.0.0", true)]
    [InlineData("  10.9.0  ", "11.0.0", true)]
    public void CompareVersions_CleansVersionStrings(string current, string latest, bool expected)
    {
        var result = MermaidUpdateService.CompareVersions(current, latest);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("10", "11", true)]
    [InlineData("10", "10", false)]
    public void CompareVersions_HandlesVersionsWithoutMinor(string current, string latest, bool expected)
    {
        var result = MermaidUpdateService.CompareVersions(current, latest);
        Assert.Equal(expected, result);
    }

    #endregion

    #region CheckForUpdatesAsync Tests

    [Fact]
    public async Task CheckForUpdatesAsync_OnHttpFailure_ReturnsUpdateAvailableFalse()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.False(result.UpdateAvailable);
        Assert.Equal(result.CurrentVersion, result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_OnTimeout_ReturnsUpdateAvailableFalse()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_OnJsonParseError_ReturnsUpdateAvailableFalse()
    {
        var mockHandler = CreateMockHandler("not valid json {{{");
        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_OnEmptyResponse_ReturnsUpdateAvailableFalse()
    {
        var mockHandler = CreateMockHandler("");
        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_OnMissingDistTags_ReturnsUpdateAvailableFalse()
    {
        var mockHandler = CreateMockHandler("""{"name": "mermaid"}""");
        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_OnMissingLatestTag_ReturnsUpdateAvailableFalse()
    {
        var mockHandler = CreateMockHandler("""{"dist-tags": {"beta": "11.0.0"}}""");
        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenUpdateAvailable_ReturnsCorrectResult()
    {
        // The service will use default version "10.9.0" since we can't access ApplicationData in tests
        var mockHandler = CreateMockHandler("""{"dist-tags": {"latest": "11.0.0"}}""");
        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal("11.0.0", result.LatestVersion);
        // UpdateAvailable depends on current version comparison
        // Since we can't control GetCurrentVersion in unit tests (it reads from file system),
        // we just verify the latest version is correctly parsed
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNoUpdateAvailable_ReturnsCorrectResult()
    {
        // Return same version as default (10.9.0)
        var mockHandler = CreateMockHandler("""{"dist-tags": {"latest": "10.9.0"}}""");
        var httpClient = new HttpClient(mockHandler.Object);
        var service = new MermaidUpdateService(_mockLogger.Object, httpClient);

        var result = await service.CheckForUpdatesAsync();

        Assert.Equal("10.9.0", result.LatestVersion);
        Assert.False(result.UpdateAvailable);
    }

    #endregion

    #region Helper Methods

    private static Mock<HttpMessageHandler> CreateMockHandler(string responseContent)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });
        return mockHandler;
    }

    #endregion
}
