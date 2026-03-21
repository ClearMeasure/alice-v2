using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Verifies the full application stack reports healthy by starting the AppHost (Aspire
/// orchestrator) and polling the UI.Server health endpoint until the system is online.
/// When run as part of the full acceptance test suite, ServerFixture has already started
/// UI.Server and the health check runs against that instance. When run in isolation,
/// this fixture starts AppHost directly, which launches UI.Server and Worker as child processes.
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

        if (await IsHealthyAsync(healthUrl))
        {
            TestContext.Out.WriteLine("AppHostHealthTests: server already healthy — reusing running instance.");
            return;
        }

        TestContext.Out.WriteLine("AppHostHealthTests: starting AppHost...");
        var appHostDir = Path.Combine(ResolveRepoRoot(), "src", "AppHost");

        _appHostProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = appHostDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Failed to start AppHost process.");

        _appHostProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                TestContext.Out.WriteLine($"[AppHost] {e.Data}");
        };
        _appHostProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                TestContext.Out.WriteLine($"[AppHost err] {e.Data}");
        };
        _appHostProcess.BeginOutputReadLine();
        _appHostProcess.BeginErrorReadLine();

        await WaitForHealthyAsync(healthUrl, TimeSpan.FromSeconds(120));
        TestContext.Out.WriteLine("AppHostHealthTests: all services healthy.");
    }

    [OneTimeTearDown]
    public async Task StopAppHost()
    {
        if (_appHostProcess is { HasExited: false })
        {
            _appHostProcess.Kill(entireProcessTree: true);
            await _appHostProcess.WaitForExitAsync();
        }
        _appHostProcess?.Dispose();
        _appHostProcess = null;
    }

    [Test]
    public async Task UiServer_HealthCheck_ReturnsHealthyOrDegraded()
    {
        using var client = new HttpClient(InsecureHandler, disposeHandler: false);
        var body = await client.GetStringAsync($"{_uiBaseUrl}/_healthcheck");
        TestContext.Out.WriteLine($"/_healthcheck: {body}");
        IsAcceptableHealthStatus(body).ShouldBeTrue($"Expected Healthy or Degraded but got: {body}");
    }

    [Test]
    public async Task UiServer_Root_ReturnsSuccess()
    {
        using var client = new HttpClient(InsecureHandler, disposeHandler: false);
        var response = await client.GetAsync(_uiBaseUrl);
        TestContext.Out.WriteLine($"GET {_uiBaseUrl} -> {(int)response.StatusCode}");
        ((int)response.StatusCode).ShouldBeInRange(200, 399);
    }

    private static bool IsAcceptableHealthStatus(string body) =>
        body.Contains("Healthy", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> IsHealthyAsync(string url)
    {
        try
        {
            using var client = new HttpClient(InsecureHandler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            var body = await client.GetStringAsync(url);
            return IsAcceptableHealthStatus(body);
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForHealthyAsync(string url, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsHealthyAsync(url)) return;
            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        throw new TimeoutException(
            $"Health check at '{url}' did not return Healthy or Degraded within {timeout.TotalSeconds}s.");
    }

    private static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "AISoftwareFactory.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repository root from AppContext.BaseDirectory.");
    }
}
