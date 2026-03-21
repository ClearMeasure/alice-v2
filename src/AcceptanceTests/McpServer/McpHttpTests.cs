namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpHttpTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task ListTools_ReturnsExpectedEmployeeTools()
    {
        var client = McpHttpServerFixture.Client!;

        var tools = await client.ListToolsAsync();

        tools.ShouldContain(t => t.Name == "list-employees");
        tools.ShouldContain(t => t.Name == "get-employee");
    }

    [Test]
    public async Task ListEmployees_ReturnsNonEmptyResult()
    {
        var client = McpHttpServerFixture.Client!;

        var result = await client.CallToolAsync("list-employees");

        result.ShouldNotBeNull();
        result.IsError.ShouldNotBe(true);
        result.Content.ShouldNotBeEmpty();
    }
}
