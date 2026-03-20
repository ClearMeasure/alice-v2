namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Abstraction for interacting with GitHub Projects V2 boards.
/// Used by health checks to trigger round-trip webhook verification.
/// </summary>
public interface IGitHubProjectClient
{
    /// <summary>Whether the client is configured with a valid personal access token.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Updates the status field of a known health-check item on a GitHub Projects V2 board,
    /// triggering a webhook back to the application.
    /// Returns a correlation identifier embedded in the update for matching the webhook receipt.
    /// </summary>
    Task<string> TriggerHealthCheckWebhookAsync(CancellationToken cancellationToken = default);
}
