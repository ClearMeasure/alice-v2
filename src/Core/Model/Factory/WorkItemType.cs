namespace ClearMeasure.Bootcamp.Core.Model.Factory;

/// <summary>
/// Smart enum representing the type of a factory work item
/// </summary>
public class WorkItemType
{
    public static readonly WorkItemType Feature = new("Feature");
    public static readonly WorkItemType Bug = new("Bug");
    public static readonly WorkItemType Chore = new("Chore");
    public static readonly WorkItemType Spike = new("Spike");
    public static readonly WorkItemType Hotfix = new("Hotfix");

    private static readonly Dictionary<string, WorkItemType> ByCode = new(StringComparer.OrdinalIgnoreCase);

    static WorkItemType()
    {
        var all = new[] { Feature, Bug, Chore, Spike, Hotfix };
        foreach (var type in all)
            ByCode[type.Code] = type;
    }

    /// <summary>
    /// The string code identifying this work item type
    /// </summary>
    public string Code { get; }

    private WorkItemType(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Resolves a WorkItemType from its string code
    /// </summary>
    public static WorkItemType FromCode(string code)
    {
        if (ByCode.TryGetValue(code, out var type))
            return type;
        throw new ArgumentException($"Unknown WorkItemType code: {code}", nameof(code));
    }

    /// <summary>
    /// Resolves a WorkItemType from its string key
    /// </summary>
    public static WorkItemType FromKey(string key) => FromCode(key);

    public override string ToString() => Code;

    public override bool Equals(object? obj) => obj is WorkItemType other && Code == other.Code;

    public override int GetHashCode() => Code.GetHashCode();

    public static bool operator ==(WorkItemType? a, WorkItemType? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Code == b.Code;
    }

    public static bool operator !=(WorkItemType? a, WorkItemType? b) => !(a == b);
}
