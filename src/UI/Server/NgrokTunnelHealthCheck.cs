using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Verifies the ngrok dev tunnel is connected by querying the local ngrok agent API
/// and confirming at least one active tunnel is established.
/// </summary>
public class NgrokTunnelHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<NgrokTunnelHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var authToken = configuration["NGROK_AUTHTOKEN"];
        if (string.IsNullOrEmpty(authToken))
        {
            logger.LogWarning("NGROK_AUTHTOKEN is not configured; skipping tunnel check");
            return HealthCheckResult.Degraded("NGROK_AUTHTOKEN is not configured");
        }

        var apiUrl = configuration["Ngrok__ApiUrl"] ?? "http://localhost:4040";

        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetFromJsonAsync<NgrokTunnelsResponse>(
                $"{apiUrl.TrimEnd('/')}/api/tunnels",
                cancellationToken);

            if (response?.Tunnels == null || response.Tunnels.Length == 0)
            {
                logger.LogWarning("No active ngrok tunnels found");
                return HealthCheckResult.Unhealthy("No active ngrok tunnels found");
            }

            var tunnel = response.Tunnels.FirstOrDefault(t =>
                string.Equals(t.Proto, "https", StringComparison.OrdinalIgnoreCase));

            if (tunnel is null)
            {
                logger.LogWarning("No HTTPS ngrok tunnel found among {Count} tunnel(s)", response.Tunnels.Length);
                return HealthCheckResult.Degraded("No HTTPS ngrok tunnel found");
            }

            logger.LogDebug("Dev tunnel connected: {PublicUrl}", tunnel.PublicUrl);
            return HealthCheckResult.Healthy($"Dev tunnel connected: {tunnel.PublicUrl}");
        }
        catch (Exception ex)
        {
            var message = $"Ngrok tunnel check failed: {ex.Message}";
            logger.LogWarning(message);
            return HealthCheckResult.Unhealthy(message, ex);
        }
    }

    private record NgrokTunnelsResponse(
        [property: JsonPropertyName("tunnels")] NgrokTunnel[] Tunnels);

    private record NgrokTunnel(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("public_url")] string PublicUrl,
        [property: JsonPropertyName("proto")] string Proto);
}
