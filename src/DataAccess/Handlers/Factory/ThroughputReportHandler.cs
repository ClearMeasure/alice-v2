using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.Core.Queries.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Queries for items reaching terminal statuses within a date range
/// </summary>
public class ThroughputReportHandler(DataContext context)
    : IRequestHandler<ThroughputReportQuery, IEnumerable<ThroughputRecord>>
{
    public async Task<IEnumerable<ThroughputRecord>> Handle(
        ThroughputReportQuery request, CancellationToken cancellationToken)
    {
        var terminalCodes = new[] { "Stable", "Deployed", "Complete" };

        var query = context.Set<StatusTransition>()
            .Where(t => t.TransitionDate >= request.StartDate && t.TransitionDate <= request.EndDate);

        if (request.StatusFilter != null)
            terminalCodes = new[] { request.StatusFilter };

        query = query.Where(t => terminalCodes.Contains(EF.Property<string>(t, "ToStatusCode")));

        var transitions = await query
            .Join(context.Set<FactoryWorkItem>(),
                t => t.FactoryWorkItemId,
                w => w.Id,
                (t, w) => new { Transition = t, WorkItem = w })
            .ToListAsync(cancellationToken);

        var results = transitions;

        if (request.WorkItemTypeFilter != null)
            results = results.Where(r => r.WorkItem.WorkItemType.Code == request.WorkItemTypeFilter).ToList();

        return results.Select(r => new ThroughputRecord(
            r.WorkItem.Title,
            r.WorkItem.ExternalId,
            r.WorkItem.WorkItemType.Code,
            r.Transition.TransitionDate));
    }
}
