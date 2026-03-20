namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Tracks webhook receipts so health checks can verify round-trip connectivity.
/// The webhook controller signals receipt; health checks wait for signals.
/// </summary>
public interface IWebhookReceiptTracker
{
    /// <summary>
    /// Records that a webhook was received from the given source.
    /// </summary>
    void RecordReceipt(string source, string workItemExternalId);

    /// <summary>
    /// Waits for a webhook receipt matching the source.
    /// Returns true if a receipt arrived within the timeout, false otherwise.
    /// </summary>
    Task<bool> WaitForReceiptAsync(string source, TimeSpan timeout, CancellationToken cancellationToken = default);
}
