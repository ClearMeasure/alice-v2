using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Resources;

[McpServerResourceType]
public class ReferenceResources
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [McpServerResource(UriTemplate = "aisoftwarefactory://reference/application-skeleton", Name = "application-skeleton"),
     Description("High-level architectural layers retained in the cleaned application skeleton.")]
    public static string GetApplicationSkeleton()
    {
        var skeleton = new
        {
            Domain = new[] { "Employee" },
            Layers = new[] { "Core", "DataAccess", "UI.Server", "UI.Client", "UI.Shared", "Worker", "McpServer", "LlmGateway" }
        };

        return JsonSerializer.Serialize(skeleton, JsonOptions);
    }
}
