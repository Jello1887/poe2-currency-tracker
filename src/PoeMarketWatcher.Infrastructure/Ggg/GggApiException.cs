using System.Net;

namespace PoeMarketWatcher.Infrastructure.Ggg;

public class GggApiException : Exception
{
    public GggApiException(HttpStatusCode statusCode, string message, RateLimitHeaders rateLimits)
        : base(message)
    {
        StatusCode = statusCode;
        RateLimits = rateLimits;
    }

    public HttpStatusCode StatusCode { get; }

    public RateLimitHeaders RateLimits { get; }
}

public sealed class GggRateLimitException : GggApiException
{
    public GggRateLimitException(HttpStatusCode statusCode, string message, RateLimitHeaders rateLimits)
        : base(statusCode, message, rateLimits)
    {
        RetryAfter = rateLimits.RetryAfter;
    }

    public TimeSpan? RetryAfter { get; }
}
