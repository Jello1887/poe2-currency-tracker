using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Alerts;

public sealed record MarketAlert(
    CurrencyPair Pair,
    AlertSignal Signal,
    AlertConfidence Confidence,
    decimal CurrentExchangeRate,
    decimal PreviousExchangeRate,
    decimal ExchangeRateChangePercent,
    decimal CurrentTradedVolume,
    decimal PreviousTradedVolume,
    decimal VolumeMultiplier,
    decimal Severity,
    string Reason);
