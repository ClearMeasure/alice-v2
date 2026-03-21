namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Verifies the full application stack reports healthy. The Aspire AppHost is started
/// by <see cref="ServerFixture"/> before this fixture runs; these tests simply assert
/// against the already-running instance.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AppHostHealthTests
{
    private string _uiBaseUrl = string.Empty;

    private static readonly HttpClientHandler InsecureHandler = new()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    [OneTimeSetUp]
    public void SetUp()
    {
        _uiBaseUrl = ServerFixture.ApplicationBaseUrl.Length > 0
            ? ServerFixture.ApplicationBaseUrl
            : "https://localhost:7174";
    }

    [Test]
    public async Task UiServer_HealthCheck_ReturnsKnownStatus()
    {
        using var client = new HttpClient(InsecureHandler, disposeHandler: false);
        var body = await client.GetStringAsync($"{_uiBaseUrl}/_healthcheck");
        TestContext.Out.WriteLine($"/_healthcheck: {body}");
        var isKnownStatus = body.Contains("Healthy", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);
        isKnownStatus.ShouldBe(true, $"Expected health check to return Healthy or Degraded but got: {body}");
    }

    [Test]
    public async Task UiServer_Root_ReturnsSuccess()
    {
        using var client = new HttpClient(InsecureHandler, disposeHandler: false);
        var response = await client.GetAsync(_uiBaseUrl);
        TestContext.Out.WriteLine($"GET {_uiBaseUrl} -> {(int)response.StatusCode}");
        ((int)response.StatusCode).ShouldBeInRange(200, 399);
    }
}
