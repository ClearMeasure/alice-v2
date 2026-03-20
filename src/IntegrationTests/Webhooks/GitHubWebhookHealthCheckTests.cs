using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Webhooks;

[TestFixture]
public class GitHubWebhookHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WithLiveGitHub_ReturnsHealthy()
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var pat = configuration["GitHub:PersonalAccessToken"];

        if (string.IsNullOrWhiteSpace(pat))
        {
            Assert.Inconclusive(
                "GitHub:PersonalAccessToken not configured. " +
                "Set the GitHub:PersonalAccessToken configuration value to run this test.");
            return;
        }

        var gitHubClient = TestHost.GetRequiredService<IGitHubProjectClient>();
        if (!gitHubClient.IsConfigured)
        {
            Assert.Inconclusive(
                "GitHub health check configuration incomplete. " +
                "Set GitHub:HealthCheck:ProjectId, ItemId, StatusFieldId, and StatusOptionId.");
            return;
        }

        var tracker = TestHost.GetRequiredService<IWebhookReceiptTracker>();
        var logger = TestHost.GetRequiredService<ILogger<GitHubWebhookHealthCheck>>();
        var healthCheck = new GitHubWebhookHealthCheck(gitHubClient, tracker, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("GitHubWebhook", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy,
            $"GitHub webhook round-trip failed: {result.Description}");
    }

    [Test]
    public async Task CheckHealthAsync_WithoutToken_ReturnsDegraded()
    {
        var stubClient = new StubUnconfiguredGitHubClient();
        var tracker = new WebhookReceiptTracker();
        var logger = TestHost.GetRequiredService<ILogger<GitHubWebhookHealthCheck>>();
        var healthCheck = new GitHubWebhookHealthCheck(stubClient, tracker, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("GitHubWebhook", healthCheck, null, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("not configured");
    }

    private class StubUnconfiguredGitHubClient : IGitHubProjectClient
    {
        public bool IsConfigured => false;

        public Task<string> TriggerHealthCheckWebhookAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Should not be called when not configured");
        }
    }
}
