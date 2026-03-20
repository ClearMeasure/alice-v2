namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Represents a computed dashboard metric value for a specific period
/// </summary>
public class DashboardMetric
{
    /// <summary>
    /// Name of the metric
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Category this metric belongs to
    /// </summary>
    public MetricCategory Category { get; set; }

    /// <summary>
    /// Computed metric value
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Start of the measurement period
    /// </summary>
    public DateTimeOffset PeriodStart { get; set; }

    /// <summary>
    /// End of the measurement period
    /// </summary>
    public DateTimeOffset PeriodEnd { get; set; }

    /// <summary>
    /// Trend direction compared to previous period
    /// </summary>
    public TrendDirection Trend { get; set; }
}

/// <summary>
/// Direction of metric trend
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Metric is increasing
    /// </summary>
    Up,

    /// <summary>
    /// Metric is decreasing
    /// </summary>
    Down,

    /// <summary>
    /// Metric is unchanged
    /// </summary>
    Stable
}
