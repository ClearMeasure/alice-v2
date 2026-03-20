using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkItemQueryHandler(DataContext context)
    : IRequestHandler<WorkItemStateByExternalIdQuery, WorkItemState?>,
        IRequestHandler<WorkItemEventsByExternalIdQuery, WorkItemEvent[]>
{
    public async Task<WorkItemState?> Handle(WorkItemStateByExternalIdQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkItemState>()
            .SingleOrDefaultAsync(
                s => s.ExternalId == request.ExternalId && s.Source == request.Source,
                cancellationToken);
    }

    public async Task<WorkItemEvent[]> Handle(WorkItemEventsByExternalIdQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkItemEvent>()
            .Where(e => e.WorkItemExternalId == request.ExternalId && e.Source == request.Source)
            .OrderBy(e => e.OccurredAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
