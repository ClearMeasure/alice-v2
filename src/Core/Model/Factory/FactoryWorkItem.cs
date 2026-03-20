namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Aggregate root tracking a work item through the software delivery pipeline
/// </summary>
public class FactoryWorkItem : EntityBase<FactoryWorkItem>
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public override Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier from the external system
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Source system identifier
    /// </summary>
    public string ExternalSystem { get; set; } = string.Empty;

    /// <summary>
    /// Work item title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Classification of the work item
    /// </summary>
    public WorkItemType WorkItemType { get; set; } = WorkItemType.Feature;

    /// <summary>
    /// Current pipeline status
    /// </summary>
    public FactoryStatus CurrentStatus { get; set; } = FactoryStatus.Conceptual;

    /// <summary>
    /// When the item was first tracked
    /// </summary>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// When the status last changed
    /// </summary>
    public DateTimeOffset LastStatusChangeDate { get; set; }

    /// <summary>
    /// Full timeline of status transitions
    /// </summary>
    public List<StatusTransition> StatusHistory { get; set; } = new();

    /// <summary>
    /// Transitions the work item to a new status and records the transition
    /// </summary>
    public void ChangeStatus(FactoryStatus newStatus, DateTimeOffset timestamp)
    {
        var transition = new StatusTransition
        {
            Id = Guid.NewGuid(),
            FactoryWorkItemId = Id,
            FromStatus = CurrentStatus,
            ToStatus = newStatus,
            TransitionDate = timestamp
        };

        StatusHistory.Add(transition);
        CurrentStatus = newStatus;
        LastStatusChangeDate = timestamp;
    }

    /// <summary>
    /// Returns true if the item has not changed status within the given threshold
    /// </summary>
    public bool IsStuck(TimeSpan threshold)
    {
        return DateTimeOffset.UtcNow - LastStatusChangeDate > threshold;
    }

    /// <summary>
    /// Returns true if any status transition moved backward in the pipeline
    /// </summary>
    public bool HasImplicitDefect()
    {
        return StatusHistory.Any(t => t.IsBackward);
    }
}
