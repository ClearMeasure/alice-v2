using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Api.Webhooks;

/// <summary>
/// In-memory tracker for webhook receipts. Registered as singleton so health checks
/// and the webhook controller share the same instance.
/// Uses source-keyed semaphores to signal receipt.
/// </summary>
public class WebhookReceiptTracker : IWebhookReceiptTracker
{
    private readonly object _lock = new();
    private readonly Dictionary<string, SemaphoreSlim> _signals = new(StringComparer.OrdinalIgnoreCase);

    public void RecordReceipt(string source, string workItemExternalId)
    {
        var signal = GetOrCreateSignal(source);
        signal.Release();
    }

    public async Task<bool> WaitForReceiptAsync(string source, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var signal = GetOrCreateSignal(source);

        // Drain any previously recorded receipts before waiting
        while (signal.CurrentCount > 0)
        {
            await signal.WaitAsync(TimeSpan.Zero, cancellationToken);
        }

        // Now wait for a fresh receipt
        return await signal.WaitAsync(timeout, cancellationToken);
    }

    private SemaphoreSlim GetOrCreateSignal(string source)
    {
        lock (_lock)
        {
            if (!_signals.TryGetValue(source, out var signal))
            {
                signal = new SemaphoreSlim(0);
                _signals[source] = signal;
            }

            return signal;
        }
    }
}
