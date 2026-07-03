namespace PoeMarketWatcher.Core.MarketData;

public sealed record PairMetric(
    CurrencyPair Pair,
    decimal ExchangeRate,
    decimal TradedVolume,
    decimal LiquidityScore);
