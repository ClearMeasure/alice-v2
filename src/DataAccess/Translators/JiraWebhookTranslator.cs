using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Interfaces;
using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.DataAccess.Translators;

/// <summary>
/// Translates Jira webhook payloads into factory events
/// </summary>
public class JiraWebhookTranslator : IWebhookTranslator
{
    public FactoryEvent? Translate(string system, string payload)
    {
        if (!string.Equals(system, "jira", StringComparison.OrdinalIgnoreCase))
            return null;

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var webhookEvent = root.TryGetProperty("webhookEvent", out var we)
            ? we.GetString() ?? ""
            : "";

        return webhookEvent switch
        {
            "jira:issue_updated" => TranslateIssueUpdated(root),
            _ => null
        };
    }

    private static FactoryEvent TranslateIssueUpdated(JsonElement root)
    {
        var issue = root.GetProperty("issue");
        var key = issue.GetProperty("key").GetString() ?? "";
        var fields = issue.GetProperty("fields");
        var summary = fields.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";

        var changelog = root.TryGetProperty("changelog", out var cl) ? cl : default;
        var newStatus = ExtractStatusChange(changelog);

        if (newStatus == null)
            return new FactoryEvent
            {
                EventType = FactoryEventType.StatusChanged,
                ExternalId = key,
                ExternalSystem = "jira",
                Payload = new Dictionary<string, string>
                {
                    ["Title"] = summary
                },
                OccurredAt = DateTimeOffset.UtcNow
            };

        return new FactoryEvent
        {
            EventType = FactoryEventType.StatusChanged,
            ExternalId = key,
            ExternalSystem = "jira",
            Payload = new Dictionary<string, string>
            {
                ["Title"] = summary,
                ["NewStatus"] = MapJiraStatus(newStatus)
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static string? ExtractStatusChange(JsonElement changelog)
    {
        if (changelog.ValueKind == JsonValueKind.Undefined)
            return null;

        if (!changelog.TryGetProperty("items", out var items))
            return null;

        foreach (var item in items.EnumerateArray())
        {
            var field = item.TryGetProperty("field", out var f) ? f.GetString() ?? "" : "";
            if (field == "status")
            {
                return item.TryGetProperty("toString", out var to) ? to.GetString() : null;
            }
        }

        return null;
    }

    private static string MapJiraStatus(string jiraStatus)
    {
        return jiraStatus.ToLowerInvariant() switch
        {
            "to do" or "open" or "backlog" => "Conceptual",
            "in progress" => "DevelopmentInProgress",
            "in review" or "review" => "ReviewRequested",
            "done" or "closed" or "resolved" => "Stable",
            _ => "Conceptual"
        };
    }
}
