using ClearMeasure.Bootcamp.Core.Model.Factory;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Factory;

[TestFixture]
public class FactoryWorkItemTests
{
    [Test]
    public void ShouldChangeStatusAndRecordTransition()
    {
        var workItem = new FactoryWorkItem
        {
            CurrentStatus = FactoryStatus.Conceptual,
            CreatedDate = DateTimeOffset.UtcNow
        };
        var timestamp = DateTimeOffset.UtcNow;

        workItem.ChangeStatus(FactoryStatus.DesignInProgress, timestamp);

        workItem.CurrentStatus.ShouldBe(FactoryStatus.DesignInProgress);
        workItem.LastStatusChangeDate.ShouldBe(timestamp);
        workItem.StatusHistory.Count.ShouldBe(1);
        workItem.StatusHistory[0].FromStatus.ShouldBe(FactoryStatus.Conceptual);
        workItem.StatusHistory[0].ToStatus.ShouldBe(FactoryStatus.DesignInProgress);
    }

    [Test]
    public void ShouldRecordMultipleTransitions()
    {
        var workItem = new FactoryWorkItem { CurrentStatus = FactoryStatus.Conceptual };
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddHours(1);

        workItem.ChangeStatus(FactoryStatus.DesignInProgress, time1);
        workItem.ChangeStatus(FactoryStatus.DesignComplete, time2);

        workItem.StatusHistory.Count.ShouldBe(2);
        workItem.CurrentStatus.ShouldBe(FactoryStatus.DesignComplete);
        workItem.LastStatusChangeDate.ShouldBe(time2);
    }

    [Test]
    public void ShouldDetectBackwardTransition()
    {
        var workItem = new FactoryWorkItem { CurrentStatus = FactoryStatus.DevelopmentComplete };

        workItem.ChangeStatus(FactoryStatus.DevelopmentInProgress, DateTimeOffset.UtcNow);

        workItem.StatusHistory[0].IsBackward.ShouldBeTrue();
    }

    [Test]
    public void ShouldNotDetectBackwardForForwardTransition()
    {
        var workItem = new FactoryWorkItem { CurrentStatus = FactoryStatus.Conceptual };

        workItem.ChangeStatus(FactoryStatus.DesignInProgress, DateTimeOffset.UtcNow);

        workItem.StatusHistory[0].IsBackward.ShouldBeFalse();
    }

    [Test]
    public void ShouldReturnTrueWhenStuck()
    {
        var workItem = new FactoryWorkItem
        {
            LastStatusChangeDate = DateTimeOffset.UtcNow.AddDays(-5)
        };

        workItem.IsStuck(TimeSpan.FromDays(3)).ShouldBeTrue();
    }

    [Test]
    public void ShouldReturnFalseWhenNotStuck()
    {
        var workItem = new FactoryWorkItem
        {
            LastStatusChangeDate = DateTimeOffset.UtcNow.AddHours(-1)
        };

        workItem.IsStuck(TimeSpan.FromDays(3)).ShouldBeFalse();
    }

    [Test]
    public void ShouldDetectImplicitDefect()
    {
        var workItem = new FactoryWorkItem { CurrentStatus = FactoryStatus.ReviewComplete };

        workItem.ChangeStatus(FactoryStatus.DevelopmentInProgress, DateTimeOffset.UtcNow);

        workItem.HasImplicitDefect().ShouldBeTrue();
    }

    [Test]
    public void ShouldNotDetectImplicitDefectForForwardOnly()
    {
        var workItem = new FactoryWorkItem { CurrentStatus = FactoryStatus.Conceptual };

        workItem.ChangeStatus(FactoryStatus.DesignInProgress, DateTimeOffset.UtcNow);
        workItem.ChangeStatus(FactoryStatus.DesignComplete, DateTimeOffset.UtcNow);

        workItem.HasImplicitDefect().ShouldBeFalse();
    }
}
