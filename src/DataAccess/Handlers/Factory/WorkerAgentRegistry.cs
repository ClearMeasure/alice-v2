using ClearMeasure.Bootcamp.Core.Model.Agents;
using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// In-process registry managing worker agent registrations
/// </summary>
public class WorkerAgentRegistry : IWorkerAgentRegistry
{
    private readonly Dictionary<string, List<IWorkerAgent>> _agents = new();

    public void RegisterAgent(IWorkerAgent agent)
    {
        var key = agent.TargetStatus.Code;
        if (!_agents.ContainsKey(key))
            _agents[key] = new List<IWorkerAgent>();
        _agents[key].Add(agent);
    }

    public IEnumerable<IWorkerAgent> GetAgents(FactoryStatus status)
    {
        return _agents.TryGetValue(status.Code, out var agents)
            ? agents
            : Enumerable.Empty<IWorkerAgent>();
    }

    public IEnumerable<(string AgentName, string TargetStatus)> GetAllRegistrations()
    {
        return _agents.SelectMany(kvp =>
            kvp.Value.Select(a => (a.AgentName, kvp.Key)));
    }
}
