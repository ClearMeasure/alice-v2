using ClearMeasure.Bootcamp.Core.Model.Factory;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Factory;

[TestFixture]
public class FactoryStatusTests
{
    [Test]
    public void ShouldResolveFromCode()
    {
        FactoryStatus.FromCode("Conceptual").ShouldBe(FactoryStatus.Conceptual);
        FactoryStatus.FromCode("Deployed").ShouldBe(FactoryStatus.Deployed);
        FactoryStatus.FromCode("Cancelled").ShouldBe(FactoryStatus.Cancelled);
    }

    [Test]
    public void ShouldResolveFromCodeCaseInsensitive()
    {
        FactoryStatus.FromCode("conceptual").ShouldBe(FactoryStatus.Conceptual);
        FactoryStatus.FromCode("DEPLOYED").ShouldBe(FactoryStatus.Deployed);
    }

    [Test]
    public void ShouldResolveFromKey()
    {
        FactoryStatus.FromKey(0).ShouldBe(FactoryStatus.Conceptual);
        FactoryStatus.FromKey(10).ShouldBe(FactoryStatus.Deployed);
        FactoryStatus.FromKey(-1).ShouldBe(FactoryStatus.Cancelled);
    }

    [Test]
    public void ShouldThrowForUnknownCode()
    {
        Should.Throw<ArgumentException>(() => FactoryStatus.FromCode("Unknown"));
    }

    [Test]
    public void ShouldThrowForUnknownKey()
    {
        Should.Throw<ArgumentException>(() => FactoryStatus.FromKey(999));
    }

    [Test]
    public void ShouldHaveCorrectProgressionOrdering()
    {
        FactoryStatus.Conceptual.ProgressionIndex.ShouldBeLessThan(FactoryStatus.DesignInProgress.ProgressionIndex);
        FactoryStatus.DesignComplete.ProgressionIndex.ShouldBeLessThan(FactoryStatus.DevelopmentInProgress.ProgressionIndex);
        FactoryStatus.Deployed.ProgressionIndex.ShouldBeLessThan(FactoryStatus.Stable.ProgressionIndex);
        FactoryStatus.Cancelled.ProgressionIndex.ShouldBeLessThan(FactoryStatus.Conceptual.ProgressionIndex);
    }
}
