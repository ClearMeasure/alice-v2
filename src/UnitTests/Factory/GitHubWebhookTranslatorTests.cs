using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model.Factory;
using ClearMeasure.Bootcamp.DataAccess.Translators;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Factory;

[TestFixture]
public class GitHubWebhookTranslatorTests
{
    private readonly GitHubWebhookTranslator _translator = new();

    [Test]
    public void ShouldTranslatePullRequestOpenedEvent()
    {
        var payload = JsonSerializer.Serialize(new
        {
            action = "opened",
            pull_request = new
            {
                number = 42,
                title = "Add new feature",
                merged = false
            }
        });

        var result = _translator.Translate("github", payload);

        result.ShouldNotBeNull();
        result.EventType.ShouldBe(FactoryEventType.PullRequestOpened);
        result.ExternalId.ShouldBe("42");
        result.ExternalSystem.ShouldBe("github");
        result.Payload["Title"].ShouldBe("Add new feature");
    }

    [Test]
    public void ShouldTranslateCheckSuiteSuccessEvent()
    {
        var payload = JsonSerializer.Serialize(new
        {
            action = "completed",
            check_suite = new
            {
                id = 12345L,
                conclusion = "success"
            }
        });

        var result = _translator.Translate("github", payload);

        result.ShouldNotBeNull();
        result.EventType.ShouldBe(FactoryEventType.BuildSucceeded);
        result.ExternalId.ShouldBe("12345");
    }

    [Test]
    public void ShouldTranslateCheckSuiteFailureEvent()
    {
        var payload = JsonSerializer.Serialize(new
        {
            action = "completed",
            check_suite = new
            {
                id = 12345L,
                conclusion = "failure"
            }
        });

        var result = _translator.Translate("github", payload);

        result.ShouldNotBeNull();
        result.EventType.ShouldBe(FactoryEventType.BuildFailed);
    }

    [Test]
    public void ShouldTranslatePushEvent()
    {
        var payload = JsonSerializer.Serialize(new
        {
            @ref = "refs/heads/main",
            commits = new[] { new { id = "abc123" } },
            head_commit = new { id = "abc123" }
        });

        var result = _translator.Translate("github", payload);

        result.ShouldNotBeNull();
        result.EventType.ShouldBe(FactoryEventType.StatusChanged);
        result.ExternalId.ShouldBe("abc123");
        result.Payload["NewStatus"].ShouldBe("DevelopmentInProgress");
    }

    [Test]
    public void ShouldReturnNullForNonGitHubSystem()
    {
        var result = _translator.Translate("azdo", "{}");

        result.ShouldBeNull();
    }
}
