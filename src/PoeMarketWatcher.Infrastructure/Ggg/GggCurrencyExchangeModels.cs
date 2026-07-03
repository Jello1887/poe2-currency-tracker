using System.Text.Json.Serialization;

namespace PoeMarketWatcher.Infrastructure.Ggg;

public sealed record GggCurrencyExchangeResponse(
    [property: JsonPropertyName("next_change_id")] long NextChangeId,
    [property: JsonPropertyName("markets")] IReadOnlyList<GggCurrencyExchangeMarket> Markets);

public sealed record GggCurrencyExchangeMarket(
    [property: JsonPropertyName("market_id")] string? MarketId,
    [property: JsonPropertyName("volume_traded")] decimal VolumeTraded,
    [property: JsonPropertyName("lowest_ratio")] decimal? LowestRatio,
    [property: JsonPropertyName("highest_ratio")] decimal? HighestRatio);
