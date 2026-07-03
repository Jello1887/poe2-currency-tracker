using PoeMarketWatcher.Core.Digests;
using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Tests.Digests;

public class MarketDigestBuilderTests
{
    [Fact]
    public void BuildSelectsTopVolumePairsInDeterministicOrder()
    {
        var snapshot = Snapshot(
            new PairMetric(new CurrencyPair("wisdom", "divine"), 0.01m, 100m, 10m),
            new PairMetric(new CurrencyPair("chaos", "divine"), 0.5m, 1_000m, 90m),
            new PairMetric(new CurrencyPair("exalted", "divine"), 0.25m, 1_000m, 80m),
            new PairMetric(new CurrencyPair("regal", "divine"), 0.1m, 500m, 70m));

        var digest = MarketDigestBuilder.Build(snapshot, previousSnapshot: null, new DigestOptions(PairCount: 3));

        Assert.Equal(
            ["chaos|divine", "exalted|divine", "regal|divine"],
            digest.Rows.Select(row => row.Pair.MarketId));
        Assert.Equal([1_000m, 1_000m, 500m], digest.Rows.Select(row => row.TradedVolume));
    }

    [Fact]
    public void BuildMarksRowsAsBaselinePendingWhenPreviousSnapshotIsMissing()
    {
        var snapshot = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 0.5m, 1_000m, 90m));

        var digest = MarketDigestBuilder.Build(snapshot, previousSnapshot: null);

        var row = Assert.Single(digest.Rows);
        Assert.Null(row.ExchangeRateChangePercent);
        Assert.Equal("baseline pending", row.ChangeDescription);
    }

    [Fact]
    public void BuildCalculatesPreviousSnapshotPercentageChangeRoundedAwayFromZero()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.335m, 1_000m, 90m),
            new PairMetric(new CurrencyPair("exalted", "divine"), 0.995m, 900m, 80m));
        var previous = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1m, 500m, 40m),
            new PairMetric(new CurrencyPair("exalted", "divine"), 2m, 500m, 40m));

        var digest = MarketDigestBuilder.Build(current, previous, new DigestOptions(PairCount: 2));

        var rowsByPair = digest.Rows.ToDictionary(row => row.Pair.MarketId);
        Assert.Equal(33.5m, rowsByPair["chaos|divine"].ExchangeRateChangePercent);
        Assert.Equal(-50.3m, rowsByPair["exalted|divine"].ExchangeRateChangePercent);
        Assert.Null(rowsByPair["chaos|divine"].ChangeDescription);
        Assert.Null(rowsByPair["exalted|divine"].ChangeDescription);
    }

    [Fact]
    public void BuildMarksRowsAsBaselinePendingWhenPreviousMetricIsMissing()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 0.5m, 1_000m, 90m));
        var previous = Snapshot(
            new PairMetric(new CurrencyPair("exalted", "divine"), 0.25m, 900m, 80m));

        var digest = MarketDigestBuilder.Build(current, previous);

        var row = Assert.Single(digest.Rows);
        Assert.Null(row.ExchangeRateChangePercent);
        Assert.Equal("baseline pending", row.ChangeDescription);
    }

    [Fact]
    public void BuildMarksRowsAsBaselinePendingWhenPreviousExchangeRateIsZero()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 0.5m, 1_000m, 90m));
        var previous = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 0m, 900m, 80m));

        var digest = MarketDigestBuilder.Build(current, previous);

        var row = Assert.Single(digest.Rows);
        Assert.Null(row.ExchangeRateChangePercent);
        Assert.Equal("baseline pending", row.ChangeDescription);
    }

    private static MarketSnapshot Snapshot(params PairMetric[] metrics)
    {
        return new MarketSnapshot(
            ChangeId: 123456,
            SnapshotAt: new DateTimeOffset(2026, 7, 3, 12, 30, 0, TimeSpan.Zero),
            Metrics: metrics,
            RawJson: "{}");
    }
}
