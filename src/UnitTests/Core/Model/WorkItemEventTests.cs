using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkItemEventTests
{
    [Test]
    public void Constructor_WithValues_SetsProperties()
    {
        var occurredAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var workItemEvent = new WorkItemEvent(
            "PVTI_123",
            "GitHub",
            "StatusChanged",
            "Todo",
            "In Progress",
            occurredAt,
            "{\"action\":\"edited\"}");

        workItemEvent.WorkItemExternalId.ShouldBe("PVTI_123");
        workItemEvent.Source.ShouldBe("GitHub");
        workItemEvent.EventType.ShouldBe("StatusChanged");
        workItemEvent.PreviousStatus.ShouldBe("Todo");
        workItemEvent.NewStatus.ShouldBe("In Progress");
        workItemEvent.OccurredAtUtc.ShouldBe(occurredAt);
        workItemEvent.RawPayload.ShouldBe("{\"action\":\"edited\"}");
        workItemEvent.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Constructor_Default_SetsEmptyStrings()
    {
        var workItemEvent = new WorkItemEvent();

        workItemEvent.WorkItemExternalId.ShouldBeEmpty();
        workItemEvent.Source.ShouldBeEmpty();
        workItemEvent.EventType.ShouldBeEmpty();
        workItemEvent.NewStatus.ShouldBeEmpty();
        workItemEvent.RawPayload.ShouldBeEmpty();
        workItemEvent.PreviousStatus.ShouldBeNull();
    }

    [Test]
    public void Equality_WithMatchingIds_IsTrue()
    {
        var event1 = new WorkItemEvent("A", "GitHub", "Created", null, "Todo",
            DateTimeOffset.UtcNow, "{}");
        var event2 = new WorkItemEvent("B", "GitHub", "Deleted", null, "Done",
            DateTimeOffset.UtcNow, "{}") { Id = event1.Id };

        event1.ShouldBe(event2);
    }
}
