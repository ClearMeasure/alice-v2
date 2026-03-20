namespace ClearMeasure.Bootcamp.AcceptanceTests;

public class BlazorWasmWarmUp
{
    private const int MaxAttempts = 5;
    private const int TimeoutSeconds = 30;

    private readonly IPlaywright _playwright;
    private readonly string _baseUrl;

    public BlazorWasmWarmUp(IPlaywright playwright, string baseUrl)
    {
        _playwright = playwright;
        _baseUrl = baseUrl;
    }

    public async Task ExecuteAsync()
    {
        TestContext.Out.WriteLine("Blazor WASM warm-up: starting...");

        await using var browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true
        });
        context.SetDefaultTimeout(TimeoutSeconds * 1000);

        var page = await context.NewPageAsync();
        var jsErrors = new List<string>();
        page.PageError += (_, error) => jsErrors.Add(error);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            jsErrors.Clear();
            TestContext.Out.WriteLine($"Blazor WASM warm-up: attempt {attempt}/{MaxAttempts}");

            await page.GotoAsync("/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var shell = page.GetByTestId("ApplicationSkeleton");
            try
            {
                await shell.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = TimeoutSeconds * 1000
                });

                TestContext.Out.WriteLine("Blazor WASM warm-up: application skeleton visible — app is ready.");
                await page.CloseAsync();
                await context.CloseAsync();
                return;
            }
            catch (TimeoutException)
            {
                TestContext.Out.WriteLine(
                    $"Blazor WASM warm-up: application skeleton not visible after {TimeoutSeconds}s. JS errors captured: {jsErrors.Count}");
                foreach (var error in jsErrors)
                {
                    TestContext.Out.WriteLine($"  JS error: {error}");
                }

                if (attempt < MaxAttempts)
                {
                    await page.ReloadAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
            }
        }

        await page.CloseAsync();
        await context.CloseAsync();
        TestContext.Out.WriteLine("WARNING: Blazor WASM warm-up did not confirm the application skeleton before tests continued.");
    }
}
