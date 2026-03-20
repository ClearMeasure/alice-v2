using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Commands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Api.Webhooks;

/// <summary>
/// Translates GitHub Projects V2 webhook payloads into work item event commands.
/// Handles "projects_v2_item" events with "edited", "created", and "deleted" actions.
/// </summary>
public class GitHubProjectsV2WebhookTranslator : IWorkItemWebhookTranslator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string Source => "GitHub";

    public bool CanHandle(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            return doc.RootElement.TryGetProperty("action", out _)
                   && doc.RootElement.TryGetProperty("projects_v2_item", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public RecordWorkItemEventCommand? Translate(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var action = root.GetProperty("action").GetString() ?? string.Empty;
        var item = root.GetProperty("projects_v2_item");

        var itemNodeId = item.GetProperty("node_id").GetString() ?? string.Empty;
        var projectNodeId = item.TryGetProperty("project_node_id", out var projId)
            ? projId.GetString() ?? string.Empty
            : string.Empty;

        var title = ExtractTitle(root);
        var projectName = ExtractProjectName(root);

        var occurredAt = item.TryGetProperty("updated_at", out var updatedAt)
            ? DateTimeOffset.Parse(updatedAt.GetString()!)
            : DateTimeOffset.UtcNow;

        return action switch
        {
            "created" => new RecordWorkItemEventCommand(
                WorkItemExternalId: itemNodeId,
                Source: Source,
                EventType: "Created",
                PreviousStatus: null,
                NewStatus: ExtractCurrentStatus(root) ?? "No Status",
                Title: title,
                ProjectName: projectName,
                OccurredAtUtc: occurredAt,
                RawPayload: payload),

            "edited" => TranslateEditedAction(root, itemNodeId, title, projectName, occurredAt, payload),

            "deleted" => new RecordWorkItemEventCommand(
                WorkItemExternalId: itemNodeId,
                Source: Source,
                EventType: "Deleted",
                PreviousStatus: ExtractCurrentStatus(root),
                NewStatus: "Deleted",
                Title: title,
                ProjectName: projectName,
                OccurredAtUtc: occurredAt,
                RawPayload: payload),

            _ => null
        };
    }

    private RecordWorkItemEventCommand? TranslateEditedAction(
        JsonElement root, string itemNodeId, string title,
        string projectName, DateTimeOffset occurredAt, string payload)
    {
        if (!root.TryGetProperty("changes", out var changes))
        {
            return null;
        }

        if (!changes.TryGetProperty("field_value", out var fieldValue))
        {
            return null;
        }

        var previousStatus = fieldValue.TryGetProperty("from", out var from)
            ? ExtractFieldValueText(from)
            : null;

        var newStatus = fieldValue.TryGetProperty("to", out var to)
            ? ExtractFieldValueText(to)
            : ExtractCurrentStatus(root) ?? "Unknown";

        return new RecordWorkItemEventCommand(
            WorkItemExternalId: itemNodeId,
            Source: Source,
            EventType: "StatusChanged",
            PreviousStatus: previousStatus,
            NewStatus: newStatus,
            Title: title,
            ProjectName: projectName,
            OccurredAtUtc: occurredAt,
            RawPayload: payload);
    }

    private static string? ExtractFieldValueText(JsonElement fieldValue)
    {
        if (fieldValue.TryGetProperty("name", out var name))
        {
            return name.GetString();
        }

        if (fieldValue.TryGetProperty("text", out var text))
        {
            return text.GetString();
        }

        if (fieldValue.ValueKind == JsonValueKind.String)
        {
            return fieldValue.GetString();
        }

        return null;
    }

    private static string? ExtractCurrentStatus(JsonElement root)
    {
        if (root.TryGetProperty("projects_v2_item", out var item)
            && item.TryGetProperty("content_type", out _))
        {
            // Status may be in changes.field_value.to for edited events
            if (root.TryGetProperty("changes", out var changes)
                && changes.TryGetProperty("field_value", out var fieldValue)
                && fieldValue.TryGetProperty("to", out var to))
            {
                return ExtractFieldValueText(to);
            }
        }

        return null;
    }

    private static string ExtractTitle(JsonElement root)
    {
        if (root.TryGetProperty("projects_v2_item", out var item)
            && item.TryGetProperty("content_node_id", out _))
        {
            // GitHub doesn't always include the full title in the webhook
            // The content_node_id can be used to fetch it via GraphQL if needed
        }

        return "Work Item";
    }

    private static string ExtractProjectName(JsonElement root)
    {
        if (root.TryGetProperty("organization", out var org)
            && org.TryGetProperty("login", out var login))
        {
            return login.GetString() ?? "Unknown Project";
        }

        if (root.TryGetProperty("sender", out var sender)
            && sender.TryGetProperty("login", out var senderLogin))
        {
            return senderLogin.GetString() ?? "Unknown Project";
        }

        return "Unknown Project";
    }
}
