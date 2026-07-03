using System.Globalization;

namespace PoeMarketWatcher.Infrastructure.Ggg;

public sealed record RateLimitHeaders(
    string? Policy,
    IReadOnlyList<string> Rules,
    IReadOnlyDictionary<string, RateLimitRule> Limits,
    TimeSpan? RetryAfter)
{
    public static RateLimitHeaders Parse(IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        var policy = FirstHeader(headers, "X-Rate-Limit-Policy");
        var rules = ParseRules(FirstHeader(headers, "X-Rate-Limit-Rules"));
        var limits = new Dictionary<string, RateLimitRule>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            var limitHeader = FirstHeader(headers, $"X-Rate-Limit-{rule}");
            var stateHeader = FirstHeader(headers, $"X-Rate-Limit-{rule}-State");

            if (limitHeader is null || stateHeader is null)
            {
                continue;
            }

            limits[rule] = new RateLimitRule(
                ParseLimits(limitHeader),
                ParseStates(stateHeader));
        }

        return new RateLimitHeaders(
            policy,
            rules,
            limits,
            ParseRetryAfter(FirstHeader(headers, "Retry-After")));
    }

    private static string? FirstHeader(IReadOnlyDictionary<string, IEnumerable<string>> headers, string name)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value.FirstOrDefault();
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ParseRules(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static IReadOnlyList<RateLimitLimit> ParseLimits(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseLimit)
            .ToArray();
    }

    private static IReadOnlyList<RateLimitState> ParseStates(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseState)
            .ToArray();
    }

    private static RateLimitLimit ParseLimit(string value)
    {
        var parts = ParseTriple(value);
        return new RateLimitLimit(parts[0], parts[1], parts[2]);
    }

    private static RateLimitState ParseState(string value)
    {
        var parts = ParseTriple(value);
        return new RateLimitState(parts[0], parts[1], parts[2]);
    }

    private static int[] ParseTriple(string value)
    {
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            throw new InvalidOperationException($"Rate limit value '{value}' must contain three colon-separated integers.");
        }

        return parts.Select(part => int.Parse(part, CultureInfo.InvariantCulture)).ToArray();
    }

    private static TimeSpan? ParseRetryAfter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var retryAt))
        {
            var delay = retryAt - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        throw new InvalidOperationException($"Retry-After value '{value}' is not a valid delay or HTTP date.");
    }
}

public sealed record RateLimitRule(IReadOnlyList<RateLimitLimit> Limits, IReadOnlyList<RateLimitState> States);

public sealed record RateLimitLimit(int MaxHits, int PeriodSeconds, int RestrictionSeconds);

public sealed record RateLimitState(int CurrentHits, int PeriodSeconds, int ActiveRestrictionSeconds);
