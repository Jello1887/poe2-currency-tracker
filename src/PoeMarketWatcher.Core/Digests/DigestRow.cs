using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Digests;

public sealed record DigestRow(
    CurrencyPair Pair,
    decimal ExchangeRate,
    decimal TradedVolume,
    decimal LiquidityScore,
    decimal? ExchangeRateChangePercent,
    string? ChangeDescription);
