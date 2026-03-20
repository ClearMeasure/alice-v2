using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.Core.Queries.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Computes average duration per status from StatusTransition timestamps
/// </summary>
public class ThroughputByStatusHandler(DataContext context)
    : IRequestHandler<ThroughputByStatusQuery, IEnumerable<StatusDurationRecord>>
{
    public async Task<IEnumerable<StatusDurationRecord>> Handle(
        ThroughputByStatusQuery request, CancellationToken cancellationToken)
    {
        var transitions = await context.Set<StatusTransition>()
            .Where(t => t.TransitionDate >= request.StartDate && t.TransitionDate <= request.EndDate)
            .OrderBy(t => t.FactoryWorkItemId)
            .ThenBy(t => t.TransitionDate)
            .ToListAsync(cancellationToken);

        if (request.WorkItemTypeFilter != null)
        {
            var workItemIds = await context.Set<FactoryWorkItem>()
                .Where(w => w.WorkItemType == WorkItemType.FromCode(request.WorkItemTypeFilter))
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);
            transitions = transitions.Where(t => workItemIds.Contains(t.FactoryWorkItemId)).ToList();
        }

        var grouped = transitions
            .GroupBy(t => t.FactoryWorkItemId)
            .SelectMany(g =>
            {
                var ordered = g.OrderBy(t => t.TransitionDate).ToList();
                var durations = new List<(string StatusCode, TimeSpan Duration)>();

                for (var i = 0; i < ordered.Count - 1; i++)
                {
                    var fromCode = ordered[i].ToStatus.Code;
                    var duration = ordered[i + 1].TransitionDate - ordered[i].TransitionDate;
                    durations.Add((fromCode, duration));
                }

                return durations;
            })
            .GroupBy(d => d.StatusCode)
            .Select(g => new StatusDurationRecord(
                g.Key,
                TimeSpan.FromTicks((long)g.Average(d => d.Duration.Ticks)),
                g.Count()));

        return grouped;
    }
}
