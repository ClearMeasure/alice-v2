using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.Core.Queries.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Queries for work items exceeding a staleness threshold
/// </summary>
public class StuckWorkItemsHandler(DataContext context)
    : IRequestHandler<StuckWorkItemsQuery, IEnumerable<FactoryWorkItem>>
{
    public async Task<IEnumerable<FactoryWorkItem>> Handle(
        StuckWorkItemsQuery request, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow - request.StalenessThreshold;

        var query = context.Set<FactoryWorkItem>()
            .Where(w => w.LastStatusChangeDate < cutoff);

        if (request.StatusFilter != null)
        {
            var items = await query.ToListAsync(cancellationToken);
            var filtered = items.Where(w => w.CurrentStatus.Code == request.StatusFilter);
            if (request.WorkItemTypeFilter != null)
                filtered = filtered.Where(w => w.WorkItemType.Code == request.WorkItemTypeFilter);
            return filtered;
        }

        var result = await query.ToListAsync(cancellationToken);
        if (request.WorkItemTypeFilter != null)
            return result.Where(w => w.WorkItemType.Code == request.WorkItemTypeFilter);
        return result;
    }
}
