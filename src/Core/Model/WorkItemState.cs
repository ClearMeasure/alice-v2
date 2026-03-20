namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Current state of a tracked work item on a board.
/// </summary>
public class WorkItemState : EntityBase<WorkItemState>
{
    public WorkItemState()
    {
        ExternalId = string.Empty;
        Source = string.Empty;
        Title = string.Empty;
        CurrentStatus = string.Empty;
        ProjectName = string.Empty;
    }

    public WorkItemState(
        string externalId,
        string source,
        string title,
        string currentStatus,
        string projectName)
    {
        ExternalId = externalId;
        Source = source;
        Title = title;
        CurrentStatus = currentStatus;
        ProjectName = projectName;
        LastUpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public override Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Identifier of the work item in the external system.</summary>
    public string ExternalId { get; set; }

    /// <summary>Origin system (e.g. "GitHub", "AzureDevOps").</summary>
    public string Source { get; set; }

    /// <summary>Display title of the work item.</summary>
    public string Title { get; set; }

    /// <summary>Current board column or status.</summary>
    public string CurrentStatus { get; set; }

    /// <summary>Name of the project or board.</summary>
    public string ProjectName { get; set; }

    /// <summary>When the state was last updated.</summary>
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
