namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class WarmUpTests : AcceptanceTestBase
{
    [Test, Retry(2), Explicit]
    public async Task WarmUp_BlazorWasm_ApplicationSkeletonVisible()
    {
        var shell = Page.GetByTestId("ApplicationSkeleton");
        await shell.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 30_000
        });
        await Expect(shell).ToBeVisibleAsync();
    }
}
