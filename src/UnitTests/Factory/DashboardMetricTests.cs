using ClearMeasure.Bootcamp.Core.Model.Factory;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Factory;

[TestFixture]
public class DashboardMetricTests
{
    [Test]
    public void ShouldCreateMetricWithTrendUp()
    {
        var metric = new DashboardMetric
        {
            MetricName = "DeploymentFrequency",
            Category = MetricCategory.Speed,
            Value = 5.0m,
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-7),
            PeriodEnd = DateTimeOffset.UtcNow,
            Trend = TrendDirection.Up
        };

        metric.MetricName.ShouldBe("DeploymentFrequency");
        metric.Category.ShouldBe(MetricCategory.Speed);
        metric.Value.ShouldBe(5.0m);
        metric.Trend.ShouldBe(TrendDirection.Up);
    }

    [Test]
    public void ShouldHaveAllMetricCategories()
    {
        var categories = Enum.GetValues<MetricCategory>();

        categories.Length.ShouldBe(4);
        categories.ShouldContain(MetricCategory.Quality);
        categories.ShouldContain(MetricCategory.Stability);
        categories.ShouldContain(MetricCategory.Speed);
        categories.ShouldContain(MetricCategory.Leadership);
    }

    [Test]
    public void ShouldHaveAllTrendDirections()
    {
        var trends = Enum.GetValues<TrendDirection>();

        trends.Length.ShouldBe(3);
        trends.ShouldContain(TrendDirection.Up);
        trends.ShouldContain(TrendDirection.Down);
        trends.ShouldContain(TrendDirection.Stable);
    }
}
