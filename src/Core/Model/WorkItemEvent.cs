namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Records a single event in the lifecycle of a work item on a board.
/// </summary>
public class WorkItemEvent : EntityBase<WorkItemEvent>
{
    public WorkItemEvent()
    {
        WorkItemExternalId = string.Empty;
        Source = string.Empty;
        EventType = string.Empty;
        NewStatus = string.Empty;
        RawPayload = string.Empty;
    }

    public WorkItemEvent(
        string workItemExternalId,
        string source,
        string eventType,
        string? previousStatus,
        string newStatus,
        DateTimeOffset occurredAtUtc,
        string rawPayload)
    {
        WorkItemExternalId = workItemExternalId;
        Source = source;
        EventType = eventType;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        OccurredAtUtc = occurredAtUtc;
        ReceivedAtUtc = DateTimeOffset.UtcNow;
        RawPayload = rawPayload;
    }

    public override Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Identifier of the work item in the external system.</summary>
    public string WorkItemExternalId { get; set; }

    /// <summary>Origin system (e.g. "GitHub", "AzureDevOps").</summary>
    public string Source { get; set; }

    /// <summary>Type of event (e.g. "StatusChanged", "ColumnMoved", "Created").</summary>
    public string EventType { get; set; }

    /// <summary>Previous status or column, if applicable.</summary>
    public string? PreviousStatus { get; set; }

    /// <summary>New status or column after the event.</summary>
    public string NewStatus { get; set; }

    /// <summary>When the event occurred in the source system.</summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>When the webhook was received.</summary>
    public DateTimeOffset ReceivedAtUtc { get; set; }

    /// <summary>Original webhook payload for audit purposes.</summary>
    public string RawPayload { get; set; }
}
