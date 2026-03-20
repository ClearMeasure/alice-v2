using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Factory;

[TestFixture]
public class FactoryWorkItemPersistenceTests : IntegratedTestBase
{
    [Test]
    public async Task ShouldPersistAndRetrieveFactoryWorkItem()
    {
        var context = TestHost.NewDbContext();
        var workItem = new FactoryWorkItem
        {
            ExternalId = "GH-100",
            ExternalSystem = "github",
            Title = "Test Work Item",
            WorkItemType = WorkItemType.Feature,
            CurrentStatus = FactoryStatus.Conceptual,
            CreatedDate = DateTimeOffset.UtcNow,
            LastStatusChangeDate = DateTimeOffset.UtcNow
        };

        context.Set<FactoryWorkItem>().Add(workItem);
        await context.SaveChangesAsync();

        var freshContext = TestHost.NewDbContext();
        var retrieved = await freshContext.Set<FactoryWorkItem>()
            .SingleOrDefaultAsync(w => w.ExternalId == "GH-100" && w.ExternalSystem == "github");

        retrieved.ShouldNotBeNull();
        retrieved.Title.ShouldBe("Test Work Item");
        retrieved.WorkItemType.ShouldBe(WorkItemType.Feature);
        retrieved.CurrentStatus.ShouldBe(FactoryStatus.Conceptual);
    }

    [Test]
    public async Task ShouldPersistStatusTransition()
    {
        var context = TestHost.NewDbContext();
        var workItem = new FactoryWorkItem
        {
            ExternalId = "GH-200",
            ExternalSystem = "github",
            Title = "Transition Test",
            WorkItemType = WorkItemType.Bug,
            CurrentStatus = FactoryStatus.Conceptual,
            CreatedDate = DateTimeOffset.UtcNow,
            LastStatusChangeDate = DateTimeOffset.UtcNow
        };
        context.Set<FactoryWorkItem>().Add(workItem);
        await context.SaveChangesAsync();

        var transition = new StatusTransition
        {
            FactoryWorkItemId = workItem.Id,
            FromStatus = FactoryStatus.Conceptual,
            ToStatus = FactoryStatus.DesignInProgress,
            TransitionDate = DateTimeOffset.UtcNow
        };
        context.Set<StatusTransition>().Add(transition);
        await context.SaveChangesAsync();

        var freshContext = TestHost.NewDbContext();
        var retrieved = await freshContext.Set<StatusTransition>()
            .Where(t => t.FactoryWorkItemId == workItem.Id)
            .ToListAsync();

        retrieved.Count.ShouldBe(1);
        retrieved[0].ToStatus.ShouldBe(FactoryStatus.DesignInProgress);
    }

    [Test]
    public async Task ShouldPersistFactoryEvent()
    {
        var context = TestHost.NewDbContext();
        var eventEntity = new FactoryEventEntity
        {
            EventTypeCode = "StatusChanged",
            ExternalId = "GH-300",
            ExternalSystem = "github",
            Payload = "{\"NewStatus\":\"DesignInProgress\"}",
            OccurredAt = DateTimeOffset.UtcNow
        };

        context.Set<FactoryEventEntity>().Add(eventEntity);
        await context.SaveChangesAsync();

        var freshContext = TestHost.NewDbContext();
        var retrieved = await freshContext.Set<FactoryEventEntity>()
            .SingleOrDefaultAsync(e => e.ExternalId == "GH-300");

        retrieved.ShouldNotBeNull();
        retrieved.EventTypeCode.ShouldBe("StatusChanged");
    }
}
