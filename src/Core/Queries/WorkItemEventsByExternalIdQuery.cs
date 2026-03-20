using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Retrieves the event history for a work item by its external identifier and source.
/// </summary>
public record WorkItemEventsByExternalIdQuery(string ExternalId, string Source)
    : IRequest<WorkItemEvent[]>;
