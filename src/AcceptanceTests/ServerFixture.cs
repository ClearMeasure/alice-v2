using Aspire.Hosting;
using Aspire.Hosting.Testing;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

[SetUpFixture]
public class ServerFixture
{
    public static int SlowMo { get; set; } = 100;
    public static string ApplicationBaseUrl { get; private set; } = string.Empty;
    public static bool SkipScreenshotsForSpeed { get; set; } = true;
    public static bool HeadlessTestBrowser { get; set; } = true;

    /// <summary>
    /// True once the Aspire AppHost has started and the Worker resource is running.
    /// The Worker is always started by the AppHost when using SQL Server transport.
    /// </summary>
    public static bool WorkerStarted { get; private set; }

    /// <summary>
    /// Shared Playwright instance for all tests. Thread-safe for parallel execution.
    /// </summary>
    public static IPlaywright Playwright { get; private set; } = null!;

    private DistributedApplication? _app;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        SlowMo = int.TryParse(Environment.GetEnvironmentVariable("SlowMo"), out var slowMo) ? slowMo : 100;
        SkipScreenshotsForSpeed = !string.Equals(Environment.GetEnvironmentVariable("SkipScreenshotsForSpeed"), "false", StringComparison.OrdinalIgnoreCase);
        HeadlessTestBrowser = !string.Equals(Environment.GetEnvironmentVariable("HeadlessTestBrowser"), "false", StringComparison.OrdinalIgnoreCase);

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

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await VerifyApplicationHealthy();
        await new BlazorWasmWarmUp(Playwright, ApplicationBaseUrl).ExecuteAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_app is not null)
            await _app.DisposeAsync();
        Playwright?.Dispose();
    }

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

    private static IEnumerable<Employee> BuildEmployees()
    {
        yield return new Employee("jpalermo", "Jeffrey Palermo");
        yield return new Employee("sspaniel", "Sean Spaniel");
        yield return new Employee("hsimpson", "Homer Simpson");
        yield return new Employee("tlovejoy", "Timothy Lovejoy Jr");
        yield return new Employee("gwillie", "Groundskeeper Willie MacDougal");
        yield return new Employee("nflanders", "Ned Flanders");
    }

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
