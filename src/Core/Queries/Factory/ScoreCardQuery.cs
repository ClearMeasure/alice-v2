using ClearMeasure.Bootcamp.Core.Model.Factory;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries.Factory;

/// <summary>
/// Result record for a category score
/// </summary>
public record CategoryScore(MetricCategory Category, decimal Score, TrendDirection Trend);

/// <summary>
/// Query for composite scorecard across all metric categories
/// </summary>
public record ScoreCardQuery : IRequest<IEnumerable<CategoryScore>>, IRemotableRequest;
