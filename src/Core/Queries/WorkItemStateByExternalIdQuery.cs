using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Retrieves the current state of a work item by its external identifier and source.
/// </summary>
public record WorkItemStateByExternalIdQuery(string ExternalId, string Source)
    : IRequest<WorkItemState?>;
