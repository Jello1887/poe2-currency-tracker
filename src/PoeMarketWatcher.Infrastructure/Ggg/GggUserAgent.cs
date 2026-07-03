namespace PoeMarketWatcher.Infrastructure.Ggg;

internal static class GggUserAgent
{
    public static string Build(GggApiOptions options)
    {
        return $"OAuth {options.ClientId}/{options.Version} (contact: {options.Contact}) PoeMarketWatcher";
    }
}
