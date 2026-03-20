using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Api.Webhooks;

/// <summary>
/// Verifies round-trip connectivity between the application and GitHub Projects V2.
/// Triggers a status change on a health-check item via the GitHub API,
/// then waits for the corresponding webhook to arrive back at the application.
/// Returns Degraded when the GitHub personal access token is not configured.
/// </summary>
public class GitHubWebhookHealthCheck(
    IGitHubProjectClient gitHubClient,
    IWebhookReceiptTracker receiptTracker,
    ILogger<GitHubWebhookHealthCheck> logger) : IHealthCheck
{
    private static readonly TimeSpan WebhookTimeout = TimeSpan.FromSeconds(30);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        if (!gitHubClient.IsConfigured)
        {
            logger.LogDebug("GitHub personal access token not configured; skipping webhook health check");
            return HealthCheckResult.Degraded("GitHub personal access token not configured");
        }

        try
        {
            logger.LogInformation("Triggering GitHub webhook health check");
            await gitHubClient.TriggerHealthCheckWebhookAsync(cancellationToken);

            var received = await receiptTracker.WaitForReceiptAsync("GitHub", WebhookTimeout, cancellationToken);
            if (received)
            {
                logger.LogDebug("GitHub webhook round-trip health check succeeded");
                return HealthCheckResult.Healthy("GitHub webhook round-trip verified");
            }

            logger.LogWarning("GitHub webhook was not received within {Timeout}s", WebhookTimeout.TotalSeconds);
            return HealthCheckResult.Unhealthy(
                $"GitHub webhook not received within {WebhookTimeout.TotalSeconds}s after triggering status change");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GitHub webhook health check failed");
            return HealthCheckResult.Unhealthy($"GitHub webhook health check failed: {ex.Message}");
        }
    }
}
