using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.Core.Queries.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Queries dashboard metrics for current and previous week comparison
/// </summary>
public class WeekOverWeekMetricsHandler(DataContext context)
    : IRequestHandler<WeekOverWeekMetricsQuery, IEnumerable<DashboardMetric>>
{
    public async Task<IEnumerable<DashboardMetric>> Handle(
        WeekOverWeekMetricsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var currentWeekStart = now.AddDays(-(int)now.DayOfWeek);
        var previousWeekStart = currentWeekStart.AddDays(-7);
        var currentWeekEnd = currentWeekStart.AddDays(7);

        var currentSnapshots = await context.Set<DashboardMetricSnapshotEntity>()
            .Where(s => s.PeriodStart >= currentWeekStart && s.PeriodEnd <= currentWeekEnd)
            .ToListAsync(cancellationToken);

        var previousSnapshots = await context.Set<DashboardMetricSnapshotEntity>()
            .Where(s => s.PeriodStart >= previousWeekStart && s.PeriodEnd <= currentWeekStart)
            .ToListAsync(cancellationToken);

        var metrics = new List<DashboardMetric>();
        var metricNames = currentSnapshots.Select(s => s.MetricName).Distinct();

        foreach (var name in metricNames)
        {
            var currentValue = currentSnapshots
                .Where(s => s.MetricName == name)
                .OrderByDescending(s => s.ComputedAt)
                .FirstOrDefault()?.Value ?? 0;

            var previousValue = previousSnapshots
                .Where(s => s.MetricName == name)
                .OrderByDescending(s => s.ComputedAt)
                .FirstOrDefault()?.Value ?? 0;

            var trend = currentValue > previousValue ? TrendDirection.Up
                : currentValue < previousValue ? TrendDirection.Down
                : TrendDirection.Stable;

            var categoryStr = currentSnapshots.First(s => s.MetricName == name).Category;
            Enum.TryParse<MetricCategory>(categoryStr, out var category);

            metrics.Add(new DashboardMetric
            {
                MetricName = name,
                Category = category,
                Value = currentValue,
                PeriodStart = currentWeekStart,
                PeriodEnd = currentWeekEnd,
                Trend = trend
            });
        }

        return metrics;
    }
}
