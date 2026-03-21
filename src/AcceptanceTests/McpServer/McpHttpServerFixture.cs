using ModelContextProtocol.Client;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

/// <summary>
/// SetUpFixture for MCP HTTP acceptance tests. Connects to the /mcp endpoint
/// hosted inside UI.Server (started by the outer ServerFixture) using the
/// StreamableHttp transport. This fixture runs after ServerFixture because it
/// is scoped to the inner ClearMeasure.Bootcamp.AcceptanceTests.McpServer namespace.
/// </summary>
[SetUpFixture]
public class McpHttpServerFixture
{
    public static McpClient? Client { get; private set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var mcpUrl = ServerFixture.ApplicationBaseUrl.TrimEnd('/') + "/mcp";
        TestContext.Out.WriteLine($"McpHttpServerFixture: connecting to {mcpUrl}");

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var httpClient = new HttpClient(handler);

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(mcpUrl),
            Name = "AISoftwareFactory-AcceptanceTest"
        };
        var transport = new HttpClientTransport(transportOptions, httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);

        TestContext.Out.WriteLine("McpHttpServerFixture: MCP client connected.");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (Client != null)
        {
            await Client.DisposeAsync();
            Client = null;
        }
    }
}
