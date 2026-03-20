using ClearMeasure.Bootcamp.Core.Commands;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Translates a raw webhook payload from an external board system into a domain command.
/// Implementations exist per source system (GitHub, Azure DevOps, etc.).
/// </summary>
public interface IWorkItemWebhookTranslator
{
    /// <summary>The source system this translator handles (e.g. "GitHub").</summary>
    string Source { get; }

    /// <summary>Determines whether this translator can handle the given payload.</summary>
    bool CanHandle(string payload);

    /// <summary>
    /// Translates the raw payload into a command.
    /// Returns null if the payload represents an event that should be ignored.
    /// </summary>
    RecordWorkItemEventCommand? Translate(string payload);
}
