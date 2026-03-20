using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.Core.Interfaces;

/// <summary>
/// Translates external system webhook payloads into factory events
/// </summary>
public interface IWebhookTranslator
{
    /// <summary>
    /// Translates a webhook payload from the specified system into a factory event
    /// </summary>
    /// <param name="system">The external system identifier</param>
    /// <param name="payload">The raw JSON payload</param>
    /// <returns>The translated factory event, or null if the payload is not relevant</returns>
    FactoryEvent? Translate(string system, string payload);
}
