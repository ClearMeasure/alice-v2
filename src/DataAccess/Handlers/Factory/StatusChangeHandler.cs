using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Handles StatusChanged factory events by updating work item status
/// </summary>
public class StatusChangeHandler(DataContext context) : INotificationHandler<FactoryEvent>
{
    public async Task Handle(FactoryEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EventType != FactoryEventType.StatusChanged)
            return;

        if (!notification.Payload.TryGetValue("NewStatus", out var newStatusCode))
            return;

        var newStatus = FactoryStatus.FromCode(newStatusCode);

        var workItem = await context.Set<FactoryWorkItem>()
            .SingleOrDefaultAsync(w =>
                w.ExternalId == notification.ExternalId &&
                w.ExternalSystem == notification.ExternalSystem, cancellationToken);

        if (workItem == null)
        {
            notification.Payload.TryGetValue("Title", out var title);
            notification.Payload.TryGetValue("WorkItemType", out var workItemType);

            workItem = new FactoryWorkItem
            {
                ExternalId = notification.ExternalId,
                ExternalSystem = notification.ExternalSystem,
                Title = title ?? string.Empty,
                WorkItemType = workItemType != null ? WorkItemType.FromCode(workItemType) : WorkItemType.Feature,
                CurrentStatus = FactoryStatus.Conceptual,
                CreatedDate = notification.OccurredAt,
                LastStatusChangeDate = notification.OccurredAt
            };
            context.Set<FactoryWorkItem>().Add(workItem);
        }

        workItem.ChangeStatus(newStatus, notification.OccurredAt);

        var lastTransition = workItem.StatusHistory.Last();
        var transitionEntity = new StatusTransition
        {
            Id = lastTransition.Id,
            FactoryWorkItemId = workItem.Id,
            FromStatus = lastTransition.FromStatus,
            ToStatus = lastTransition.ToStatus,
            TransitionDate = lastTransition.TransitionDate
        };
        context.Set<StatusTransition>().Add(transitionEntity);
        context.Entry(transitionEntity).Property("IsBackward").CurrentValue = lastTransition.IsBackward;

        await context.SaveChangesAsync(cancellationToken);
    }
}
