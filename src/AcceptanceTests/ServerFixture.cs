using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

[SetUpFixture]
public class ServerFixture
{
    private const string UiServerProjectPath = "../../../../UI/Server";
    private const int WaitTimeoutSeconds = 60;

    public static int SlowMo { get; set; } = 100;
    public static string ApplicationBaseUrl { get; private set; } = string.Empty;
    public static bool SkipScreenshotsForSpeed { get; set; } = true;
    public static bool HeadlessTestBrowser { get; set; } = true;

    /// <summary>
    /// True once the Worker resource is running. For SQL Server mode the AppHost
    /// always starts Worker. For SQLite mode Worker is skipped (requires SqlServerTransport).
    /// </summary>
    public static bool WorkerStarted { get; private set; }

    /// <summary>
    /// Shared Playwright instance for all tests. Thread-safe for parallel execution.
    /// </summary>
    public static IPlaywright Playwright { get; private set; } = null!;

    private DistributedApplication? _app;
    private Process? _serverProcess;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        SlowMo = int.TryParse(Environment.GetEnvironmentVariable("SlowMo"), out var slowMo) ? slowMo : 100;
        SkipScreenshotsForSpeed = !string.Equals(Environment.GetEnvironmentVariable("SkipScreenshotsForSpeed"), "false", StringComparison.OrdinalIgnoreCase);
        HeadlessTestBrowser = !string.Equals(Environment.GetEnvironmentVariable("HeadlessTestBrowser"), "false", StringComparison.OrdinalIgnoreCase);

        var useSqlite = IsSqliteMode();

        if (useSqlite)
        {
            await StartViaDotnetRunAsync();
        }
        else
        {
            await StartViaAspireAsync();
        }

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await VerifyApplicationHealthy();
        await new BlazorWasmWarmUp(Playwright, ApplicationBaseUrl).ExecuteAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
            _app = null;
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
    // Aspire path (SQL Server / Docker container)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task StartViaAspireAsync()
    {
        // Supply the sql-password parameter if not already set in the environment.
        // In CI, Setup-DatabaseForBuild starts the SQL Server container using the
        // password derived from Get-SqlServerPassword("aisoftwarefactory-mssql"),
        // which returns "aisoftwarefactory-mssql#1A". Aspire reads the parameter
        // value from Parameters:{name} configuration, so inject it here when no
        // explicit override is present.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Parameters__sql-password")))
        {
            Environment.SetEnvironmentVariable("Parameters__sql-password", "aisoftwarefactory-mssql#1A");
        }

        // AppInsights is optional for testing. Provide a dummy value so Aspire does not
        // fail to resolve the connection string resource when no real key is configured.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__AppInsights")))
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__AppInsights", "InstrumentationKey=00000000-0000-0000-0000-000000000000");
        }

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        var uiEndpoint = _app.GetEndpoint("ui-server");
        ApplicationBaseUrl = uiEndpoint.ToString().TrimEnd('/');

        var connectionString = await _app.GetConnectionStringAsync("SqlConnectionString")
            ?? throw new InvalidOperationException("SqlConnectionString not available from Aspire AppHost.");

        // Publish the connection string so TestHost (used by TracerBulletTests and others)
        // connects to the same SQL Server managed by the Aspire AppHost.
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlConnectionString", connectionString);

        await SeedTestDataAsync(connectionString);

        // Worker is always started by the AppHost when using SQL Server transport.
        WorkerStarted = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SQLite / direct dotnet run path
    // ─────────────────────────────────────────────────────────────────────────

    private async Task StartViaDotnetRunAsync()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        ApplicationBaseUrl = configuration["ApplicationBaseUrl"] ?? "https://localhost:7174";

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlConnectionString")
            ?? configuration.GetConnectionString("SqlConnectionString")
            ?? "Data Source=AISoftwareFactory.db";

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

        // Seed test data into the SQLite database before starting the server.
        InitializeDatabaseOnce(connectionString);

        var buildConfig = GetBuildConfiguration();
        var arguments = $"run --no-build --configuration {buildConfig} --no-launch-profile --urls={ApplicationBaseUrl}";

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

        _serverProcess.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        _serverProcess.StartInfo.Environment["ConnectionStrings__SqlConnectionString"] = connectionString;
        _serverProcess.StartInfo.Environment["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000";

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

        // Worker requires SqlServerTransport — skip for SQLite.
        WorkerStarted = false;
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
    /// Clears all existing data and seeds the baseline employees using the
    /// connection string provided by the Aspire AppHost.
    /// </summary>
    private static async Task SeedTestDataAsync(string connectionString)
    {
        var config = new LiteralDatabaseConfiguration(connectionString);
        await using var context = new DataContext(config);

        new DatabaseEmptier(context.Database).DeleteAllData();

        foreach (var employee in BuildEmployees())
            context.Add(employee);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Initialises the SQLite database (EnsureCreated + seed) for the direct dotnet-run path.
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
    // Process management (SQLite path only)
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
