using ClearMeasure.Bootcamp.UI.Api.Webhooks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Webhooks;

[TestFixture]
public class GitHubProjectsV2WebhookTranslatorTests
{
    private GitHubProjectsV2WebhookTranslator _translator = null!;

    [SetUp]
    public void SetUp()
    {
        _translator = new GitHubProjectsV2WebhookTranslator();
    }

    [Test]
    public void Source_ReturnsGitHub()
    {
        _translator.Source.ShouldBe("GitHub");
    }

    [Test]
    public void CanHandle_WithProjectsV2ItemPayload_ReturnsTrue()
    {
        var payload = """
            {
                "action": "edited",
                "projects_v2_item": { "node_id": "PVTI_123" }
            }
            """;

        _translator.CanHandle(payload).ShouldBeTrue();
    }

    [Test]
    public void CanHandle_WithNonProjectPayload_ReturnsFalse()
    {
        var payload = """{ "action": "opened", "issue": { "id": 1 } }""";

        _translator.CanHandle(payload).ShouldBeFalse();
    }

    [Test]
    public void CanHandle_WithInvalidJson_ReturnsFalse()
    {
        _translator.CanHandle("not json").ShouldBeFalse();
    }

    [Test]
    public void Translate_CreatedAction_ReturnsCreatedCommand()
    {
        var payload = """
            {
                "action": "created",
                "projects_v2_item": {
                    "node_id": "PVTI_abc",
                    "project_node_id": "PVT_xyz",
                    "content_type": "Issue",
                    "updated_at": "2026-03-20T10:00:00Z"
                },
                "organization": { "login": "my-org" }
            }
            """;

        var command = _translator.Translate(payload);

        command.ShouldNotBeNull();
        command.WorkItemExternalId.ShouldBe("PVTI_abc");
        command.Source.ShouldBe("GitHub");
        command.EventType.ShouldBe("Created");
        command.PreviousStatus.ShouldBeNull();
        command.NewStatus.ShouldBe("No Status");
        command.ProjectName.ShouldBe("my-org");
    }

    [Test]
    public void Translate_EditedAction_WithFieldValueChange_ReturnsStatusChangedCommand()
    {
        var payload = """
            {
                "action": "edited",
                "projects_v2_item": {
                    "node_id": "PVTI_abc",
                    "project_node_id": "PVT_xyz",
                    "content_type": "Issue",
                    "updated_at": "2026-03-20T10:00:00Z"
                },
                "changes": {
                    "field_value": {
                        "field_node_id": "PVTSSF_123",
                        "field_type": "single_select",
                        "from": { "name": "Todo" },
                        "to": { "name": "In Progress" }
                    }
                },
                "organization": { "login": "my-org" }
            }
            """;

        var command = _translator.Translate(payload);

        command.ShouldNotBeNull();
        command.EventType.ShouldBe("StatusChanged");
        command.PreviousStatus.ShouldBe("Todo");
        command.NewStatus.ShouldBe("In Progress");
    }

    [Test]
    public void Translate_EditedAction_WithoutChanges_ReturnsNull()
    {
        var payload = """
            {
                "action": "edited",
                "projects_v2_item": {
                    "node_id": "PVTI_abc",
                    "updated_at": "2026-03-20T10:00:00Z"
                }
            }
            """;

        _translator.Translate(payload).ShouldBeNull();
    }

    [Test]
    public void Translate_DeletedAction_ReturnsDeletedCommand()
    {
        var payload = """
            {
                "action": "deleted",
                "projects_v2_item": {
                    "node_id": "PVTI_abc",
                    "updated_at": "2026-03-20T10:00:00Z"
                },
                "sender": { "login": "user1" }
            }
            """;

        var command = _translator.Translate(payload);

        command.ShouldNotBeNull();
        command.EventType.ShouldBe("Deleted");
        command.NewStatus.ShouldBe("Deleted");
        command.ProjectName.ShouldBe("user1");
    }

    [Test]
    public void Translate_UnknownAction_ReturnsNull()
    {
        var payload = """
            {
                "action": "reopened",
                "projects_v2_item": {
                    "node_id": "PVTI_abc",
                    "updated_at": "2026-03-20T10:00:00Z"
                }
            }
            """;

        _translator.Translate(payload).ShouldBeNull();
    }
}
