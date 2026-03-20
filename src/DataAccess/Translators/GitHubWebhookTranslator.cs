using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Interfaces;
using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.DataAccess.Translators;

/// <summary>
/// Translates GitHub webhook payloads into factory events
/// </summary>
public class GitHubWebhookTranslator : IWebhookTranslator
{
    public FactoryEvent? Translate(string system, string payload)
    {
        if (!string.Equals(system, "github", StringComparison.OrdinalIgnoreCase))
            return null;

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        if (root.TryGetProperty("pull_request", out var pr))
            return TranslatePullRequest(root, pr);

        if (root.TryGetProperty("check_suite", out var checkSuite))
            return TranslateCheckSuite(root, checkSuite);

        if (root.TryGetProperty("deployment", out _))
            return TranslateDeployment(root);

        if (root.TryGetProperty("ref", out _) && root.TryGetProperty("commits", out _))
            return TranslatePush(root);

        return null;
    }

    private static FactoryEvent TranslatePullRequest(JsonElement root, JsonElement pr)
    {
        var action = root.GetProperty("action").GetString() ?? "";
        var number = pr.GetProperty("number").GetInt32().ToString();
        var title = pr.GetProperty("title").GetString() ?? "";

        var eventType = action switch
        {
            "opened" => FactoryEventType.PullRequestOpened,
            "merged" or "closed" when pr.TryGetProperty("merged", out var m) && m.GetBoolean()
                => FactoryEventType.PullRequestMerged,
            _ => FactoryEventType.PullRequestOpened
        };

        return new FactoryEvent
        {
            EventType = eventType,
            ExternalId = number,
            ExternalSystem = "github",
            Payload = new Dictionary<string, string>
            {
                ["Title"] = title,
                ["Action"] = action
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static FactoryEvent TranslateCheckSuite(JsonElement root, JsonElement checkSuite)
    {
        var conclusion = checkSuite.TryGetProperty("conclusion", out var c)
            ? c.GetString() ?? ""
            : "";
        var id = checkSuite.GetProperty("id").GetInt64().ToString();

        var eventType = conclusion switch
        {
            "success" => FactoryEventType.BuildSucceeded,
            "failure" => FactoryEventType.BuildFailed,
            _ => FactoryEventType.BuildFailed
        };

        return new FactoryEvent
        {
            EventType = eventType,
            ExternalId = id,
            ExternalSystem = "github",
            Payload = new Dictionary<string, string>
            {
                ["Conclusion"] = conclusion
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static FactoryEvent TranslateDeployment(JsonElement root)
    {
        var deployment = root.GetProperty("deployment");
        var id = deployment.GetProperty("id").GetInt64().ToString();
        var action = root.GetProperty("action").GetString() ?? "";

        var eventType = action switch
        {
            "created" => FactoryEventType.DeploymentStarted,
            _ => FactoryEventType.DeploymentStarted
        };

        return new FactoryEvent
        {
            EventType = eventType,
            ExternalId = id,
            ExternalSystem = "github",
            Payload = new Dictionary<string, string>
            {
                ["Action"] = action
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static FactoryEvent TranslatePush(JsonElement root)
    {
        var refName = root.GetProperty("ref").GetString() ?? "";
        var headCommit = root.TryGetProperty("head_commit", out var hc)
            ? hc.GetProperty("id").GetString() ?? ""
            : "";

        return new FactoryEvent
        {
            EventType = FactoryEventType.StatusChanged,
            ExternalId = headCommit,
            ExternalSystem = "github",
            Payload = new Dictionary<string, string>
            {
                ["Ref"] = refName,
                ["NewStatus"] = "DevelopmentInProgress"
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
