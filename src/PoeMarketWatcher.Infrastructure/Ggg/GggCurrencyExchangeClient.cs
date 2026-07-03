using System.Net;
using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Infrastructure.Ggg;

public sealed class GggCurrencyExchangeClient(
    HttpClient httpClient,
    GggApiOptions options,
    IGggCurrencyExchangeParser? parser = null)
{
    private readonly IGggCurrencyExchangeParser _parser = parser ?? new DefaultGggCurrencyExchangeParser();

    public Task<GggCurrencyExchangeClientResult> GetLatestSnapshotAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return GetSnapshotCoreAsync("currency-exchange/poe2", accessToken, cancellationToken);
    }

    public Task<GggCurrencyExchangeClientResult> GetSnapshotAsync(
        long timestamp,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return GetSnapshotCoreAsync($"currency-exchange/poe2/{timestamp}", accessToken, cancellationToken);
    }

    private async Task<GggCurrencyExchangeClientResult> GetSnapshotCoreAsync(
        string relativePath,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var requestUri = new Uri(options.ApiBaseUrl, relativePath);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation("User-Agent", GggUserAgent.Build(options));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var rateLimits = RateLimitHeaders.Parse(GggHttpHeaders.ToDictionary(response));
        var body = await GggHttpContent.ReadBodyAsStringAsync(response, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new GggCurrencyExchangeClientResult(
                _parser.ParseSnapshot(body),
                rateLimits);
        }

        var sanitizedBody = GggSecretSanitizer.Sanitize(body, accessToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new GggApiException(
                response.StatusCode,
                "GGG Currency Exchange request failed with HTTP 403. "
                    + "The bearer token has the wrong or missing service:cxapi scope. "
                    + sanitizedBody,
                rateLimits);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new GggRateLimitException(
                response.StatusCode,
                "GGG Currency Exchange request failed with HTTP 429. Retry-After: "
                    + $"{FormatRetryAfter(rateLimits.RetryAfter)}. {sanitizedBody}",
                rateLimits);
        }

        throw new GggApiException(
            response.StatusCode,
            "GGG Currency Exchange request failed with HTTP "
                + $"{(int)response.StatusCode}. {sanitizedBody}",
            rateLimits);
    }

    private static string FormatRetryAfter(TimeSpan? retryAfter)
    {
        return retryAfter?.ToString() ?? "not provided";
    }
}

public sealed record GggCurrencyExchangeClientResult(MarketSnapshot Snapshot, RateLimitHeaders RateLimits);
