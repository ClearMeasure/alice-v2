namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    [Test, Retry(2), Explicit]
    public async Task Should_DisplayApplicationSkeletonPlaceholder()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs());

        var shell = Page.GetByTestId("ApplicationSkeleton");
        await Expect(shell).ToBeVisibleAsync();
        await Expect(shell).ToContainTextAsync("Architecture Skeleton");
    }
}
