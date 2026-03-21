using System.Diagnostics;
using System.Text.RegularExpressions;

var builder = DistributedApplication.CreateBuilder(args);

// Use the same container name, port, and password as the build script (BuildFunctions.ps1
// Get-ContainerName / Get-SqlServerPassword) so 'PrivateBuild.ps1' and Aspire share one
// SQL Server Docker container.  ContainerLifetime.Persistent keeps the container alive
// between Aspire restarts so data is preserved across sessions.
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var databaseAction = Environment.GetEnvironmentVariable("DatabaseAction") ?? "update";

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithContainerName("aisoftwarefactory-mssql")
    .WithHostPort(1433)
    .WithLifetime(ContainerLifetime.Persistent);

var sqlDb = sql.AddDatabase("SqlConnectionString", databaseName: "AISoftwareFactory");

var appInsights = builder.AddConnectionString("AppInsights");

var migrations = builder.AddProject<Projects.Database>("database")
    .WithReference(sqlDb)
    .WaitFor(sql)
    .WithArgs(databaseAction);

// Compute a stable, DNS-safe ngrok subdomain.
// In CI the BUILD_NUMBER / BUILD_BUILDNUMBER / GITHUB_RUN_NUMBER env vars supply a build
// number; otherwise machine name + git branch are combined to keep multiple developer
// environments isolated under the same ngrok auth token.
var ngrokSubdomain = ComputeNgrokSubdomain();

// Ngrok container: tunnels HTTP traffic from <subdomain>.ngrok.app to the local UI.Server.
// The UI.Server is exposed on a fixed HTTP port (5174) so the container can reach it via
// host.docker.internal.  Port 4040 is the ngrok local agent API used by the health check.
var uiServer = builder.AddProject<Projects.UI_Server>("ui-server")
    .WithReference(sqlDb)
    .WithReference(appInsights)
    // Fixed HTTP port so the ngrok container can reach the app via host.docker.internal.
    .WithHttpEndpoint(port: 5174, name: "app-http")
    .WithHttpsEndpoint(port: 7174, name: "app-https")
    .WaitForCompletion(migrations);

var ngrokAuthToken = Environment.GetEnvironmentVariable("NGROK_AUTHTOKEN") ?? string.Empty;
if (!string.IsNullOrWhiteSpace(ngrokAuthToken))
{
    var ngrok = builder.AddContainer("ngrok", "ngrok/ngrok")
        .WithEnvironment("NGROK_AUTHTOKEN", ngrokAuthToken)
        .WithArgs(
            "http",
            "--domain", $"{ngrokSubdomain}.ngrok.app",
            "--log", "stdout",
            "http://host.docker.internal:5174")
        // --add-host is required on Linux (Docker Engine) so that host.docker.internal
        // resolves to the host machine's gateway IP.
        .WithContainerRuntimeArgs("--add-host=host.docker.internal:host-gateway")
        .WithHttpEndpoint(port: 4040, targetPort: 4040, name: "dashboard");

    uiServer
        .WithEnvironment("Ngrok__ApiUrl", "http://localhost:4040")
        .WithEnvironment("Ngrok__Subdomain", ngrokSubdomain);

    // Ngrok must start after UI.Server is ready so it has something to tunnel to.
    ngrok.WaitFor(uiServer);
}

builder.AddProject<Projects.Worker>("worker")
    .WithReference(sqlDb)
    .WithReference(appInsights)
    .WaitForCompletion(migrations);

builder.Build().Run();

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static string ComputeNgrokSubdomain()
{
    // Prefer explicit build-number env vars (CI systems).
    var buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER")
        ?? Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER")
        ?? Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");

    if (!string.IsNullOrEmpty(buildNumber))
        return SanitizeForNgrok($"build-{buildNumber}");

    // Fall back to machine name + branch for developer workstations.
    var branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME")
        ?? Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME")
        ?? GetGitBranch();

    return SanitizeForNgrok($"{Environment.MachineName}-{branch}");
}

static string GetGitBranch()
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo("git", "rev-parse --abbrev-ref HEAD")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        process?.WaitForExit(3000);
        return process?.StandardOutput.ReadToEnd().Trim() ?? "unknown";
    }
    catch
    {
        return "unknown";
    }
}

/// <summary>
/// Converts an arbitrary string to a valid ngrok subdomain label:
/// lowercase alphanumeric + hyphens, no leading/trailing hyphens, max 63 chars.
/// </summary>
static string SanitizeForNgrok(string input)
{
    var sanitized = Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]+", "-")
                         .Trim('-');
    return sanitized.Length > 63 ? sanitized[..63].TrimEnd('-') : sanitized;
}
