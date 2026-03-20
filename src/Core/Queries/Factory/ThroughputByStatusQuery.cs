using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries.Factory;

/// <summary>
/// Result record for average time-in-status per status
/// </summary>
public record StatusDurationRecord(string StatusCode, TimeSpan AverageDuration, int ItemCount);

/// <summary>
/// Query for average time spent in each status within a date range
/// </summary>
public record ThroughputByStatusQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? WorkItemTypeFilter = null) : IRequest<IEnumerable<StatusDurationRecord>>, IRemotableRequest;
