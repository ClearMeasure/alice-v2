using System.Diagnostics;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

internal static class AppHostHarness
{
    private const string SqlPassword = "aisoftwarefactory-mssql#1A";
    private const int WaitTimeoutSeconds = 120;

    private static readonly HttpClientHandler InsecureHandler = new()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    internal static async Task<Process> StartAsync(string applicationBaseUrl)
    {
        var healthUrl = $"{applicationBaseUrl.TrimEnd('/')}/_healthcheck";
        if (await IsHealthyAsync(healthUrl))
        {
            throw new InvalidOperationException("AppHost is already running for the requested base URL.");
        }

        var projectPath = Path.Combine(ResolveRepoRoot(), "src", "AppHost");
        var configuration = AppDomain.CurrentDomain.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar,
            StringComparison.Ordinal)
            ? "Release"
            : "Debug";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --configuration {configuration}",
                WorkingDirectory = projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["DISABLE_NGROK_TUNNEL"] = "true";
        process.StartInfo.Environment["NGROK_AUTHTOKEN"] = "";
        process.StartInfo.Environment["AI_OpenAI_ApiKey"] = "";
        process.StartInfo.Environment["AI_OpenAI_Url"] = "";
        process.StartInfo.Environment["AI_OpenAI_Model"] = "";

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Failed to start AppHost process.");
        }

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                TestContext.Out.WriteLine($"[AppHost] {e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                TestContext.Out.WriteLine($"[AppHost err] {e.Data}");
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await WaitForHealthyAsync(healthUrl, TimeSpan.FromSeconds(WaitTimeoutSeconds));
            return process;
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            process.Dispose();
            throw;
        }
    }

    internal static async Task<bool> IsHealthyAsync(string url)
    {
        try
        {
            using var client = new HttpClient(InsecureHandler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            var body = await client.GetStringAsync(url);
            return body.Contains("Healthy", StringComparison.OrdinalIgnoreCase)
                || body.Contains("Degraded", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    internal static async Task WaitForHealthyAsync(string url, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsHealthyAsync(url))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        throw new TimeoutException(
            $"Health check at '{url}' did not return Healthy or Degraded within {timeout.TotalSeconds}s.");
    }

    internal static void SetSqlConnectionStringEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__SqlConnectionString",
            $"server=127.0.0.1,{ResolveSqlPort()};database=AISoftwareFactory;User ID=sa;Password={SqlPassword};TrustServerCertificate=true;");
    }

    internal static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "AISoftwareFactory.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from AppContext.BaseDirectory.");
    }

    private static string ResolveSqlPort()
    {
        using var process = Process.Start(new ProcessStartInfo("docker", "port aisoftwarefactory-mssql 1433/tcp")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        process?.WaitForExit(5000);
        var output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(output, @":(?<port>\d+)\s*$", System.Text.RegularExpressions.RegexOptions.Multiline);
        return match.Success ? match.Groups["port"].Value : "1433";
    }
}
