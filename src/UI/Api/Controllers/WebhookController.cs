using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Receives webhooks from external board systems and dispatches work item tracking commands.
/// </summary>
[ApiController]
[Route("webhook")]
public class WebhookController(
    IEnumerable<IWorkItemWebhookTranslator> translators,
    IBus bus,
    IWebhookReceiptTracker receiptTracker,
    ILogger<WebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Receives a webhook payload from the specified source system.
    /// </summary>
    [HttpPost("{source}")]
    public async Task<IActionResult> Receive(string source, [FromBody] object payload)
    {
        var rawPayload = payload.ToString() ?? string.Empty;

        logger.LogInformation("Webhook received from {Source}", source);

        var translator = translators
            .FirstOrDefault(t => string.Equals(t.Source, source, StringComparison.OrdinalIgnoreCase)
                                 && t.CanHandle(rawPayload));

        if (translator is null)
        {
            logger.LogWarning("No translator found for source {Source}", source);
            return Ok(new { status = "ignored", reason = "no matching translator" });
        }

        var command = translator.Translate(rawPayload);
        if (command is null)
        {
            logger.LogInformation("Webhook from {Source} translated to no-op", source);
            return Ok(new { status = "ignored", reason = "event not tracked" });
        }

        var eventId = await bus.Send(command);
        receiptTracker.RecordReceipt(source, command.WorkItemExternalId);
        logger.LogInformation("Work item event {EventId} recorded from {Source}", eventId, source);

        return Ok(new { status = "recorded", eventId });
    }
}
