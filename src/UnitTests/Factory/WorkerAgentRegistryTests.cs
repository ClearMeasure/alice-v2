using ClearMeasure.Bootcamp.Core.Model.Agents;
using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Factory;

[TestFixture]
public class WorkerAgentRegistryTests
{
    [Test]
    public void ShouldRegisterAndRetrieveAgent()
    {
        var registry = new WorkerAgentRegistry();
        var agent = new StubWorkerAgent("TestAgent", FactoryStatus.DesignComplete);

        registry.RegisterAgent(agent);

        var agents = registry.GetAgents(FactoryStatus.DesignComplete).ToList();
        agents.Count.ShouldBe(1);
        agents[0].AgentName.ShouldBe("TestAgent");
    }

    [Test]
    public void ShouldReturnEmptyForUnregisteredStatus()
    {
        var registry = new WorkerAgentRegistry();

        var agents = registry.GetAgents(FactoryStatus.Conceptual).ToList();

        agents.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRegisterMultipleAgentsForSameStatus()
    {
        var registry = new WorkerAgentRegistry();
        registry.RegisterAgent(new StubWorkerAgent("Agent1", FactoryStatus.ReviewRequested));
        registry.RegisterAgent(new StubWorkerAgent("Agent2", FactoryStatus.ReviewRequested));

        var agents = registry.GetAgents(FactoryStatus.ReviewRequested).ToList();

        agents.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldListAllRegistrations()
    {
        var registry = new WorkerAgentRegistry();
        registry.RegisterAgent(new StubWorkerAgent("Agent1", FactoryStatus.DesignComplete));
        registry.RegisterAgent(new StubWorkerAgent("Agent2", FactoryStatus.ReviewRequested));

        var registrations = registry.GetAllRegistrations().ToList();

        registrations.Count.ShouldBe(2);
    }

    private class StubWorkerAgent(string name, FactoryStatus targetStatus) : IWorkerAgent
    {
        public string AgentName => name;
        public FactoryStatus TargetStatus => targetStatus;

        public Task<WorkerAgentResult> ExecuteAsync(FactoryWorkItem workItem, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WorkerAgentResult(true, null, "Stub execution", new Dictionary<string, string>()));
        }
    }
}
