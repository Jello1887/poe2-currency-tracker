using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoeMarketWatcher.Infrastructure.Ggg;

public sealed class GggOAuthClient(HttpClient httpClient, GggApiOptions options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GggOAuthToken> GetClientCredentialsTokenAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.OAuthTokenUrl)
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", options.Scope)
            ])
        };
        request.Headers.TryAddWithoutValidation("User-Agent", GggUserAgent.Build(options));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await GggHttpContent.ReadBodyAsStringAsync(response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GggApiException(
                response.StatusCode,
                "GGG OAuth token request failed with HTTP "
                    + $"{(int)response.StatusCode}: {GggSecretSanitizer.Sanitize(body, options.ClientSecret)}",
                ParseRateLimits(response));
        }

        var token = JsonSerializer.Deserialize<GggOAuthTokenResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("GGG OAuth token response is empty.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("GGG OAuth token response is missing access_token.");
        }

        if (string.IsNullOrWhiteSpace(token.TokenType))
        {
            throw new InvalidOperationException("GGG OAuth token response is missing token_type.");
        }

        return new GggOAuthToken(token.AccessToken, token.TokenType, token.ExpiresIn, token.Scope);
    }

    private static RateLimitHeaders ParseRateLimits(HttpResponseMessage response)
    {
        return RateLimitHeaders.Parse(GggHttpHeaders.ToDictionary(response));
    }
}

public sealed record GggOAuthToken(string AccessToken, string TokenType, int? ExpiresIn, string? Scope);

internal sealed record GggOAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("expires_in")] int? ExpiresIn,
    [property: JsonPropertyName("scope")] string? Scope);
