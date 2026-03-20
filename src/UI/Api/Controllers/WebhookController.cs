using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Receives webhook payloads from external systems and translates them into factory events
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhookController(IBus bus, IEnumerable<IWebhookTranslator> translators) : ControllerBase
{
    /// <summary>
    /// Receives a webhook payload from an external system
    /// </summary>
    /// <param name="system">The external system identifier (e.g., github, azdo, jira)</param>
    [HttpPost("{system}")]
    public async Task<IActionResult> Post(string system)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        foreach (var translator in translators)
        {
            var factoryEvent = translator.Translate(system, payload);
            if (factoryEvent != null)
            {
                await bus.Publish(factoryEvent);
                return Ok(new { status = "accepted", eventType = factoryEvent.EventType.Code });
            }
        }

        return BadRequest(new { status = "unrecognized", system });
    }
}
