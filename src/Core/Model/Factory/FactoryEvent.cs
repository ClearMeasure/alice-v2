using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Notification representing an event from the software delivery pipeline
/// </summary>
public class FactoryEvent : INotification
{
    /// <summary>
    /// The type of event
    /// </summary>
    public FactoryEventType EventType { get; set; } = FactoryEventType.StatusChanged;

    /// <summary>
    /// Associated work item identifier, if applicable
    /// </summary>
    public Guid? WorkItemId { get; set; }

    /// <summary>
    /// External reference identifier
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Source system identifier
    /// </summary>
    public string ExternalSystem { get; set; } = string.Empty;

    /// <summary>
    /// Event-specific key-value data
    /// </summary>
    public Dictionary<string, string> Payload { get; set; } = new();

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }
}
