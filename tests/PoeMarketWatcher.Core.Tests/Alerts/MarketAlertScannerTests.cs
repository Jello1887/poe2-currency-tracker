using PoeMarketWatcher.Core.Alerts;
using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Tests.Alerts;

public class MarketAlertScannerTests
{
    [Fact]
    public void ScanDoesNotAlertWhenHistoryIsBelowMinimum()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.10m, 1_000m, 90m));

        var result = MarketAlertScanner.Scan(current, []);

        Assert.Empty(result.Alerts);
        Assert.Equal(0, result.SuppressedCount);
    }

    [Fact]
    public void ScanAlertsOnFivePercentPriceMovement()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.05m, 1_000m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 1_000m));

        var alert = Assert.Single(result.Alerts);
        Assert.Equal(AlertSignal.PriceMovement, alert.Signal);
        Assert.Equal(AlertConfidence.High, alert.Confidence);
        Assert.Equal(5m, alert.ExchangeRateChangePercent);
        Assert.Equal(1m, alert.VolumeMultiplier);
        Assert.Contains("5.0%", alert.Reason);
    }

    [Fact]
    public void ScanDoesNotAlertOnPriceMovementBelowRawFivePercentThreshold()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.0496m, 1_000m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 1_000m));

        Assert.Empty(result.Alerts);
    }

    [Fact]
    public void ScanAlertsOnPriceMovementWhenPreviousTradedVolumeIsZero()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.05m, 1_000m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 0m));

        var alert = Assert.Single(result.Alerts);
        Assert.Equal(AlertSignal.PriceMovement, alert.Signal);
        Assert.Equal(5m, alert.ExchangeRateChangePercent);
    }

    [Fact]
    public void ScanAlertsOnTwoTimesVolumeSpike()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1m, 2_000m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 1_000m));

        var alert = Assert.Single(result.Alerts);
        Assert.Equal(AlertSignal.VolumeSpike, alert.Signal);
        Assert.Equal(2m, alert.VolumeMultiplier);
        Assert.Contains("2.0x", alert.Reason);
    }

    [Fact]
    public void ScanDoesNotAlertOnVolumeSpikeBelowRawTwoTimesThreshold()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1m, 1_960m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 1_000m));

        Assert.Empty(result.Alerts);
    }

    [Fact]
    public void ScanAlertsOnCombinedThreePercentPriceMovementAndOnePointFiveTimesVolumeSpike()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.03m, 1_500m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 1_000m));

        var alert = Assert.Single(result.Alerts);
        Assert.Equal(AlertSignal.Combined, alert.Signal);
        Assert.Equal(3m, alert.ExchangeRateChangePercent);
        Assert.Equal(1.5m, alert.VolumeMultiplier);
        Assert.Contains("3.0%", alert.Reason);
        Assert.Contains("1.5x", alert.Reason);
    }

    [Fact]
    public void ScanDoesNotAlertOnCombinedSignalBelowRawThresholds()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.0296m, 1_496m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 1_000m));

        Assert.Empty(result.Alerts);
    }

    [Fact]
    public void ScanDoesNotAlertOnVolumeDependentSignalsWhenPreviousTradedVolumeIsZero()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.03m, 1_500m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 0m));

        Assert.Empty(result.Alerts);
    }

    [Fact]
    public void ScanDowngradesConfidenceWhenCurrentVolumeIsLowLiquidity()
    {
        var current = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), 1.05m, 499m, 90m));

        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 499m));

        var alert = Assert.Single(result.Alerts);
        Assert.Equal(AlertConfidence.Low, alert.Confidence);
        Assert.Contains("low liquidity", alert.Reason);
    }

    [Fact]
    public void ScanSortsBySeverityBeforeApplyingRunCapAndReportsSuppressedCount()
    {
        PairMetric[] currentMetrics =
        [
            Metric("pair-a", 1.05m),
            Metric("pair-b", 1.10m),
            Metric("pair-c", 1.06m),
            Metric("pair-d", 1.09m),
            Metric("pair-e", 1.07m),
            Metric("pair-f", 1.08m)
        ];

        var current = Snapshot(currentMetrics);
        var result = MarketAlertScanner.Scan(current, HistoryWithPreviousRate(1m, 1_000m, currentMetrics));

        Assert.Equal(1, result.SuppressedCount);
        Assert.Equal(
            ["pair-b|divine", "pair-d|divine", "pair-f|divine", "pair-e|divine", "pair-c|divine"],
            result.Alerts.Select(alert => alert.Pair.MarketId));
    }

    [Fact]
    public void ScanEvaluatesAllCurrentMetricsBeyondDigestRowCounts()
    {
        var quietMetrics = Enumerable.Range(0, 10)
            .Select(index => new PairMetric(new CurrencyPair($"quiet-{index}", "divine"), 1m, 10_000m, 90m));
        var alertingMetric = new PairMetric(new CurrencyPair("late", "divine"), 1.05m, 1_000m, 90m);
        var currentMetrics = quietMetrics.Append(alertingMetric).ToArray();
        var baselineMetrics = currentMetrics
            .Select(metric => metric.Pair.MarketId == "late|divine"
                ? metric with { ExchangeRate = 1m }
                : metric)
            .ToArray();

        var result = MarketAlertScanner.Scan(Snapshot(currentMetrics), [Snapshot(baselineMetrics), Snapshot(baselineMetrics)]);

        var alert = Assert.Single(result.Alerts);
        Assert.Equal("late|divine", alert.Pair.MarketId);
    }

    private static IReadOnlyList<MarketSnapshot> HistoryWithPreviousRate(
        decimal previousRate,
        decimal previousVolume,
        IReadOnlyList<PairMetric>? currentMetrics = null)
    {
        var firstHistory = Snapshot(
            new PairMetric(new CurrencyPair("chaos", "divine"), previousRate, previousVolume, 90m));

        var baselineMetrics = currentMetrics is null
            ? [new PairMetric(new CurrencyPair("chaos", "divine"), previousRate, previousVolume, 90m)]
            : currentMetrics
                .Select(metric => metric with { ExchangeRate = previousRate, TradedVolume = previousVolume })
                .ToArray();

        return [firstHistory, Snapshot(baselineMetrics)];
    }

    private static PairMetric Metric(string left, decimal exchangeRate)
    {
        return new PairMetric(new CurrencyPair(left, "divine"), exchangeRate, 1_000m, 90m);
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
