using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Verifies the full application stack reports healthy by starting the AppHost (Aspire
/// orchestrator) and polling the UI.Server health endpoint until the system is online.
/// When run as part of the full acceptance test suite, ServerFixture has already started
/// AppHost and the health check runs against that instance. When run in isolation,
/// this fixture starts AppHost directly.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AppHostHealthTests
{
    private Process? _appHostProcess;
    private string _uiBaseUrl = string.Empty;

    private static readonly HttpClientHandler InsecureHandler = new()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    [OneTimeSetUp]
    public async Task StartAppHostIfNeeded()
    {
        _uiBaseUrl = ServerFixture.ApplicationBaseUrl.Length > 0
            ? ServerFixture.ApplicationBaseUrl
            : "https://localhost:7174";

        var healthUrl = $"{_uiBaseUrl}/_healthcheck";

        if (await AppHostHarness.IsHealthyAsync(healthUrl))
        {
            TestContext.Out.WriteLine("AppHostHealthTests: server already healthy — reusing running instance.");
            return;
        }

        TestContext.Out.WriteLine("AppHostHealthTests: starting AppHost...");
        _appHostProcess = await AppHostHarness.StartAsync(_uiBaseUrl);
        TestContext.Out.WriteLine("AppHostHealthTests: all services healthy.");
    }

    [OneTimeTearDown]
    public async Task StopAppHost()
    {
        await ProcessCleanupHelper.StopServerProcessAsync(_appHostProcess, _uiBaseUrl);
        try { _appHostProcess?.Dispose(); } catch (ObjectDisposedException) { }
        _appHostProcess = null;
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
