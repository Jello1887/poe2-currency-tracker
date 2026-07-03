using PoeMarketWatcher.Core.MarketData;
using PoeMarketWatcher.Infrastructure.Storage;

namespace PoeMarketWatcher.Infrastructure.Tests.Storage;

public sealed class SqliteMarketRepositoryTests
{
    [Fact]
    public async Task SaveSnapshotAsyncReloadsSnapshotAndMetricsWithExactDecimalTextValues()
    {
        await using var database = TempSqliteDatabase.Create();
        var repository = new SqliteMarketRepository(database.Path);
        var snapshot = new MarketSnapshot(
            101,
            new DateTimeOffset(2026, 7, 3, 12, 15, 30, TimeSpan.Zero),
            new[]
            {
                new PairMetric(
                    new CurrencyPair("exalted", "divine"),
                    1.234567890123456789m,
                    987654321.123456789m,
                    0.000000000000000001m),
                new PairMetric(
                    new CurrencyPair("chaos", "exalted"),
                    12.5m,
                    345.75m,
                    678.125m),
            },
            """{"next_change_id":101}""");

        await repository.SaveSnapshotAsync(snapshot);

        var reloaded = await repository.GetLatestSnapshotAsync();

        Assert.NotNull(reloaded);
        Assert.Equal(snapshot.ChangeId, reloaded.ChangeId);
        Assert.Equal(snapshot.SnapshotAt, reloaded.SnapshotAt);
        Assert.Equal(snapshot.RawJson, reloaded.RawJson);
        Assert.Collection(
            reloaded.Metrics,
            first =>
            {
                Assert.Equal("exalted|divine", first.Pair.MarketId);
                Assert.Equal(1.234567890123456789m, first.ExchangeRate);
                Assert.Equal(987654321.123456789m, first.TradedVolume);
                Assert.Equal(0.000000000000000001m, first.LiquidityScore);
            },
            second =>
            {
                Assert.Equal("chaos|exalted", second.Pair.MarketId);
                Assert.Equal(12.5m, second.ExchangeRate);
                Assert.Equal(345.75m, second.TradedVolume);
                Assert.Equal(678.125m, second.LiquidityScore);
            });
    }

    [Fact]
    public async Task QueriesLatestPreviousAndRecentSnapshotsBySnapshotTime()
    {
        await using var database = TempSqliteDatabase.Create();
        var repository = new SqliteMarketRepository(database.Path);

        await repository.SaveSnapshotAsync(CreateSnapshot(100, new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero)));
        await repository.SaveSnapshotAsync(CreateSnapshot(200, new DateTimeOffset(2026, 7, 3, 11, 0, 0, TimeSpan.Zero)));
        await repository.SaveSnapshotAsync(CreateSnapshot(300, new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero)));

        var latest = await repository.GetLatestSnapshotAsync();
        var previous = await repository.GetPreviousSnapshotAsync(300);
        var recent = await repository.GetRecentSnapshotsAsync(new DateTimeOffset(2026, 7, 3, 10, 30, 0, TimeSpan.Zero), limit: 10);

        Assert.NotNull(latest);
        Assert.Equal(300, latest.ChangeId);
        Assert.NotNull(previous);
        Assert.Equal(200, previous.ChangeId);
        Assert.Equal(new[] { 200L, 300L }, recent.Select(snapshot => snapshot.ChangeId));
    }

    [Fact]
    public async Task GetRecentSnapshotsAsyncLimitsToMostRecentSnapshotsThenReturnsChronologically()
    {
        await using var database = TempSqliteDatabase.Create();
        var repository = new SqliteMarketRepository(database.Path);

        await repository.SaveSnapshotAsync(CreateSnapshot(100, new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero)));
        await repository.SaveSnapshotAsync(CreateSnapshot(200, new DateTimeOffset(2026, 7, 3, 11, 0, 0, TimeSpan.Zero)));
        await repository.SaveSnapshotAsync(CreateSnapshot(300, new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero)));

        var recent = await repository.GetRecentSnapshotsAsync(new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero), limit: 2);

        Assert.Equal(new[] { 200L, 300L }, recent.Select(snapshot => snapshot.ChangeId));
    }

    private static MarketSnapshot CreateSnapshot(long changeId, DateTimeOffset snapshotAt)
    {
        return new MarketSnapshot(
            changeId,
            snapshotAt,
            new[]
            {
                new PairMetric(
                    new CurrencyPair("exalted", "divine"),
                    changeId / 100m,
                    changeId * 2m,
                    changeId * 3m),
            },
            $$"""{"next_change_id":{{changeId}}}""");
    }
}
