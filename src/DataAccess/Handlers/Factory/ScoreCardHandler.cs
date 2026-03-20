using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.Core.Queries.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Aggregates metrics into weighted composite scores per category
/// </summary>
public class ScoreCardHandler(DataContext context)
    : IRequestHandler<ScoreCardQuery, IEnumerable<CategoryScore>>
{
    public async Task<IEnumerable<CategoryScore>> Handle(
        ScoreCardQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var currentWeekStart = now.AddDays(-(int)now.DayOfWeek);
        var currentWeekEnd = currentWeekStart.AddDays(7);
        var previousWeekStart = currentWeekStart.AddDays(-7);

        var currentSnapshots = await context.Set<DashboardMetricSnapshotEntity>()
            .Where(s => s.PeriodStart >= currentWeekStart && s.PeriodEnd <= currentWeekEnd)
            .ToListAsync(cancellationToken);

        var previousSnapshots = await context.Set<DashboardMetricSnapshotEntity>()
            .Where(s => s.PeriodStart >= previousWeekStart && s.PeriodEnd <= currentWeekStart)
            .ToListAsync(cancellationToken);

        var scores = new List<CategoryScore>();

        foreach (var category in Enum.GetValues<MetricCategory>())
        {
            var categoryName = category.ToString();

            var currentMetrics = currentSnapshots
                .Where(s => s.Category == categoryName)
                .ToList();

            var previousMetrics = previousSnapshots
                .Where(s => s.Category == categoryName)
                .ToList();

            var currentAvg = currentMetrics.Count > 0
                ? currentMetrics.Average(m => m.Value)
                : 0;

            var previousAvg = previousMetrics.Count > 0
                ? previousMetrics.Average(m => m.Value)
                : 0;

            var trend = currentAvg > previousAvg ? TrendDirection.Up
                : currentAvg < previousAvg ? TrendDirection.Down
                : TrendDirection.Stable;

            scores.Add(new CategoryScore(category, currentAvg, trend));
        }

        return scores;
    }
}
