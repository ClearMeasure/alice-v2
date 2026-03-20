using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Commands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Controllers;

[TestFixture]
public class WebhookControllerTests
{
    [Test]
    public async Task Receive_WithMatchingTranslator_ReturnsRecordedStatus()
    {
        var eventId = Guid.NewGuid();
        var stubBus = new StubBus(eventId);
        var translator = new StubTranslator("GitHub", canHandle: true,
            new RecordWorkItemEventCommand("EXT1", "GitHub", "StatusChanged",
                "Todo", "Done", "Title", "Proj", DateTimeOffset.UtcNow, "{}"));
        var controller = new WebhookController(
            [translator], stubBus, NullLogger<WebhookController>.Instance);

        var result = await controller.Receive("GitHub", "{}");

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldNotBeNull();
        stubBus.LastSentCommand.ShouldNotBeNull();
        stubBus.LastSentCommand.WorkItemExternalId.ShouldBe("EXT1");
    }

    [Test]
    public async Task Receive_WithNoMatchingTranslator_ReturnsIgnoredStatus()
    {
        var stubBus = new StubBus(Guid.NewGuid());
        var controller = new WebhookController(
            [], stubBus, NullLogger<WebhookController>.Instance);

        var result = await controller.Receive("Unknown", "{}");

        result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public async Task Receive_WhenTranslatorReturnsNull_ReturnsIgnoredStatus()
    {
        var stubBus = new StubBus(Guid.NewGuid());
        var translator = new StubTranslator("GitHub", canHandle: true, command: null);
        var controller = new WebhookController(
            [translator], stubBus, NullLogger<WebhookController>.Instance);

        var result = await controller.Receive("GitHub", "{}");

        result.ShouldBeOfType<OkObjectResult>();
        stubBus.LastSentCommand.ShouldBeNull();
    }

    [Test]
    public async Task Receive_MatchesTranslatorBySourceCaseInsensitive()
    {
        var eventId = Guid.NewGuid();
        var stubBus = new StubBus(eventId);
        var translator = new StubTranslator("GitHub", canHandle: true,
            new RecordWorkItemEventCommand("EXT1", "GitHub", "Created",
                null, "Todo", "Title", "Proj", DateTimeOffset.UtcNow, "{}"));
        var controller = new WebhookController(
            [translator], stubBus, NullLogger<WebhookController>.Instance);

        var result = await controller.Receive("github", "{}");

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        stubBus.LastSentCommand.ShouldNotBeNull();
    }

    private class StubBus(Guid responseId) : IBus
    {
        public RecordWorkItemEventCommand? LastSentCommand { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is RecordWorkItemEventCommand cmd)
            {
                LastSentCommand = cmd;
            }

            return Task.FromResult((TResponse)(object)responseId);
        }

        public Task<object?> Send(object request)
        {
            throw new NotImplementedException();
        }

        public Task Publish(INotification notification)
        {
            throw new NotImplementedException();
        }
    }

    private class StubTranslator(string source, bool canHandle, RecordWorkItemEventCommand? command)
        : IWorkItemWebhookTranslator
    {
        public string Source => source;
        public bool CanHandle(string payload) => canHandle;
        public RecordWorkItemEventCommand? Translate(string payload) => command;
    }
}
