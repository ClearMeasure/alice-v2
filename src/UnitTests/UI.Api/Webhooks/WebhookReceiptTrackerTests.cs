using ClearMeasure.Bootcamp.UI.Api.Webhooks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Webhooks;

[TestFixture]
public class WebhookReceiptTrackerTests
{
    [Test]
    public async Task WaitForReceiptAsync_WhenReceiptRecorded_ReturnsTrue()
    {
        var tracker = new WebhookReceiptTracker();

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            tracker.RecordReceipt("GitHub", "PVTI_1");
        });

        var result = await tracker.WaitForReceiptAsync("GitHub", TimeSpan.FromSeconds(5));

        result.ShouldBeTrue();
    }

    [Test]
    public async Task WaitForReceiptAsync_WhenNoReceipt_ReturnsFalse()
    {
        var tracker = new WebhookReceiptTracker();

        var result = await tracker.WaitForReceiptAsync("GitHub", TimeSpan.FromMilliseconds(100));

        result.ShouldBeFalse();
    }

    [Test]
    public async Task WaitForReceiptAsync_IsCaseInsensitive()
    {
        var tracker = new WebhookReceiptTracker();

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            tracker.RecordReceipt("github", "PVTI_1");
        });

        var result = await tracker.WaitForReceiptAsync("GitHub", TimeSpan.FromSeconds(5));

        result.ShouldBeTrue();
    }
}
