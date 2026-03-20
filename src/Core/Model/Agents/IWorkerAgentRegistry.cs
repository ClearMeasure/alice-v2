using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.Core.Model.Agents;

/// <summary>
/// Registry for managing worker agent registrations
/// </summary>
public interface IWorkerAgentRegistry
{
    /// <summary>
    /// Registers an agent for its target status
    /// </summary>
    void RegisterAgent(IWorkerAgent agent);

    /// <summary>
    /// Gets all agents registered for the given status
    /// </summary>
    IEnumerable<IWorkerAgent> GetAgents(FactoryStatus status);

    /// <summary>
    /// Gets all registered agent names and their target statuses
    /// </summary>
    IEnumerable<(string AgentName, string TargetStatus)> GetAllRegistrations();
}
