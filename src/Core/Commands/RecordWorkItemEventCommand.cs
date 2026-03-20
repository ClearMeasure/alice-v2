using MediatR;

namespace ClearMeasure.Bootcamp.Core.Commands;

/// <summary>
/// Records a work item event and updates the current state of the work item.
/// </summary>
public record RecordWorkItemEventCommand(
    string WorkItemExternalId,
    string Source,
    string EventType,
    string? PreviousStatus,
    string NewStatus,
    string Title,
    string ProjectName,
    DateTimeOffset OccurredAtUtc,
    string RawPayload) : IRequest<Guid>;
