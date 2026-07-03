using PoeMarketWatcher.Core.Alerts;
using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Tests.Alerts;

public class AlertDedupeServiceTests
{
    [Fact]
    public void FilterSuppressesSamePairAndSignalInsideCooldown()
    {
        var now = new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
        var alert = Alert("chaos", AlertSignal.PriceMovement);
        EmittedAlertRecord[] prior =
        [
            new("chaos|divine", AlertSignal.PriceMovement, now.AddHours(-5))
        ];

        var result = AlertDedupeService.Filter([alert], prior, now);

        Assert.Empty(result.Alerts);
        Assert.Equal(1, result.SuppressedCount);
    }

    [Fact]
    public void FilterAllowsSamePairAndDifferentSignalInsideCooldown()
    {
        var now = new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
        var alert = Alert("chaos", AlertSignal.VolumeSpike);
        EmittedAlertRecord[] prior =
        [
            new("chaos|divine", AlertSignal.PriceMovement, now.AddHours(-5))
        ];

        var result = AlertDedupeService.Filter([alert], prior, now);

        Assert.Same(alert, Assert.Single(result.Alerts));
        Assert.Equal(0, result.SuppressedCount);
    }

    [Fact]
    public void FilterAllowsSamePairAndSignalAfterCooldown()
    {
        var now = new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
        var alert = Alert("chaos", AlertSignal.PriceMovement);
        EmittedAlertRecord[] prior =
        [
            new("chaos|divine", AlertSignal.PriceMovement, now.AddHours(-6).AddTicks(-1))
        ];

        var result = AlertDedupeService.Filter([alert], prior, now);

        Assert.Same(alert, Assert.Single(result.Alerts));
        Assert.Equal(0, result.SuppressedCount);
    }

    [Fact]
    public void FilterSuppressesDuplicatePairAndSignalInsideSameInputBatch()
    {
        var now = new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
        var first = Alert("chaos", AlertSignal.PriceMovement);
        var duplicate = Alert("chaos", AlertSignal.PriceMovement);

        var result = AlertDedupeService.Filter([first, duplicate], [], now);

        Assert.Same(first, Assert.Single(result.Alerts));
        Assert.Equal(1, result.SuppressedCount);
    }

    private static MarketAlert Alert(string left, AlertSignal signal)
    {
        var pair = new CurrencyPair(left, "divine");

        return new MarketAlert(
            Pair: pair,
            Signal: signal,
            Confidence: AlertConfidence.High,
            CurrentExchangeRate: 1.05m,
            PreviousExchangeRate: 1m,
            ExchangeRateChangePercent: 5m,
            CurrentTradedVolume: 1_000m,
            PreviousTradedVolume: 1_000m,
            VolumeMultiplier: 1m,
            Severity: 5m,
            Reason: "price moved 5.0%");
    }
}
