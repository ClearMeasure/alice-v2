using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Api.Webhooks;

/// <summary>
/// GitHub Projects V2 client that uses the GraphQL API to trigger status changes
/// on a dedicated health-check item. Requires configuration:
///   - GitHub:PersonalAccessToken — PAT with project read/write scope
///   - GitHub:HealthCheck:ProjectId — the Projects V2 node ID
///   - GitHub:HealthCheck:ItemId — the project item node ID for health checks
///   - GitHub:HealthCheck:StatusFieldId — the single-select status field node ID
///   - GitHub:HealthCheck:StatusOptionId — the option ID to set (toggles back and forth)
/// </summary>
public class GitHubProjectClient : IGitHubProjectClient
{
    private readonly string? _personalAccessToken;
    private readonly string? _projectId;
    private readonly string? _itemId;
    private readonly string? _statusFieldId;
    private readonly string? _statusOptionId;
    private readonly ILogger<GitHubProjectClient> _logger;
    private static readonly HttpClient HttpClient = new();

    public GitHubProjectClient(IConfiguration configuration, ILogger<GitHubProjectClient> logger)
    {
        _personalAccessToken = configuration["GitHub:PersonalAccessToken"];
        _projectId = configuration["GitHub:HealthCheck:ProjectId"];
        _itemId = configuration["GitHub:HealthCheck:ItemId"];
        _statusFieldId = configuration["GitHub:HealthCheck:StatusFieldId"];
        _statusOptionId = configuration["GitHub:HealthCheck:StatusOptionId"];
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_personalAccessToken)
        && !string.IsNullOrWhiteSpace(_projectId)
        && !string.IsNullOrWhiteSpace(_itemId)
        && !string.IsNullOrWhiteSpace(_statusFieldId)
        && !string.IsNullOrWhiteSpace(_statusOptionId);

    public async Task<string> TriggerHealthCheckWebhookAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("GitHub health check client is not configured");
        }

        var mutation = """
            mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
              updateProjectV2ItemFieldValue(
                input: {
                  projectId: $projectId
                  itemId: $itemId
                  fieldId: $fieldId
                  value: { singleSelectOptionId: $optionId }
                }
              ) {
                projectV2Item {
                  id
                }
              }
            }
            """;

        var requestBody = new
        {
            query = mutation,
            variables = new
            {
                projectId = _projectId,
                itemId = _itemId,
                fieldId = _statusFieldId,
                optionId = _statusOptionId
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _personalAccessToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AISoftwareFactory", "1.0"));
        request.Content = content;

        _logger.LogInformation("Triggering GitHub Projects V2 status change for health check");
        var response = await HttpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GitHub GraphQL request failed: {StatusCode} {Body}",
                response.StatusCode, responseBody);
            throw new HttpRequestException(
                $"GitHub GraphQL request failed with status {response.StatusCode}: {responseBody}");
        }

        _logger.LogDebug("GitHub Projects V2 status change triggered successfully");
        return _itemId!;
    }
}
