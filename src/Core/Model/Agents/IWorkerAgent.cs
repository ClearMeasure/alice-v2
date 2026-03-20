using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.Core.Model.Agents;

/// <summary>
/// Interface for worker agents that process work items at specific pipeline statuses
/// </summary>
public interface IWorkerAgent
{
    /// <summary>
    /// Display name of the agent
    /// </summary>
    string AgentName { get; }

    /// <summary>
    /// The pipeline status this agent targets
    /// </summary>
    FactoryStatus TargetStatus { get; }

    /// <summary>
    /// Executes the agent against a work item
    /// </summary>
    Task<WorkerAgentResult> ExecuteAsync(FactoryWorkItem workItem, CancellationToken cancellationToken);
}
