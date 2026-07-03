using System.Globalization;
using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Alerts;

public static class MarketAlertScanner
{
    public static MarketAlertScanResult Scan(
        MarketSnapshot currentSnapshot,
        IReadOnlyList<MarketSnapshot> history,
        AlertOptions? options = null)
    {
        options ??= new AlertOptions();

        if (history.Count < options.MinimumHistorySnapshots)
        {
            return new MarketAlertScanResult([], 0);
        }

        var previousMetrics = history[^1].Metrics.ToDictionary(metric => metric.Pair.MarketId);
        var alerts = currentSnapshot.Metrics
            .Select(metric => BuildAlert(metric, previousMetrics, options))
            .OfType<MarketAlert>()
            .OrderByDescending(alert => alert.Severity)
            .ThenBy(alert => alert.Pair.MarketId, StringComparer.Ordinal)
            .ToArray();

        var emittedAlerts = alerts
            .Take(options.MaxAlertsPerRun)
            .ToArray();

        return new MarketAlertScanResult(
            emittedAlerts,
            Math.Max(0, alerts.Length - emittedAlerts.Length));
    }

    private static MarketAlert? BuildAlert(
        PairMetric current,
        IReadOnlyDictionary<string, PairMetric> previousMetrics,
        AlertOptions options)
    {
        if (!previousMetrics.TryGetValue(current.Pair.MarketId, out var previous) ||
            previous.ExchangeRate <= 0m)
        {
            return null;
        }

        var rawChangePercent = ((current.ExchangeRate - previous.ExchangeRate) / previous.ExchangeRate) * 100m;
        var hasUsableVolume = previous.TradedVolume > 0m;
        var rawVolumeMultiplier = hasUsableVolume
            ? current.TradedVolume / previous.TradedVolume
            : 0m;
        var rawAbsoluteChangePercent = Math.Abs(rawChangePercent);

        var hasCombinedSignal =
            hasUsableVolume &&
            rawAbsoluteChangePercent >= options.CombinedPriceMovementPercent &&
            rawVolumeMultiplier >= options.CombinedVolumeSpikeMultiplier;
        var hasPriceMovement = rawAbsoluteChangePercent >= options.PriceMovementPercent;
        var hasVolumeSpike = hasUsableVolume && rawVolumeMultiplier >= options.VolumeSpikeMultiplier;

        if (!hasCombinedSignal && !hasPriceMovement && !hasVolumeSpike)
        {
            return null;
        }

        var changePercent = Math.Round(rawChangePercent, 1, MidpointRounding.AwayFromZero);
        var volumeMultiplier = Math.Round(rawVolumeMultiplier, 1, MidpointRounding.AwayFromZero);
        var signal = hasCombinedSignal
            ? AlertSignal.Combined
            : hasPriceMovement
                ? AlertSignal.PriceMovement
                : AlertSignal.VolumeSpike;
        var confidence = current.TradedVolume < options.LowLiquidityVolume
            ? AlertConfidence.Low
            : AlertConfidence.High;
        var severity = CalculateSeverity(signal, rawAbsoluteChangePercent, rawVolumeMultiplier, options);
        var reason = BuildReason(signal, confidence, changePercent, volumeMultiplier);

        return new MarketAlert(
            current.Pair,
            signal,
            confidence,
            current.ExchangeRate,
            previous.ExchangeRate,
            changePercent,
            current.TradedVolume,
            previous.TradedVolume,
            volumeMultiplier,
            severity,
            reason);
    }

    private static decimal CalculateSeverity(
        AlertSignal signal,
        decimal absoluteChangePercent,
        decimal volumeMultiplier,
        AlertOptions options)
    {
        return signal switch
        {
            AlertSignal.Combined =>
                absoluteChangePercent + ((volumeMultiplier - options.CombinedVolumeSpikeMultiplier) * 10m),
            AlertSignal.VolumeSpike =>
                (volumeMultiplier - options.VolumeSpikeMultiplier) * 10m,
            _ => absoluteChangePercent
        };
    }

    private static string BuildReason(
        AlertSignal signal,
        AlertConfidence confidence,
        decimal changePercent,
        decimal volumeMultiplier)
    {
        var formattedChange = changePercent.ToString("0.0", CultureInfo.InvariantCulture);
        var formattedMultiplier = volumeMultiplier.ToString("0.0", CultureInfo.InvariantCulture);
        var baseReason = signal switch
        {
            AlertSignal.Combined => $"price moved {formattedChange}% and volume reached {formattedMultiplier}x baseline",
            AlertSignal.VolumeSpike => $"volume reached {formattedMultiplier}x baseline",
            _ => $"price moved {formattedChange}%"
        };

        return confidence == AlertConfidence.Low
            ? $"{baseReason}; low liquidity confidence downgrade"
            : baseReason;
    }
}

public sealed record MarketAlertScanResult(
    IReadOnlyList<MarketAlert> Alerts,
    int SuppressedCount);
