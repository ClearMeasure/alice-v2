using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkItemStateTests
{
    [Test]
    public void Constructor_WithValues_SetsProperties()
    {
        var state = new WorkItemState(
            "PVTI_123",
            "GitHub",
            "Fix login bug",
            "In Progress",
            "my-org");

        state.ExternalId.ShouldBe("PVTI_123");
        state.Source.ShouldBe("GitHub");
        state.Title.ShouldBe("Fix login bug");
        state.CurrentStatus.ShouldBe("In Progress");
        state.ProjectName.ShouldBe("my-org");
        state.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Constructor_Default_SetsEmptyStrings()
    {
        var state = new WorkItemState();

        state.ExternalId.ShouldBeEmpty();
        state.Source.ShouldBeEmpty();
        state.Title.ShouldBeEmpty();
        state.CurrentStatus.ShouldBeEmpty();
        state.ProjectName.ShouldBeEmpty();
    }

    [Test]
    public void Equality_WithMatchingIds_IsTrue()
    {
        var state1 = new WorkItemState("A", "GitHub", "Title1", "Todo", "Proj1");
        var state2 = new WorkItemState("B", "GitHub", "Title2", "Done", "Proj2") { Id = state1.Id };

        state1.ShouldBe(state2);
    }
}
