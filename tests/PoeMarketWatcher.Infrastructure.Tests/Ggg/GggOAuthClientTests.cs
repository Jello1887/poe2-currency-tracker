using System.Net;
using System.Text;
using PoeMarketWatcher.Infrastructure.Ggg;

namespace PoeMarketWatcher.Infrastructure.Tests.Ggg;

public class GggOAuthClientTests
{
    [Fact]
    public async Task GetClientCredentialsTokenPostsOfficialTokenRequest()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.OK, """
            {
              "access_token": "token-value",
              "token_type": "Bearer",
              "expires_in": 3600,
              "scope": "service:cxapi"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var options = new GggApiOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Contact = "ops@example.test",
            Version = "1.2.3"
        };
        var client = new GggOAuthClient(httpClient, options);

        var token = await client.GetClientCredentialsTokenAsync(CancellationToken.None);

        Assert.Equal("token-value", token.AccessToken);
        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(3600, token.ExpiresIn);
        Assert.Equal("service:cxapi", token.Scope);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://www.pathofexile.com/oauth/token", request.RequestUri?.ToString());
        Assert.Contains("OAuth client-id/1.2.3", request.Headers.UserAgent.ToString(), StringComparison.Ordinal);
        Assert.Contains("(contact: ops@example.test)", request.Headers.UserAgent.ToString(), StringComparison.Ordinal);
        var form = ParseForm(await request.Content!.ReadAsStringAsync(CancellationToken.None));
        Assert.Equal("client-id", form["client_id"]);
        Assert.Equal("client-secret", form["client_secret"]);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("service:cxapi", form["scope"]);
    }

    [Fact]
    public async Task GetClientCredentialsTokenAllowsNullableExpiresIn()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.OK, """
            {
              "access_token": "token-value",
              "token_type": "Bearer",
              "scope": "service:cxapi"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var client = new GggOAuthClient(httpClient, new GggApiOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Contact = "ops@example.test",
            Version = "1.2.3"
        });

        var token = await client.GetClientCredentialsTokenAsync(CancellationToken.None);

        Assert.Null(token.ExpiresIn);
    }

    [Fact]
    public async Task GetClientCredentialsTokenSanitizesSecretFromFailures()
    {
        using var handler = new RecordingHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.BadRequest, """
            {
              "error": "invalid_client",
              "error_description": "client-secret was rejected"
            }
            """));
        using var httpClient = new HttpClient(handler);
        var client = new GggOAuthClient(httpClient, new GggApiOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Contact = "ops@example.test",
            Version = "1.2.3"
        });

        var exception = await Assert.ThrowsAsync<GggApiException>(() =>
            client.GetClientCredentialsTokenAsync(CancellationToken.None));

        Assert.DoesNotContain("client-secret", exception.Message, StringComparison.Ordinal);
        Assert.Contains("400", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetClientCredentialsTokenTreatsMissingFailureBodyAsEmpty()
    {
        using var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        using var httpClient = new HttpClient(handler);
        var client = new GggOAuthClient(httpClient, new GggApiOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Contact = "ops@example.test",
            Version = "1.2.3"
        });

        var exception = await Assert.ThrowsAsync<GggApiException>(() =>
            client.GetClientCredentialsTokenAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Empty(exception.RateLimits.Rules);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static IReadOnlyDictionary<string, string> ParseForm(string form)
    {
        return form
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                parts => WebUtility.UrlDecode(parts[0]),
                parts => WebUtility.UrlDecode(parts[1]),
                StringComparer.Ordinal);
    }
}
