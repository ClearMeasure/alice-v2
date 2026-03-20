using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries.Factory;

/// <summary>
/// Result record for throughput report entries
/// </summary>
public record ThroughputRecord(string WorkItemTitle, string ExternalId, string WorkItemType, DateTimeOffset CompletedDate);

/// <summary>
/// Query for work items completed within a date range
/// </summary>
public record ThroughputReportQuery(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? StatusFilter = null,
    string? WorkItemTypeFilter = null) : IRequest<IEnumerable<ThroughputRecord>>, IRemotableRequest;
