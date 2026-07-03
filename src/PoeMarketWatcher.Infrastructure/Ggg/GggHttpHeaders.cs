namespace PoeMarketWatcher.Infrastructure.Ggg;

internal static class GggHttpHeaders
{
    public static IReadOnlyDictionary<string, IEnumerable<string>> ToDictionary(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value;
        }

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = header.Value;
            }
        }

        return headers;
    }
}
