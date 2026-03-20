namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Categories for dashboard metrics
/// </summary>
public enum MetricCategory
{
    /// <summary>
    /// Quality-related metrics (e.g., change failure rate)
    /// </summary>
    Quality,

    /// <summary>
    /// Stability-related metrics (e.g., MTTR)
    /// </summary>
    Stability,

    /// <summary>
    /// Speed-related metrics (e.g., lead time, deployment frequency)
    /// </summary>
    Speed,

    /// <summary>
    /// Leadership and organizational metrics
    /// </summary>
    Leadership
}
