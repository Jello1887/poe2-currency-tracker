namespace PoeMarketWatcher.Infrastructure.Ggg;

internal static class GggHttpContent
{
    public static Task<string> ReadBodyAsStringAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        return response.Content is null
            ? Task.FromResult("")
            : response.Content.ReadAsStringAsync(cancellationToken);
    }
}
