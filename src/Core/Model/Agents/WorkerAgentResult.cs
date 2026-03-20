using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.Core.Model.Agents;

/// <summary>
/// Result of a worker agent execution
/// </summary>
/// <param name="Success">Whether the agent completed successfully</param>
/// <param name="NextStatus">Optional next status to transition the work item to</param>
/// <param name="Summary">Human-readable summary of what the agent did</param>
/// <param name="OutputData">Agent-specific output data</param>
public record WorkerAgentResult(
    bool Success,
    FactoryStatus? NextStatus,
    string Summary,
    Dictionary<string, string> OutputData);
