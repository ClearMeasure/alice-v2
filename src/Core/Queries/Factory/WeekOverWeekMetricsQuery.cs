using ClearMeasure.Bootcamp.Core.Model.Factory;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries.Factory;

/// <summary>
/// Query for current vs previous week metrics
/// </summary>
public record WeekOverWeekMetricsQuery : IRequest<IEnumerable<DashboardMetric>>, IRemotableRequest;
