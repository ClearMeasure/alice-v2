namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Records a single status change for a factory work item
/// </summary>
public class StatusTransition
{
    /// <summary>
    /// Unique identifier for this transition
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The work item this transition belongs to
    /// </summary>
    public Guid FactoryWorkItemId { get; set; }

    /// <summary>
    /// The status before the transition, null for initial status
    /// </summary>
    public FactoryStatus? FromStatus { get; set; }

    /// <summary>
    /// The status after the transition
    /// </summary>
    public FactoryStatus ToStatus { get; set; } = FactoryStatus.Conceptual;

    /// <summary>
    /// When the transition occurred
    /// </summary>
    public DateTimeOffset TransitionDate { get; set; }

    /// <summary>
    /// Whether the transition moved backward in the pipeline
    /// </summary>
    public bool IsBackward => FromStatus is not null && ToStatus.ProgressionIndex < FromStatus.ProgressionIndex;
}
