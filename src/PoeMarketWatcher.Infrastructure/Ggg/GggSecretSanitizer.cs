namespace PoeMarketWatcher.Infrastructure.Ggg;

internal static class GggSecretSanitizer
{
    public static string Sanitize(string message, params string?[] secrets)
    {
        var sanitized = message;
        foreach (var secret in secrets)
        {
            if (!string.IsNullOrWhiteSpace(secret))
            {
                sanitized = sanitized.Replace(secret, "[redacted]", StringComparison.Ordinal);
            }
        }

        return sanitized;
    }
}
