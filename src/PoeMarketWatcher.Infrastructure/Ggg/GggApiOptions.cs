namespace PoeMarketWatcher.Infrastructure.Ggg;

public sealed class GggApiOptions
{
    public string ClientId { get; init; } = "";

    public string ClientSecret { get; init; } = "";

    public string Contact { get; init; } = "";

    public string Version { get; init; } = "";

    public string Scope { get; init; } = "service:cxapi";

    public Uri ApiBaseUrl { get; init; } = new("https://api.pathofexile.com");

    public Uri OAuthTokenUrl { get; init; } = new("https://www.pathofexile.com/oauth/token");
}
