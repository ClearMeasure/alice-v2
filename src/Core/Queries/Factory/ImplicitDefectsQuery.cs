using ClearMeasure.Bootcamp.Core.Model.Factory;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries.Factory;

/// <summary>
/// Query for work items with backward status transitions within a date range
/// </summary>
public record ImplicitDefectsQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? WorkItemTypeFilter = null) : IRequest<IEnumerable<FactoryWorkItem>>, IRemotableRequest;
