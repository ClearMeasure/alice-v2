using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Interfaces;
using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.DataAccess.Translators;

/// <summary>
/// Translates Azure DevOps service hook payloads into factory events
/// </summary>
public class AzureDevOpsWebhookTranslator : IWebhookTranslator
{
    public FactoryEvent? Translate(string system, string payload)
    {
        if (!string.Equals(system, "azdo", StringComparison.OrdinalIgnoreCase))
            return null;

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var eventType = root.TryGetProperty("eventType", out var et)
            ? et.GetString() ?? ""
            : "";

        return eventType switch
        {
            "workitem.updated" => TranslateWorkItemUpdated(root),
            "build.complete" => TranslateBuildComplete(root),
            "ms.vss-release.deployment-completed-event" => TranslateDeployment(root),
            _ => null
        };
    }

    private static FactoryEvent TranslateWorkItemUpdated(JsonElement root)
    {
        var resource = root.GetProperty("resource");
        var id = resource.GetProperty("id").GetInt32().ToString();
        var title = resource.TryGetProperty("fields", out var fields)
            && fields.TryGetProperty("System.Title", out var t)
                ? t.GetString() ?? ""
                : "";
        var newState = resource.TryGetProperty("fields", out var f)
            && f.TryGetProperty("System.State", out var s)
                ? s.GetProperty("newValue").GetString() ?? ""
                : "";

        return new FactoryEvent
        {
            EventType = FactoryEventType.StatusChanged,
            ExternalId = id,
            ExternalSystem = "azdo",
            Payload = new Dictionary<string, string>
            {
                ["Title"] = title,
                ["NewStatus"] = MapAzdoState(newState)
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static FactoryEvent TranslateBuildComplete(JsonElement root)
    {
        var resource = root.GetProperty("resource");
        var id = resource.GetProperty("id").GetInt32().ToString();
        var result = resource.TryGetProperty("result", out var r) ? r.GetString() ?? "" : "";

        return new FactoryEvent
        {
            EventType = result == "succeeded" ? FactoryEventType.BuildSucceeded : FactoryEventType.BuildFailed,
            ExternalId = id,
            ExternalSystem = "azdo",
            Payload = new Dictionary<string, string>
            {
                ["Result"] = result
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static FactoryEvent TranslateDeployment(JsonElement root)
    {
        var resource = root.GetProperty("resource");
        var environment = resource.TryGetProperty("environment", out var env)
            ? env.GetProperty("name").GetString() ?? ""
            : "";
        var id = resource.TryGetProperty("id", out var idProp) ? idProp.GetInt32().ToString() : "";
        var status = resource.TryGetProperty("deploymentStatus", out var ds)
            ? ds.GetString() ?? ""
            : "";

        var eventType = status switch
        {
            "succeeded" => FactoryEventType.DeploymentCompleted,
            "failed" => FactoryEventType.DeploymentFailed,
            _ => FactoryEventType.DeploymentStarted
        };

        return new FactoryEvent
        {
            EventType = eventType,
            ExternalId = id,
            ExternalSystem = "azdo",
            Payload = new Dictionary<string, string>
            {
                ["Environment"] = environment,
                ["Status"] = status
            },
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static string MapAzdoState(string azdoState)
    {
        return azdoState.ToLowerInvariant() switch
        {
            "new" => "Conceptual",
            "active" => "DevelopmentInProgress",
            "resolved" => "DevelopmentComplete",
            "closed" => "Stable",
            _ => "Conceptual"
        };
    }
}
