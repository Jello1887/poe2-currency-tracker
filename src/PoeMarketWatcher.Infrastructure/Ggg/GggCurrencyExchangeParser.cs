using System.Text.Json;
using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Infrastructure.Ggg;

public static class GggCurrencyExchangeParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static MarketSnapshot ParseSnapshot(string rawJson)
    {
        var response = JsonSerializer.Deserialize<GggCurrencyExchangeResponse>(rawJson, JsonOptions)
            ?? throw new InvalidOperationException("Currency exchange response is empty.");

        var changeId = TruncateToHour(response.NextChangeId);
        var metrics = response.Markets
            .GroupBy(market => RequireMarketId(market.MarketId))
            .Select(ToMetric)
            .ToArray();

        return new MarketSnapshot(
            changeId,
            DateTimeOffset.FromUnixTimeSeconds(changeId),
            metrics,
            rawJson);
    }

    private static PairMetric ToMetric(IGrouping<string, GggCurrencyExchangeMarket> marketGroup)
    {
        var midpointRates = new List<decimal>();
        var tradedVolume = 0m;

        foreach (var market in marketGroup)
        {
            midpointRates.Add(MidpointRatio(market));
            tradedVolume += market.VolumeTraded;
        }

        var exchangeRate = midpointRates.Sum() / midpointRates.Count;
        return new PairMetric(
            CurrencyPair.FromMarketId(marketGroup.Key),
            exchangeRate,
            tradedVolume,
            tradedVolume);
    }

    private static decimal MidpointRatio(GggCurrencyExchangeMarket market)
    {
        if (market.LowestRatio is null || market.HighestRatio is null)
        {
            throw new InvalidOperationException($"Market '{market.MarketId ?? "<missing>"}' is missing ratio data.");
        }

        return (market.LowestRatio.Value + market.HighestRatio.Value) / 2m;
    }

    private static string RequireMarketId(string? marketId)
    {
        if (string.IsNullOrWhiteSpace(marketId))
        {
            throw new InvalidOperationException("Currency exchange market is missing market_id.");
        }

        return marketId;
    }

    private static long TruncateToHour(long unixSeconds)
    {
        return unixSeconds - (unixSeconds % 3600);
    }
}
