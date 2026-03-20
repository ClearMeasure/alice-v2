using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.Agents;
using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Dispatches registered worker agents when a work item changes status
/// </summary>
public class AgentDispatchHandler(
    DataContext context,
    IWorkerAgentRegistry registry,
    IBus bus,
    ILogger<AgentDispatchHandler> logger) : INotificationHandler<FactoryEvent>
{
    public async Task Handle(FactoryEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EventType != FactoryEventType.StatusChanged)
            return;

        if (!notification.Payload.TryGetValue("NewStatus", out var newStatusCode))
            return;

        var newStatus = FactoryStatus.FromCode(newStatusCode);
        var agents = registry.GetAgents(newStatus);

        foreach (var agent in agents)
        {
            var workItem = notification.WorkItemId.HasValue
                ? await context.Set<FactoryWorkItem>()
                    .SingleOrDefaultAsync(w => w.Id == notification.WorkItemId.Value, cancellationToken)
                : await context.Set<FactoryWorkItem>()
                    .SingleOrDefaultAsync(w =>
                        w.ExternalId == notification.ExternalId &&
                        w.ExternalSystem == notification.ExternalSystem, cancellationToken);

            if (workItem == null)
                continue;

            var logEntry = new WorkerAgentExecutionLogEntity
            {
                AgentName = agent.AgentName,
                FactoryWorkItemId = workItem.Id,
                StartedAt = DateTimeOffset.UtcNow
            };

            try
            {
                var result = await agent.ExecuteAsync(workItem, cancellationToken);

                logEntry.CompletedAt = DateTimeOffset.UtcNow;
                logEntry.Success = result.Success;
                logEntry.Summary = result.Summary;
                logEntry.OutputData = result.OutputData.Count > 0
                    ? JsonSerializer.Serialize(result.OutputData)
                    : null;

                if (result.Success && result.NextStatus != null)
                {
                    await bus.Publish(new FactoryEvent
                    {
                        EventType = FactoryEventType.StatusChanged,
                        WorkItemId = workItem.Id,
                        ExternalId = workItem.ExternalId,
                        ExternalSystem = workItem.ExternalSystem,
                        Payload = new Dictionary<string, string>
                        {
                            ["NewStatus"] = result.NextStatus.Code,
                            ["Source"] = $"Agent:{agent.AgentName}"
                        },
                        OccurredAt = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Agent {AgentName} failed for work item {WorkItemId}", agent.AgentName, workItem.Id);
                logEntry.CompletedAt = DateTimeOffset.UtcNow;
                logEntry.Success = false;
                logEntry.Summary = ex.Message;
            }
        }
    }
}

/// <summary>
/// Persistence entity for worker agent execution logs
/// </summary>
public class WorkerAgentExecutionLogEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AgentName { get; set; } = string.Empty;
    public Guid FactoryWorkItemId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool? Success { get; set; }
    public string? Summary { get; set; }
    public string? OutputData { get; set; }
}
