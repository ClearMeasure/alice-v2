using ClearMeasure.Bootcamp.Core.Commands;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class RecordWorkItemEventCommandHandler(DataContext context)
    : IRequestHandler<RecordWorkItemEventCommand, Guid>
{
    public async Task<Guid> Handle(RecordWorkItemEventCommand request,
        CancellationToken cancellationToken = default)
    {
        var workItemEvent = new WorkItemEvent(
            request.WorkItemExternalId,
            request.Source,
            request.EventType,
            request.PreviousStatus,
            request.NewStatus,
            request.OccurredAtUtc,
            request.RawPayload);

        context.Set<WorkItemEvent>().Add(workItemEvent);

        var existingState = await context.Set<WorkItemState>()
            .SingleOrDefaultAsync(
                s => s.ExternalId == request.WorkItemExternalId && s.Source == request.Source,
                cancellationToken);

        if (existingState is null)
        {
            var newState = new WorkItemState(
                request.WorkItemExternalId,
                request.Source,
                request.Title,
                request.NewStatus,
                request.ProjectName);
            context.Set<WorkItemState>().Add(newState);
        }
        else
        {
            existingState.CurrentStatus = request.NewStatus;
            existingState.Title = request.Title;
            existingState.ProjectName = request.ProjectName;
            existingState.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return workItemEvent.Id;
    }
}
