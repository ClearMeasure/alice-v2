using ClearMeasure.Bootcamp.Core.Model.Factory;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries.Factory;

/// <summary>
/// Query for work items that have not changed status within a given threshold
/// </summary>
public record StuckWorkItemsQuery(
    TimeSpan StalenessThreshold,
    string? StatusFilter = null,
    string? WorkItemTypeFilter = null) : IRequest<IEnumerable<FactoryWorkItem>>, IRemotableRequest;
