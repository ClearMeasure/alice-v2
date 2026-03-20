namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Smart enum representing the pipeline status of a factory work item
/// </summary>
public class FactoryStatus
{
    public static readonly FactoryStatus Conceptual = new("Conceptual", 0);
    public static readonly FactoryStatus DesignInProgress = new("DesignInProgress", 1);
    public static readonly FactoryStatus DesignComplete = new("DesignComplete", 2);
    public static readonly FactoryStatus DevelopmentInProgress = new("DevelopmentInProgress", 3);
    public static readonly FactoryStatus DevelopmentComplete = new("DevelopmentComplete", 4);
    public static readonly FactoryStatus ReviewRequested = new("ReviewRequested", 5);
    public static readonly FactoryStatus ReviewComplete = new("ReviewComplete", 6);
    public static readonly FactoryStatus TestingInProgress = new("TestingInProgress", 7);
    public static readonly FactoryStatus TestingComplete = new("TestingComplete", 8);
    public static readonly FactoryStatus DeploymentInProgress = new("DeploymentInProgress", 9);
    public static readonly FactoryStatus Deployed = new("Deployed", 10);
    public static readonly FactoryStatus StabilizationInProgress = new("StabilizationInProgress", 11);
    public static readonly FactoryStatus Stable = new("Stable", 12);
    public static readonly FactoryStatus Cancelled = new("Cancelled", -1);

    private static readonly Dictionary<string, FactoryStatus> ByCode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, FactoryStatus> ByKey = new();

    static FactoryStatus()
    {
        var all = new[]
        {
            Conceptual, DesignInProgress, DesignComplete, DevelopmentInProgress,
            DevelopmentComplete, ReviewRequested, ReviewComplete, TestingInProgress,
            TestingComplete, DeploymentInProgress, Deployed, StabilizationInProgress,
            Stable, Cancelled
        };

        foreach (var status in all)
        {
            ByCode[status.Code] = status;
            ByKey[status.ProgressionIndex] = status;
        }
    }

    /// <summary>
    /// The string code identifying this status
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Numeric index indicating progression order
    /// </summary>
    public int ProgressionIndex { get; }

    private FactoryStatus(string code, int progressionIndex)
    {
        Code = code;
        ProgressionIndex = progressionIndex;
    }

    /// <summary>
    /// Resolves a FactoryStatus from its string code
    /// </summary>
    public static FactoryStatus FromCode(string code)
    {
        if (ByCode.TryGetValue(code, out var status))
            return status;
        throw new ArgumentException($"Unknown FactoryStatus code: {code}", nameof(code));
    }

    /// <summary>
    /// Resolves a FactoryStatus from its progression index key
    /// </summary>
    public static FactoryStatus FromKey(int key)
    {
        if (ByKey.TryGetValue(key, out var status))
            return status;
        throw new ArgumentException($"Unknown FactoryStatus key: {key}", nameof(key));
    }

    public override string ToString() => Code;

    public override bool Equals(object? obj) => obj is FactoryStatus other && Code == other.Code;

    public override int GetHashCode() => Code.GetHashCode();

    public static bool operator ==(FactoryStatus? a, FactoryStatus? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Code == b.Code;
    }

    public static bool operator !=(FactoryStatus? a, FactoryStatus? b) => !(a == b);
}
