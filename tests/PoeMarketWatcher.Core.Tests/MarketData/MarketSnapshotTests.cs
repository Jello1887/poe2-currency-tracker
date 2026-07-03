using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Tests.MarketData;

public class MarketSnapshotTests
{
    [Fact]
    public void FromMarketIdParsesCurrencyPair()
    {
        var pair = CurrencyPair.FromMarketId("chaos|divine");

        Assert.Equal("chaos", pair.Left);
        Assert.Equal("divine", pair.Right);
    }

    [Fact]
    public void MarketIdJoinsLeftAndRightWithPipe()
    {
        var pair = new CurrencyPair("chaos", "divine");

        Assert.Equal("chaos|divine", pair.MarketId);
    }

    [Fact]
    public void DisplayNameSeparatesLeftAndRightForHumans()
    {
        var pair = new CurrencyPair("chaos", "divine");

        Assert.Equal("chaos / divine", pair.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("chaos")]
    [InlineData("chaos|")]
    [InlineData("|divine")]
    [InlineData("chaos|divine|exalted")]
    public void FromMarketIdRejectsInvalidIds(string marketId)
    {
        Assert.Throws<ArgumentException>(() => CurrencyPair.FromMarketId(marketId));
    }

    [Fact]
    public void MarketSnapshotPreservesSuppliedValuesAndMetricOrder()
    {
        var snapshotAt = new DateTimeOffset(2026, 7, 3, 12, 30, 0, TimeSpan.Zero);
        PairMetric[] metrics =
        [
            new(new CurrencyPair("chaos", "divine"), 0.5m, 1200m, 98.5m),
            new(new CurrencyPair("exalted", "divine"), 0.25m, 450m, 72.25m)
        ];
        const string rawJson = """{"change_id":123456,"pairs":["chaos|divine","exalted|divine"]}""";

        var snapshot = new MarketSnapshot(123456, snapshotAt, metrics, rawJson);

        Assert.Equal(123456, snapshot.ChangeId);
        Assert.Equal(snapshotAt, snapshot.SnapshotAt);
        Assert.Same(metrics, snapshot.Metrics);
        Assert.Equal(["chaos|divine", "exalted|divine"], snapshot.Metrics.Select(metric => metric.Pair.MarketId));
        Assert.Equal(rawJson, snapshot.RawJson);
    }
}
