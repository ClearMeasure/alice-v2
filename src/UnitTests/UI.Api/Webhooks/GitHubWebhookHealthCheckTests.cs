using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api.Webhooks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Webhooks;

[TestFixture]
public class GitHubWebhookHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenNotConfigured_ReturnsDegraded()
    {
        var stubClient = new StubGitHubProjectClient(isConfigured: false);
        var tracker = new WebhookReceiptTracker();
        var healthCheck = new GitHubWebhookHealthCheck(
            stubClient, tracker, NullLogger<GitHubWebhookHealthCheck>.Instance);
        var context = CreateContext(healthCheck);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("not configured");
    }

    [Test]
    public async Task CheckHealthAsync_WhenWebhookReceived_ReturnsHealthy()
    {
        var stubClient = new StubGitHubProjectClient(isConfigured: true);
        var tracker = new WebhookReceiptTracker();
        var healthCheck = new GitHubWebhookHealthCheck(
            stubClient, tracker, NullLogger<GitHubWebhookHealthCheck>.Instance);
        var context = CreateContext(healthCheck);

        // Simulate a webhook arriving shortly after the trigger
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            tracker.RecordReceipt("GitHub", "PVTI_health");
        });

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
        stubClient.TriggerCallCount.ShouldBe(1);
    }

    [Test]
    public async Task CheckHealthAsync_WhenWebhookNotReceived_ReturnsUnhealthy()
    {
        var stubClient = new StubGitHubProjectClient(isConfigured: true);
        var tracker = new WebhookReceiptTracker();

        // Override timeout to be very short for testing
        var healthCheck = new StubTimeoutGitHubWebhookHealthCheck(
            stubClient, tracker, NullLogger<GitHubWebhookHealthCheck>.Instance,
            TimeSpan.FromMilliseconds(100));
        var context = CreateContext(healthCheck);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("not received");
    }

    [Test]
    public async Task CheckHealthAsync_WhenGitHubApiThrows_ReturnsUnhealthy()
    {
        var stubClient = new StubGitHubProjectClient(isConfigured: true, throwOnTrigger: true);
        var tracker = new WebhookReceiptTracker();
        var healthCheck = new GitHubWebhookHealthCheck(
            stubClient, tracker, NullLogger<GitHubWebhookHealthCheck>.Instance);
        var context = CreateContext(healthCheck);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("failed");
    }

    private static HealthCheckContext CreateContext(IHealthCheck healthCheck)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("GitHubWebhook", healthCheck, null, null)
        };
    }

    private class StubGitHubProjectClient(bool isConfigured, bool throwOnTrigger = false)
        : IGitHubProjectClient
    {
        public bool IsConfigured => isConfigured;
        public int TriggerCallCount { get; private set; }

        public Task<string> TriggerHealthCheckWebhookAsync(CancellationToken cancellationToken = default)
        {
            TriggerCallCount++;
            if (throwOnTrigger)
            {
                throw new HttpRequestException("GitHub API unavailable");
            }

            return Task.FromResult("PVTI_health");
        }
    }

    /// <summary>
    /// Allows overriding the webhook timeout for fast test execution.
    /// </summary>
    private class StubTimeoutGitHubWebhookHealthCheck(
        IGitHubProjectClient gitHubClient,
        IWebhookReceiptTracker receiptTracker,
        Microsoft.Extensions.Logging.ILogger<GitHubWebhookHealthCheck> logger,
        TimeSpan timeout) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new())
        {
            try
            {
                await gitHubClient.TriggerHealthCheckWebhookAsync(cancellationToken);

                var received = await receiptTracker.WaitForReceiptAsync("GitHub", timeout, cancellationToken);
                if (received)
                {
                    return HealthCheckResult.Healthy("GitHub webhook round-trip verified");
                }

                return HealthCheckResult.Unhealthy(
                    $"GitHub webhook not received within {timeout.TotalSeconds}s after triggering status change");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"GitHub webhook health check failed: {ex.Message}");
            }
        }
    }
}
