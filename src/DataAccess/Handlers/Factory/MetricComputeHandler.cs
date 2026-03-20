using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Computes DORA and Five Pillar metrics from factory events
/// </summary>
public class MetricComputeHandler(DataContext context) : INotificationHandler<FactoryEvent>
{
    public async Task Handle(FactoryEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EventType != FactoryEventType.StatusChanged)
            return;

        if (!notification.Payload.TryGetValue("NewStatus", out var newStatusCode))
            return;

        if (newStatusCode != "Deployed" && newStatusCode != "Stable")
            return;

        var now = DateTimeOffset.UtcNow;
        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        var deploymentCount = await context.Set<StatusTransition>()
            .CountAsync(t =>
                EF.Property<string>(t, "ToStatusCode") == "Deployed" &&
                t.TransitionDate >= weekStart &&
                t.TransitionDate <= weekEnd, cancellationToken);

        var snapshot = new DashboardMetricSnapshotEntity
        {
            MetricName = "DeploymentFrequency",
            Category = "Speed",
            Value = deploymentCount,
            PeriodStart = weekStart,
            PeriodEnd = weekEnd,
            ComputedAt = now
        };

        context.Set<DashboardMetricSnapshotEntity>().Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);
    }
}
