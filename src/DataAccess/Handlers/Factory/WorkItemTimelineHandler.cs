using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.Core.Queries.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Returns ordered StatusTransition list for a given work item
/// </summary>
public class WorkItemTimelineHandler(DataContext context)
    : IRequestHandler<WorkItemTimelineQuery, IEnumerable<StatusTransition>>
{
    public async Task<IEnumerable<StatusTransition>> Handle(
        WorkItemTimelineQuery request, CancellationToken cancellationToken)
    {
        return await context.Set<StatusTransition>()
            .Where(t => t.FactoryWorkItemId == request.WorkItemId)
            .OrderBy(t => t.TransitionDate)
            .ToListAsync(cancellationToken);
    }
}
