using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Persists every factory event to the FactoryEvent table
/// </summary>
public class EventLogHandler(DataContext context) : INotificationHandler<FactoryEvent>
{
    public async Task Handle(FactoryEvent notification, CancellationToken cancellationToken)
    {
        var entity = new FactoryEventEntity
        {
            EventTypeCode = notification.EventType.Code,
            FactoryWorkItemId = notification.WorkItemId,
            ExternalId = notification.ExternalId,
            ExternalSystem = notification.ExternalSystem,
            Payload = notification.Payload.Count > 0
                ? JsonSerializer.Serialize(notification.Payload)
                : null,
            OccurredAt = notification.OccurredAt
        };

        context.Set<FactoryEventEntity>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
