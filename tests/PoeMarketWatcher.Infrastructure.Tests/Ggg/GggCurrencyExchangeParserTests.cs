using PoeMarketWatcher.Infrastructure.Ggg;
using System.Text.Json;

namespace PoeMarketWatcher.Infrastructure.Tests.Ggg;

public class GggCurrencyExchangeParserTests
{
    [Fact]
    public void ParseSnapshotConvertsCurrencyExchangeDigestToMarketSnapshot()
    {
        var rawJson = File.ReadAllText(Path.Combine("Fixtures", "currency-exchange-poe2.json"));

        var snapshot = GggCurrencyExchangeParser.ParseSnapshot(rawJson);

        Assert.Equal(1783108800, snapshot.ChangeId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1783108800), snapshot.SnapshotAt);
        Assert.Equal(rawJson, snapshot.RawJson);

        var metric = Assert.Single(snapshot.Metrics);
        Assert.Equal("exalted|divine", metric.Pair.MarketId);
        Assert.Equal(3m, metric.ExchangeRate);
        Assert.Equal(120m, metric.TradedVolume);
        Assert.Equal(120m, metric.LiquidityScore);
    }

    [Fact]
    public void FixtureUsesOneOfficialStyleMarketEntryWithoutSyntheticSide()
    {
        var rawJson = File.ReadAllText(Path.Combine("Fixtures", "currency-exchange-poe2.json"));
        using var document = JsonDocument.Parse(rawJson);

        var markets = document.RootElement.GetProperty("markets");
        var market = Assert.Single(markets.EnumerateArray());

        Assert.True(market.TryGetProperty("market_id", out _));
        Assert.True(market.TryGetProperty("volume_traded", out _));
        Assert.True(market.TryGetProperty("lowest_ratio", out _));
        Assert.True(market.TryGetProperty("highest_ratio", out _));
        Assert.False(market.TryGetProperty("side", out _));
        Assert.Null(typeof(GggCurrencyExchangeMarket).GetProperty("Side"));
    }

    [Theory]
    [InlineData("""{"next_change_id":1783108800,"markets":[{"volume_traded":1,"lowest_ratio":1,"highest_ratio":2}]}""")]
    [InlineData("""{"next_change_id":1783108800,"markets":[{"market_id":"exalted|divine","volume_traded":1,"highest_ratio":2}]}""")]
    public void ParseSnapshotRejectsMissingRequiredMarketData(string rawJson)
    {
        Assert.Throws<InvalidOperationException>(() => GggCurrencyExchangeParser.ParseSnapshot(rawJson));
    }
}
