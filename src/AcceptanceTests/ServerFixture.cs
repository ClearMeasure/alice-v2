using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;

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
    /// True once the Worker resource is running. Worker is started for SQL Server
    /// mode and skipped for SQLite (Worker requires SqlServerTransport).
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

        var useSqlite = IsSqliteMode();

        if (useSqlite)
        {
            await StartViaDotnetRunAsync(sqliteMode: true);
        }
        else
        {
            await StartViaDotnetRunAsync(sqliteMode: false);
        }

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

    /// <summary>
    /// Returns true when the acceptance test run targets SQLite (e.g. ARM SQLite CI job).
    /// </summary>
    private static bool IsSqliteMode()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("DATABASE_ENGINE"), "SQLite", StringComparison.OrdinalIgnoreCase))
            return true;

        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__SqlConnectionString") ?? "";
        return connStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // dotnet run startup (used for both SQL Server and SQLite modes)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task StartViaDotnetRunAsync(bool sqliteMode)
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        ApplicationBaseUrl = configuration["ApplicationBaseUrl"] ?? "https://localhost:7174";

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlConnectionString")
            ?? configuration.GetConnectionString("SqlConnectionString")
            ?? (sqliteMode ? "Data Source=AISoftwareFactory.db" : throw new InvalidOperationException("ConnectionStrings__SqlConnectionString is required for SQL Server mode"));

        if (sqliteMode)
        {
            // Resolve SQLite path to absolute so the server process uses the same file.
            if (!connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                var dbPath = connectionString["Data Source=".Length..].Trim();
                var semicolonIndex = dbPath.IndexOf(';');
                if (semicolonIndex >= 0) dbPath = dbPath[..semicolonIndex];
                if (!Path.IsPathRooted(dbPath))
                {
                    var absolutePath = Path.GetFullPath(dbPath);
                    connectionString = $"Data Source={absolutePath}";
                }
            }

            // Publish the absolute path so TestHost's IConfiguration (which reads env vars)
            // resolves the same database file when ZDataLoader seeds data.
            Environment.SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", connectionString);
        }

        // Seed test data before starting the server.
        if (sqliteMode)
            InitializeDatabaseOnce(connectionString);
        else
            SeedSqlServerTestData(connectionString);

        var buildConfig = GetBuildConfiguration();

        // For SQLite: use --no-launch-profile to prevent launchSettings.json from
        // overriding connection strings; inject settings manually via env vars.
        // For SQL Server: use the default launch profile so the server starts normally.
        var arguments = sqliteMode
            ? $"run --no-build --configuration {buildConfig} --no-launch-profile --urls={ApplicationBaseUrl}"
            : $"run --no-build --configuration {buildConfig} --urls={ApplicationBaseUrl}";

        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = UiServerProjectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _serverProcess.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] = connectionString;

        if (sqliteMode)
        {
            _serverProcess.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            _serverProcess.StartInfo.Environment["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
                "InstrumentationKey=00000000-0000-0000-0000-000000000000";
        }

        _serverProcess.Start();

        // Wait for the server to respond.
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
                var response = await client.GetAsync(ApplicationBaseUrl);
                if (response.IsSuccessStatusCode) break;
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }

            await Task.Delay(1000);
        }

        if (DateTime.UtcNow >= deadline)
            throw new Exception($"UI.Server did not start within {WaitTimeoutSeconds}s. Last exception: {lastEx}");

        if (!sqliteMode)
        {
            await StartWorkerAsync(connectionString);
            WorkerStarted = true;
        }
    }

    /// <summary>
    /// Starts the Worker process (NServiceBus message handler host).
    /// Worker requires SqlServerTransport and is skipped in SQLite mode.
    /// </summary>
    private async Task StartWorkerAsync(string connectionString)
    {
        TestContext.Out.WriteLine("Worker: starting...");
        var buildConfig = GetBuildConfiguration();
        var arguments = $"run --no-build --configuration {buildConfig} --no-launch-profile";

        _workerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
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
        {
            TestContext.Out.WriteLine(
                $"Worker: did not detect startup confirmation within {WaitTimeoutSeconds}s. " +
                "Proceeding anyway — SqlServerTransport is durable.");
        }
        else
        {
            TestContext.Out.WriteLine("Worker: started successfully.");
        }
    }

    private static string GetBuildConfiguration() =>
        AppDomain.CurrentDomain.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)
            ? "Release"
            : "Debug";

    // ─────────────────────────────────────────────────────────────────────────
    // Data seeding
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds test data into the SQL Server database. The database and schema are
    /// already created by Setup-DatabaseForBuild; only employee rows are needed here.
    /// </summary>
    private static void SeedSqlServerTestData(string connectionString)
    {
        var config = new LiteralDatabaseConfiguration(connectionString);
        using var context = new DataContext(config);

        new DatabaseEmptier(context.Database).DeleteAllData();
        foreach (var employee in BuildEmployees())
            context.Add(employee);

        context.SaveChanges();
    }

    /// <summary>
    /// Initialises the SQLite database (EnsureCreated + seed) for the SQLite path.
    /// </summary>
    private static void InitializeDatabaseOnce(string connectionString)
    {
        var config = new LiteralDatabaseConfiguration(connectionString);
        using var context = new DataContext(config);

        var isSqlite = context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        if (isSqlite) context.Database.EnsureCreated();

        new ZDataLoader().LoadData();
        TestContext.Out.WriteLine("ZDataLoader().LoadData(); - complete");
        config.ResetConnectionPool();
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
    // Health gate
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies the application is reachable and healthy before tests start.
    /// Checks the site root and the /_healthcheck endpoint (which validates database
    /// connectivity). Fails fast with a clear diagnostic message instead of letting
    /// tests hang on an unreachable or unhealthy server.
    /// </summary>
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
            Assert.Fail(
                $"Health gate FAILED: Site is not reachable at {ApplicationBaseUrl} after {maxAttempts} attempts. {detail}");
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
            Assert.Fail(
                $"Health gate FAILED: /_healthcheck did not return Healthy or Degraded after {maxAttempts} attempts. {detail}");
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

    /// <summary>
    /// Minimal <see cref="IDatabaseConfiguration"/> that wraps a literal connection string
    /// for use during test data seeding, without requiring the full DI container.
    /// </summary>
    private sealed class LiteralDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;
        public void ResetConnectionPool() { }
    }
}
