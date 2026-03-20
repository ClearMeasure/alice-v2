using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model.Agents;
using ClearMeasure.Bootcamp.Core.Model.Factory;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;

/// <summary>
/// Worker agent adapter that proxies execution to an external HTTP endpoint
/// </summary>
public class RemoteWorkerAgent : IWorkerAgent
{
    private readonly HttpClient _httpClient;
    private readonly string _endpointUrl;

    public string AgentName { get; }
    public FactoryStatus TargetStatus { get; }

    public RemoteWorkerAgent(string agentName, FactoryStatus targetStatus, string endpointUrl, HttpClient httpClient)
    {
        AgentName = agentName;
        TargetStatus = targetStatus;
        _endpointUrl = endpointUrl;
        _httpClient = httpClient;
    }

    public async Task<WorkerAgentResult> ExecuteAsync(FactoryWorkItem workItem, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            workItemId = workItem.Id,
            externalId = workItem.ExternalId,
            externalSystem = workItem.ExternalSystem,
            currentStatus = workItem.CurrentStatus.Code,
            title = workItem.Title
        };

        var response = await _httpClient.PostAsJsonAsync(_endpointUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var success = root.GetProperty("success").GetBoolean();
        var summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
        var nextStatusCode = root.TryGetProperty("nextStatus", out var ns) ? ns.GetString() : null;
        var outputData = new Dictionary<string, string>();

        if (root.TryGetProperty("outputData", out var od) && od.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in od.EnumerateObject())
            {
                outputData[prop.Name] = prop.Value.GetString() ?? "";
            }
        }

        return new WorkerAgentResult(
            success,
            nextStatusCode != null ? FactoryStatus.FromCode(nextStatusCode) : null,
            summary,
            outputData);
    }
}
