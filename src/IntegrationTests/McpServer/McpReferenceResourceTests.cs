using ClearMeasure.Bootcamp.McpServer.Resources;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpReferenceResourceTests
{
    [Test]
    public void GetApplicationSkeleton_ReturnsLayerMetadata()
    {
        var result = ReferenceResources.GetApplicationSkeleton();

        result.ShouldContain("Employee");
        result.ShouldContain("Core");
        result.ShouldContain("DataAccess");
        result.ShouldContain("Worker");
        result.ShouldContain("McpServer");
    }
}
