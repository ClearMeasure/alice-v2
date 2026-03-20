using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkItemQueryHandlerTests
{
    [Test]
    public async Task Handle_StateByExternalId_ReturnsMatchingState()
    {
        new DatabaseTests().Clean();

        var state = new WorkItemState("EXT_1", "GitHub", "Bug fix", "In Progress", "my-org");
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(state);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkItemQueryHandler(dataContext);

        var result = await handler.Handle(new WorkItemStateByExternalIdQuery("EXT_1", "GitHub"));

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Bug fix");
        result.CurrentStatus.ShouldBe("In Progress");
    }

    [Test]
    public async Task Handle_StateByExternalId_ReturnsNull_WhenNotFound()
    {
        new DatabaseTests().Clean();

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkItemQueryHandler(dataContext);

        var result = await handler.Handle(new WorkItemStateByExternalIdQuery("NONE", "GitHub"));

        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_EventsByExternalId_ReturnsOrderedEvents()
    {
        new DatabaseTests().Clean();

        var event1 = new WorkItemEvent("EXT_2", "GitHub", "Created", null, "Todo",
            DateTimeOffset.UtcNow.AddMinutes(-10), "{}");
        var event2 = new WorkItemEvent("EXT_2", "GitHub", "StatusChanged", "Todo", "Done",
            DateTimeOffset.UtcNow, "{}");
        var otherEvent = new WorkItemEvent("EXT_OTHER", "GitHub", "Created", null, "Todo",
            DateTimeOffset.UtcNow, "{}");

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.AddRange(event2, event1, otherEvent);
            context.SaveChanges();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkItemQueryHandler(dataContext);

        var results = await handler.Handle(new WorkItemEventsByExternalIdQuery("EXT_2", "GitHub"));

        results.Length.ShouldBe(2);
        results[0].EventType.ShouldBe("Created");
        results[1].EventType.ShouldBe("StatusChanged");
    }
}
