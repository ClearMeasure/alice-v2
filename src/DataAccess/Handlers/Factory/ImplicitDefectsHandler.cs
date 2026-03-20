using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.Core.Queries.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Queries for work items with backward status transitions
/// </summary>
public class ImplicitDefectsHandler(DataContext context)
    : IRequestHandler<ImplicitDefectsQuery, IEnumerable<FactoryWorkItem>>
{
    public async Task<IEnumerable<FactoryWorkItem>> Handle(
        ImplicitDefectsQuery request, CancellationToken cancellationToken)
    {
        var backwardTransitionWorkItemIds = await context.Set<StatusTransition>()
            .Where(t => t.TransitionDate >= request.StartDate && t.TransitionDate <= request.EndDate)
            .Where(t => EF.Property<bool>(t, "IsBackward"))
            .Select(t => t.FactoryWorkItemId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = context.Set<FactoryWorkItem>()
            .Where(w => backwardTransitionWorkItemIds.Contains(w.Id));

        if (request.WorkItemTypeFilter != null)
        {
            var items = await query.ToListAsync(cancellationToken);
            return items.Where(w => w.WorkItemType.Code == request.WorkItemTypeFilter);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
