using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Infrastructure.Ggg;

public interface IGggCurrencyExchangeParser
{
    MarketSnapshot ParseSnapshot(string rawJson);
}

public sealed class DefaultGggCurrencyExchangeParser : IGggCurrencyExchangeParser
{
    public MarketSnapshot ParseSnapshot(string rawJson)
    {
        return GggCurrencyExchangeParser.ParseSnapshot(rawJson);
    }
}
