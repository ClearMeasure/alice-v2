using ClearMeasure.Bootcamp.Core.Model.Factory;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries.Factory;

/// <summary>
/// Query for the full status history of a single work item
/// </summary>
public record WorkItemTimelineQuery(Guid WorkItemId) : IRequest<IEnumerable<StatusTransition>>, IRemotableRequest;
