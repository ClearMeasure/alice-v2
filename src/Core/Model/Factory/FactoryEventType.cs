namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Smart enum representing the type of a factory event
/// </summary>
public class FactoryEventType
{
    public static readonly FactoryEventType StatusChanged = new("StatusChanged");
    public static readonly FactoryEventType BuildSucceeded = new("BuildSucceeded");
    public static readonly FactoryEventType BuildFailed = new("BuildFailed");
    public static readonly FactoryEventType PullRequestOpened = new("PullRequestOpened");
    public static readonly FactoryEventType PullRequestMerged = new("PullRequestMerged");
    public static readonly FactoryEventType DeploymentStarted = new("DeploymentStarted");
    public static readonly FactoryEventType DeploymentCompleted = new("DeploymentCompleted");
    public static readonly FactoryEventType DeploymentFailed = new("DeploymentFailed");
    public static readonly FactoryEventType TestsPassed = new("TestsPassed");
    public static readonly FactoryEventType TestsFailed = new("TestsFailed");

    private static readonly Dictionary<string, FactoryEventType> ByCode = new(StringComparer.OrdinalIgnoreCase);

    static FactoryEventType()
    {
        var all = new[]
        {
            StatusChanged, BuildSucceeded, BuildFailed, PullRequestOpened,
            PullRequestMerged, DeploymentStarted, DeploymentCompleted,
            DeploymentFailed, TestsPassed, TestsFailed
        };

        foreach (var type in all)
            ByCode[type.Code] = type;
    }

    /// <summary>
    /// The string code identifying this event type
    /// </summary>
    public string Code { get; }

    private FactoryEventType(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Resolves a FactoryEventType from its string code
    /// </summary>
    public static FactoryEventType FromCode(string code)
    {
        if (ByCode.TryGetValue(code, out var type))
            return type;
        throw new ArgumentException($"Unknown FactoryEventType code: {code}", nameof(code));
    }

    /// <summary>
    /// Resolves a FactoryEventType from its string key
    /// </summary>
    public static FactoryEventType FromKey(string key) => FromCode(key);

    public override string ToString() => Code;

    public override bool Equals(object? obj) => obj is FactoryEventType other && Code == other.Code;

    public override int GetHashCode() => Code.GetHashCode();

    public static bool operator ==(FactoryEventType? a, FactoryEventType? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Code == b.Code;
    }

    public static bool operator !=(FactoryEventType? a, FactoryEventType? b) => !(a == b);
}
