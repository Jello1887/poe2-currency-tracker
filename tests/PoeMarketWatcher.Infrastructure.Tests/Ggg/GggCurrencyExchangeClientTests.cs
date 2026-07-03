using System.Net;
using System.Net.Http.Headers;
using System.Text;
using PoeMarketWatcher.Core.MarketData;
using PoeMarketWatcher.Infrastructure.Ggg;

namespace PoeMarketWatcher.Infrastructure.Tests.Ggg;

public class GggCurrencyExchangeClientTests
{
    private const string RawJson = """
    {
      "next_change_id": 1783109999,
      "markets": [
        {
          "market_id": "exalted|divine",
          "volume_traded": 120,
          "lowest_ratio": 2,
          "highest_ratio": 4
        }
      ]
    }
    """;

    [Fact]
    public async Task GetLatestSnapshotUsesOfficialPoe2EndpointBearerAuthUserAgentAndParsesResponse()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
        {
            var response = JsonResponse(HttpStatusCode.OK, RawJson);
            response.Headers.TryAddWithoutValidation("X-Rate-Limit-Policy", "currency");
            response.Headers.TryAddWithoutValidation("X-Rate-Limit-Rules", "Client");
            response.Headers.TryAddWithoutValidation("X-Rate-Limit-Client", "20:5:60");
            response.Headers.TryAddWithoutValidation("X-Rate-Limit-Client-State", "1:5:0");
            return response;
        });
        using var httpClient = new HttpClient(handler);
        var client = new GggCurrencyExchangeClient(httpClient, Options());

        var result = await client.GetLatestSnapshotAsync("bearer-token", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.pathofexile.com/currency-exchange/poe2", request.RequestUri?.ToString());
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "bearer-token"), request.Headers.Authorization);
        Assert.Contains("OAuth client-id/1.2.3", request.Headers.UserAgent.ToString(), StringComparison.Ordinal);
        Assert.Contains("(contact: ops@example.test)", request.Headers.UserAgent.ToString(), StringComparison.Ordinal);

        Assert.Equal(1783108800, result.Snapshot.ChangeId);
        Assert.Equal("currency", result.RateLimits.Policy);
        Assert.Equal(["Client"], result.RateLimits.Rules);
    }

    [Fact]
    public async Task GetSnapshotUsesOfficialPoe2TimestampEndpoint()
    {
        using var handler = new RecordingHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, RawJson));
        using var httpClient = new HttpClient(handler);
        var client = new GggCurrencyExchangeClient(httpClient, Options());

        await client.GetSnapshotAsync(1783108800, "bearer-token", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.pathofexile.com/currency-exchange/poe2/1783108800", request.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetLatestSnapshotUsesInjectedParserForSuccessfulResponse()
    {
        using var handler = new RecordingHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, RawJson));
        using var httpClient = new HttpClient(handler);
        var expectedSnapshot = new MarketSnapshot(
            123,
            DateTimeOffset.FromUnixTimeSeconds(123),
            [],
            "from injected parser");
        var parser = new StubCurrencyExchangeParser(expectedSnapshot);
        var client = new GggCurrencyExchangeClient(httpClient, Options(), parser);

        var result = await client.GetLatestSnapshotAsync("bearer-token", CancellationToken.None);

        Assert.Same(expectedSnapshot, result.Snapshot);
        Assert.Equal(RawJson, parser.RawJson);
    }

    [Fact]
    public async Task GetLatestSnapshotReportsMissingCxApiScopeOnForbidden()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.Forbidden, """{"error":{"code":2,"message":"Forbidden: bearer-token"}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new GggCurrencyExchangeClient(httpClient, Options());

        var exception = await Assert.ThrowsAsync<GggApiException>(() =>
            client.GetLatestSnapshotAsync("bearer-token", CancellationToken.None));

        Assert.Contains("service:cxapi", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("bearer-token", exception.Message, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    [Fact]
    public async Task GetLatestSnapshotTreatsForbiddenMissingBodyAsEmpty()
    {
        using var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var httpClient = new HttpClient(handler);
        var client = new GggCurrencyExchangeClient(httpClient, Options());

        var exception = await Assert.ThrowsAsync<GggApiException>(() =>
            client.GetLatestSnapshotAsync("bearer-token", CancellationToken.None));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Contains("service:cxapi", exception.Message, StringComparison.Ordinal);
        Assert.Empty(exception.RateLimits.Rules);
    }

    [Fact]
    public async Task GetLatestSnapshotExposesRetryAfterOnTooManyRequests()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
        {
            var response = JsonResponse(HttpStatusCode.TooManyRequests, """{"error":{"code":3,"message":"Rate limited"}}""");
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(45));
            response.Headers.TryAddWithoutValidation("X-Rate-Limit-Rules", "Client");
            return response;
        });
        using var httpClient = new HttpClient(handler);
        var client = new GggCurrencyExchangeClient(httpClient, Options());

        var exception = await Assert.ThrowsAsync<GggRateLimitException>(() =>
            client.GetLatestSnapshotAsync("bearer-token", CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(45), exception.RateLimits.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(45), exception.RetryAfter);
    }

    [Fact]
    public async Task GetLatestSnapshotTreatsTooManyRequestsMissingBodyAsEmptyAndPreservesRetryAfter()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(45));
            return response;
        });
        using var httpClient = new HttpClient(handler);
        var client = new GggCurrencyExchangeClient(httpClient, Options());

        var exception = await Assert.ThrowsAsync<GggRateLimitException>(() =>
            client.GetLatestSnapshotAsync("bearer-token", CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(45), exception.RateLimits.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(45), exception.RetryAfter);
    }

    [Fact]
    public async Task GetLatestSnapshotSanitizesBearerTokenFromFailures()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
            JsonResponse(
                HttpStatusCode.InternalServerError,
                """{"error":{"message":"failed token bearer-token"}}"""));
        using var httpClient = new HttpClient(handler);
        var client = new GggCurrencyExchangeClient(httpClient, Options());

        var exception = await Assert.ThrowsAsync<GggApiException>(() =>
            client.GetLatestSnapshotAsync("bearer-token", CancellationToken.None));

        Assert.DoesNotContain("bearer-token", exception.Message, StringComparison.Ordinal);
    }

    private static GggApiOptions Options()
    {
        return new GggApiOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Contact = "ops@example.test",
            Version = "1.2.3"
        };
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubCurrencyExchangeParser(MarketSnapshot snapshot) : IGggCurrencyExchangeParser
    {
        public string? RawJson { get; private set; }

        public MarketSnapshot ParseSnapshot(string rawJson)
        {
            RawJson = rawJson;
            return snapshot;
        }
    }
}
