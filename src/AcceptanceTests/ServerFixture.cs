using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

[SetUpFixture]
public class ServerFixture
{
    private const string UiServerProjectPath = "../../../../UI/Server";
    private const string WorkerProjectPath = "../../../../Worker";
    private const int WaitTimeoutSeconds = 60;

    public static int SlowMo { get; set; } = 100;
    public static string ApplicationBaseUrl { get; private set; } = string.Empty;
    public static bool SkipScreenshotsForSpeed { get; set; } = true;
    public static bool HeadlessTestBrowser { get; set; } = true;

    /// <summary>
    /// True once the Worker process is running.
    /// </summary>
    public static bool WorkerStarted { get; private set; }

    /// <summary>
    /// Shared Playwright instance for all tests. Thread-safe for parallel execution.
    /// </summary>
    public static IPlaywright Playwright { get; private set; } = null!;

    private Process? _serverProcess;
    private Process? _workerProcess;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        SlowMo = int.TryParse(Environment.GetEnvironmentVariable("SlowMo"), out var slowMo) ? slowMo : 100;
        SkipScreenshotsForSpeed = !string.Equals(Environment.GetEnvironmentVariable("SkipScreenshotsForSpeed"), "false", StringComparison.OrdinalIgnoreCase);
        HeadlessTestBrowser = !string.Equals(Environment.GetEnvironmentVariable("HeadlessTestBrowser"), "false", StringComparison.OrdinalIgnoreCase);

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlConnectionString")
            ?? throw new InvalidOperationException("ConnectionStrings__SqlConnectionString environment variable is required");

        ApplicationBaseUrl = "https://localhost:7174";

        SeedTestData(connectionString);

        var buildConfig = GetBuildConfiguration();

        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --configuration {buildConfig} --urls={ApplicationBaseUrl}",
                WorkingDirectory = UiServerProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _serverProcess.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] = connectionString;
        _serverProcess.Start();

        await WaitForServerAsync(ApplicationBaseUrl);

        await StartWorkerAsync(connectionString);
        WorkerStarted = true;

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await VerifyApplicationHealthy();
        await new BlazorWasmWarmUp(Playwright, ApplicationBaseUrl).ExecuteAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Stop Worker first (it calls back to UI.Server via RemotableBus)
        if (_workerProcess is not null)
        {
            await StopProcessAsync(_workerProcess);
            _workerProcess.Dispose();
            _workerProcess = null;
        }

        if (_serverProcess is not null)
        {
            await StopProcessAsync(_serverProcess);
            _serverProcess.Dispose();
            _serverProcess = null;
        }

        Playwright?.Dispose();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Data seeding
    // ─────────────────────────────────────────────────────────────────────────

    private static void SeedTestData(string connectionString)
    {
        var config = new LiteralDatabaseConfiguration(connectionString);
        using var context = new DataContext(config);

        new DatabaseEmptier(context.Database).DeleteAllData();

        foreach (var employee in BuildEmployees())
            context.Add(employee);

        context.SaveChanges();
    }

    private static IEnumerable<Employee> BuildEmployees()
    {
        yield return new Employee("jpalermo", "Jeffrey Palermo");
        yield return new Employee("sspaniel", "Sean Spaniel");
        yield return new Employee("hsimpson", "Homer Simpson");
        yield return new Employee("tlovejoy", "Timothy Lovejoy Jr");
        yield return new Employee("gwillie", "Groundskeeper Willie MacDougal");
        yield return new Employee("nflanders", "Ned Flanders");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Process startup
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task WaitForServerAsync(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        var deadline = DateTime.UtcNow.AddSeconds(WaitTimeoutSeconds);
        Exception? lastEx = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode) return;
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }

            await Task.Delay(1000);
        }

        throw new Exception($"UI.Server did not start within {WaitTimeoutSeconds}s. Last exception: {lastEx}");
    }

    private async Task StartWorkerAsync(string connectionString)
    {
        TestContext.Out.WriteLine("Worker: starting...");
        var buildConfig = GetBuildConfiguration();

        _workerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --configuration {buildConfig} --no-launch-profile",
                WorkingDirectory = WorkerProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _workerProcess.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] = connectionString;
        _workerProcess.StartInfo.Environment["RemotableBus__ApiUrl"] =
            $"{ApplicationBaseUrl}/api/blazor-wasm-single-api";
        _workerProcess.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        _workerProcess.StartInfo.Environment["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000";
        _workerProcess.StartInfo.Environment["AI_OpenAI_ApiKey"] = "";
        _workerProcess.StartInfo.Environment["AI_OpenAI_Url"] = "";
        _workerProcess.StartInfo.Environment["AI_OpenAI_Model"] = "";

        var readySignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _workerProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            TestContext.Out.WriteLine($"  [Worker stdout] {e.Data}");
            if (e.Data.Contains("started", StringComparison.OrdinalIgnoreCase))
                readySignal.TrySetResult(true);
        };
        _workerProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) TestContext.Out.WriteLine($"  [Worker stderr] {e.Data}");
        };

        _workerProcess.Start();
        _workerProcess.BeginOutputReadLine();
        _workerProcess.BeginErrorReadLine();

        var timeout = Task.Delay(TimeSpan.FromSeconds(WaitTimeoutSeconds));
        var completed = await Task.WhenAny(readySignal.Task, timeout);

        if (completed == timeout)
            TestContext.Out.WriteLine($"Worker: did not detect startup confirmation within {WaitTimeoutSeconds}s. Proceeding anyway.");
        else
            TestContext.Out.WriteLine("Worker: started successfully.");
    }

    private static string GetBuildConfiguration() =>
        AppDomain.CurrentDomain.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)
            ? "Release"
            : "Debug";

    // ─────────────────────────────────────────────────────────────────────────
    // Health gate
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task VerifyApplicationHealthy()
    {
        const int maxAttempts = 3;
        const int delayBetweenAttemptsMs = 5000;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        TestContext.Out.WriteLine("Health gate: verifying site is reachable...");
        HttpResponseMessage? siteResponse = null;
        Exception? lastSiteException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                siteResponse = await client.GetAsync(ApplicationBaseUrl);
                TestContext.Out.WriteLine($"  GET {ApplicationBaseUrl} -> {(int)siteResponse.StatusCode}");
                if (siteResponse.IsSuccessStatusCode) break;
            }
            catch (Exception ex)
            {
                lastSiteException = ex;
                TestContext.Out.WriteLine($"  GET {ApplicationBaseUrl} -> {ex.GetType().Name}: {ex.Message}");
            }

            if (attempt < maxAttempts) await Task.Delay(delayBetweenAttemptsMs);
        }

        if (siteResponse == null || !siteResponse.IsSuccessStatusCode)
        {
            var detail = lastSiteException != null
                ? $"Last exception: {lastSiteException.GetType().Name}: {lastSiteException.Message}"
                : $"Last status code: {siteResponse?.StatusCode}";
            Assert.Fail($"Health gate FAILED: Site is not reachable at {ApplicationBaseUrl} after {maxAttempts} attempts. {detail}");
        }

        TestContext.Out.WriteLine("Health gate: verifying /_healthcheck...");
        var healthUrl = $"{ApplicationBaseUrl}/_healthcheck";
        string? healthBody = null;
        Exception? lastHealthException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await client.GetAsync(healthUrl);
                healthBody = await response.Content.ReadAsStringAsync();
                TestContext.Out.WriteLine($"  GET {healthUrl} -> {(int)response.StatusCode}: {healthBody}");
                if (response.IsSuccessStatusCode && IsAcceptableHealthStatus(healthBody))
                    break;
            }
            catch (Exception ex)
            {
                lastHealthException = ex;
                TestContext.Out.WriteLine($"  GET {healthUrl} -> {ex.GetType().Name}: {ex.Message}");
            }

            if (attempt < maxAttempts) await Task.Delay(delayBetweenAttemptsMs);
        }

        if (healthBody == null || !IsAcceptableHealthStatus(healthBody))
        {
            var detail = lastHealthException != null
                ? $"Last exception: {lastHealthException.GetType().Name}: {lastHealthException.Message}"
                : $"Body: {healthBody}";
            Assert.Fail($"Health gate FAILED: /_healthcheck did not return Healthy or Degraded after {maxAttempts} attempts. {detail}");
        }

        TestContext.Out.WriteLine("Health gate: PASSED - site is reachable and healthy.");
    }

    private static bool IsAcceptableHealthStatus(string body) =>
        body.Contains("Healthy", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);

    // ─────────────────────────────────────────────────────────────────────────
    // Process management
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task StopProcessAsync(Process process)
    {
        if (process.HasExited) return;
        try
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Warning: error stopping process {process.Id}: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class LiteralDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;
        public void ResetConnectionPool() { }
    }
}
