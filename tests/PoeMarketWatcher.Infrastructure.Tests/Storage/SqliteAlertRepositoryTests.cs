using PoeMarketWatcher.Core.Alerts;
using PoeMarketWatcher.Infrastructure.Storage;

namespace PoeMarketWatcher.Infrastructure.Tests.Storage;

public sealed class SqliteAlertRepositoryTests
{
    [Fact]
    public async Task SaveEmittedAlertsAsyncReloadsRecentAlertsWithinCooldownWindow()
    {
        await using var database = TempSqliteDatabase.Create();
        var repository = new SqliteAlertRepository(database.Path);
        var oldAlert = new EmittedAlertRecord(
            "chaos|exalted",
            AlertSignal.VolumeSpike,
            new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero));
        var firstRecentAlert = new EmittedAlertRecord(
            "exalted|divine",
            AlertSignal.PriceMovement,
            new DateTimeOffset(2026, 7, 3, 11, 0, 0, TimeSpan.Zero));
        var secondRecentAlert = new EmittedAlertRecord(
            "exalted|divine",
            AlertSignal.Combined,
            new DateTimeOffset(2026, 7, 3, 11, 30, 0, TimeSpan.Zero));

        await repository.SaveEmittedAlertsAsync(new[] { oldAlert, firstRecentAlert, secondRecentAlert });

        var recentAlerts = await repository.GetRecentAlertsAsync(new DateTimeOffset(2026, 7, 3, 10, 30, 0, TimeSpan.Zero));

        Assert.Equal(new[] { secondRecentAlert, firstRecentAlert }, recentAlerts);
    }
}
