namespace PoeMarketWatcher.App.Configuration;

public sealed class MarketWatcherOptions
{
    public GggOptions Ggg { get; set; } = new();

    public StorageOptions Storage { get; set; } = new();

    public DigestOptions Digest { get; set; } = new();

    public AlertOptions Alerts { get; set; } = new();

    public DiscordOptions Discord { get; set; } = new();
}

public sealed class GggOptions
{
    public Uri ApiBaseUrl { get; set; } = new("https://api.pathofexile.com");

    public Uri OAuthTokenUrl { get; set; } = new("https://www.pathofexile.com/oauth/token");

    public string Scope { get; set; } = "service:cxapi";

    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";

    public string Contact { get; set; } = "";

    public string Version { get; set; } = "";
}

public sealed class StorageOptions
{
    public string SqlitePath { get; set; } = "data/poe-market-watcher.db";
}

public sealed class DigestOptions
{
    public int PairCount { get; set; } = 10;
}

public sealed class AlertOptions
{
    public decimal PriceMovementPercent { get; set; } = 5m;

    public decimal VolumeSpikeMultiplier { get; set; } = 2m;

    public decimal CombinedPriceMovementPercent { get; set; } = 3m;

    public decimal CombinedVolumeSpikeMultiplier { get; set; } = 1.5m;

    public int MinimumHistorySnapshots { get; set; } = 2;

    public decimal CooldownHours { get; set; } = 6m;

    public int MaxAlertsPerRun { get; set; } = 5;

    public decimal LowLiquidityVolume { get; set; } = 500m;
}

public sealed class DiscordOptions
{
    public string WebhookUrl { get; set; } = "";
}
