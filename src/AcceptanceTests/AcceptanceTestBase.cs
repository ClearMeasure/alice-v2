using System.Collections.Concurrent;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

public class TestState
{
    public required IPage Page { get; init; }
    public required IBrowserContext BrowserContext { get; init; }
    public required IBrowser Browser { get; init; }
}

public abstract class AcceptanceTestBase
{
    private static readonly ConcurrentDictionary<string, TestState> TestStates = new();

    protected virtual bool? Headless { get; set; } = ServerFixture.HeadlessTestBrowser;
    protected virtual bool SkipScreenshotsForSpeed { get; set; } = ServerFixture.SkipScreenshotsForSpeed;

    private string TestId => TestContext.CurrentContext.Test.ID;

    private TestState State => TestStates[TestId];

    protected IPage Page => State.Page;

    protected virtual bool RequiresBrowser => true;

    [SetUp]
    public async Task SetUpAsync()
    {
        if (!RequiresBrowser) return;

        var browser = await ServerFixture.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = ServerFixture.SlowMo
        });

        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = ServerFixture.ApplicationBaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });
        browserContext.SetDefaultTimeout(60_000);

        var page = await browserContext.NewPageAsync().ConfigureAwait(false);
        TestStates[TestId] = new TestState
        {
            Page = page,
            BrowserContext = browserContext,
            Browser = browser
        };

        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        if (!TestStates.TryRemove(TestId, out var state))
        {
            return;
        }

        try { await state.Page.CloseAsync(); } catch { }
        try { await state.BrowserContext.CloseAsync(); } catch { }
        try { await state.Browser.CloseAsync(); } catch { }
    }

    protected ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    protected IPageAssertions Expect(IPage page) => Assertions.Expect(page);

    protected async Task TakeScreenshotAsync(int stepNumber = 0, string? stepName = null)
    {
        if (SkipScreenshotsForSpeed || !RequiresBrowser) return;

        var test = TestContext.CurrentContext.Test;
        var testName = test.ClassName + "-" + test.Name;
        var fileName = $"{testName}-{stepNumber}{stepName}{Guid.NewGuid()}.png";
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = fileName
        });
        TestContext.AddTestAttachment(Path.GetFullPath(fileName));
    }

    protected int GetInputDelayMs()
    {
        var envValue = Environment.GetEnvironmentVariable("TEST_INPUT_DELAY_MS");
        if (int.TryParse(envValue, out var delay))
        {
            return delay;
        }
        return 100;
    }
}
