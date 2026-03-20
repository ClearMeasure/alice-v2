using ClearMeasure.Bootcamp.Core.Commands;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class RecordWorkItemEventCommandHandlerTests
{
    [Test]
    public async Task Handle_NewWorkItem_CreatesEventAndState()
    {
        new DatabaseTests().Clean();

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new RecordWorkItemEventCommandHandler(dataContext);

        var command = new RecordWorkItemEventCommand(
            WorkItemExternalId: "PVTI_100",
            Source: "GitHub",
            EventType: "Created",
            PreviousStatus: null,
            NewStatus: "Todo",
            Title: "Fix login bug",
            ProjectName: "my-org",
            OccurredAtUtc: DateTimeOffset.UtcNow,
            RawPayload: "{\"action\":\"created\"}");

        var eventId = await handler.Handle(command);

        eventId.ShouldNotBe(Guid.Empty);

        using var verifyContext = TestHost.GetRequiredService<DataContext>();
        var savedEvent = await verifyContext.Set<WorkItemEvent>()
            .SingleAsync(e => e.Id == eventId);
        savedEvent.WorkItemExternalId.ShouldBe("PVTI_100");
        savedEvent.Source.ShouldBe("GitHub");
        savedEvent.EventType.ShouldBe("Created");
        savedEvent.NewStatus.ShouldBe("Todo");

        var savedState = await verifyContext.Set<WorkItemState>()
            .SingleAsync(s => s.ExternalId == "PVTI_100" && s.Source == "GitHub");
        savedState.Title.ShouldBe("Fix login bug");
        savedState.CurrentStatus.ShouldBe("Todo");
        savedState.ProjectName.ShouldBe("my-org");
    }

    [Test]
    public async Task Handle_ExistingWorkItem_UpdatesState()
    {
        new DatabaseTests().Clean();

        var dataContext1 = TestHost.GetRequiredService<DataContext>();
        var handler1 = new RecordWorkItemEventCommandHandler(dataContext1);

        var createCommand = new RecordWorkItemEventCommand(
            WorkItemExternalId: "PVTI_200",
            Source: "GitHub",
            EventType: "Created",
            PreviousStatus: null,
            NewStatus: "Todo",
            Title: "Add feature",
            ProjectName: "my-org",
            OccurredAtUtc: DateTimeOffset.UtcNow.AddMinutes(-10),
            RawPayload: "{}");

        await handler1.Handle(createCommand);

        var dataContext2 = TestHost.GetRequiredService<DataContext>();
        var handler2 = new RecordWorkItemEventCommandHandler(dataContext2);

        var updateCommand = new RecordWorkItemEventCommand(
            WorkItemExternalId: "PVTI_200",
            Source: "GitHub",
            EventType: "StatusChanged",
            PreviousStatus: "Todo",
            NewStatus: "In Progress",
            Title: "Add feature",
            ProjectName: "my-org",
            OccurredAtUtc: DateTimeOffset.UtcNow,
            RawPayload: "{}");

        await handler2.Handle(updateCommand);

        using var verifyContext = TestHost.GetRequiredService<DataContext>();
        var events = await verifyContext.Set<WorkItemEvent>()
            .Where(e => e.WorkItemExternalId == "PVTI_200")
            .OrderBy(e => e.OccurredAtUtc)
            .ToArrayAsync();
        events.Length.ShouldBe(2);

        var state = await verifyContext.Set<WorkItemState>()
            .SingleAsync(s => s.ExternalId == "PVTI_200");
        state.CurrentStatus.ShouldBe("In Progress");
    }
}
