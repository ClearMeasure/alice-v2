using ClearMeasure.Bootcamp.Core.Model.Messages;
using ClearMeasure.Bootcamp.IntegrationTests;
using NServiceBus;

namespace ClearMeasure.Bootcamp.AcceptanceTests.NServiceBus;

[TestFixture]
public class TracerBulletTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task TracerBullet_WorkerReceivesCommandAndReplies()
    {
        if (!ServerFixture.WorkerStarted)
        {
            Assert.Ignore("Worker is not running (requires SqlServerTransport). Skipping tracer bullet test.");
        }

        var correlationId = Guid.NewGuid();
        var messageSession = TestHost.GetRequiredService<IMessageSession>(newScope: false);
        var replyTask = TracerBulletSignal.WaitForReply(correlationId, TimeSpan.FromSeconds(60));

        await messageSession.Send(new TracerBulletCommand(correlationId));
        await replyTask;
    }
}
