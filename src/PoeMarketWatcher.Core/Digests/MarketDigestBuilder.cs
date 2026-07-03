using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Digests;

public static class MarketDigestBuilder
{
    private const string BaselinePending = "baseline pending";

    public static MarketDigest Build(
        MarketSnapshot currentSnapshot,
        MarketSnapshot? previousSnapshot = null,
        DigestOptions? options = null)
    {
        options ??= new DigestOptions();

        var previousMetrics = previousSnapshot?.Metrics.ToDictionary(metric => metric.Pair.MarketId);
        var rows = currentSnapshot.Metrics
            .OrderByDescending(metric => metric.TradedVolume)
            .ThenBy(metric => metric.Pair.MarketId, StringComparer.Ordinal)
            .Take(options.PairCount)
            .Select(metric => BuildRow(metric, previousMetrics))
            .ToArray();

        return new MarketDigest(currentSnapshot, rows);
    }

    private static DigestRow BuildRow(
        PairMetric current,
        IReadOnlyDictionary<string, PairMetric>? previousMetrics)
    {
        if (previousMetrics is null ||
            !previousMetrics.TryGetValue(current.Pair.MarketId, out var previous) ||
            previous.ExchangeRate <= 0m)
        {
            return new DigestRow(
                current.Pair,
                current.ExchangeRate,
                current.TradedVolume,
                current.LiquidityScore,
                ExchangeRateChangePercent: null,
                ChangeDescription: BaselinePending);
        }

        var changePercent = Math.Round(
            ((current.ExchangeRate - previous.ExchangeRate) / previous.ExchangeRate) * 100m,
            1,
            MidpointRounding.AwayFromZero);

        return new DigestRow(
            current.Pair,
            current.ExchangeRate,
            current.TradedVolume,
            current.LiquidityScore,
            changePercent,
            ChangeDescription: null);
    }
}
