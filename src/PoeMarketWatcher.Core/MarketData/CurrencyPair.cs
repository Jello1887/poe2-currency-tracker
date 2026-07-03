namespace PoeMarketWatcher.Core.MarketData;

public sealed record CurrencyPair(string Left, string Right)
{
    public string MarketId => $"{Left}|{Right}";

    public string DisplayName => $"{Left} / {Right}";

    public static CurrencyPair FromMarketId(string marketId)
    {
        if (string.IsNullOrEmpty(marketId))
        {
            throw new ArgumentException("Market id must contain exactly one pipe separator.", nameof(marketId));
        }

        var parts = marketId.Split('|');
        if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
        {
            throw new ArgumentException("Market id must contain exactly one pipe separator with non-empty sides.", nameof(marketId));
        }

        return new CurrencyPair(parts[0], parts[1]);
    }
}
