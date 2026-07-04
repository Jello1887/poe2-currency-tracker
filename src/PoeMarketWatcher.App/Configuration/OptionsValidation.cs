using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PoeMarketWatcher.App.Configuration;

public static class OptionsValidation
{
    public const string SectionName = "MarketWatcher";

    public static IServiceCollection AddMarketWatcherOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MarketWatcherOptions>()
            .Bind(configuration.GetSection(SectionName));

        services.AddSingleton<IValidateOptions<MarketWatcherOptions>, MarketWatcherOptionsValidator>();

        return services;
    }
}

public sealed class MarketWatcherOptionsValidator : IValidateOptions<MarketWatcherOptions>
{
    public ValidateOptionsResult Validate(string? name, MarketWatcherOptions options)
    {
        List<string> failures = [];

        RequireNotBlank(options.Ggg.ClientId, "MarketWatcher:Ggg:ClientId", failures);
        RequireNotBlank(options.Ggg.ClientSecret, "MarketWatcher:Ggg:ClientSecret", failures);
        RequireNotBlank(options.Ggg.Contact, "MarketWatcher:Ggg:Contact", failures);
        RequireNotBlank(options.Storage.SqlitePath, "MarketWatcher:Storage:SqlitePath", failures);

        RequirePositive(options.Digest.PairCount, "MarketWatcher:Digest:PairCount", failures);
        RequirePositive(options.Alerts.PriceMovementPercent, "MarketWatcher:Alerts:PriceMovementPercent", failures);
        RequirePositive(options.Alerts.VolumeSpikeMultiplier, "MarketWatcher:Alerts:VolumeSpikeMultiplier", failures);
        RequirePositive(options.Alerts.CombinedPriceMovementPercent, "MarketWatcher:Alerts:CombinedPriceMovementPercent", failures);
        RequirePositive(options.Alerts.CombinedVolumeSpikeMultiplier, "MarketWatcher:Alerts:CombinedVolumeSpikeMultiplier", failures);
        RequirePositive(options.Alerts.MinimumHistorySnapshots, "MarketWatcher:Alerts:MinimumHistorySnapshots", failures);
        RequirePositive(options.Alerts.CooldownHours, "MarketWatcher:Alerts:CooldownHours", failures);
        RequirePositive(options.Alerts.LowLiquidityVolume, "MarketWatcher:Alerts:LowLiquidityVolume", failures);

        if (options.Alerts.MaxAlertsPerRun < 0)
        {
            failures.Add("MarketWatcher:Alerts:MaxAlertsPerRun must be zero or greater.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireNotBlank(string value, string key, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required.");
        }
    }

    private static void RequirePositive(decimal value, string key, List<string> failures)
    {
        if (value <= 0)
        {
            failures.Add($"{key} must be greater than zero.");
        }
    }

    private static void RequirePositive(int value, string key, List<string> failures)
    {
        if (value <= 0)
        {
            failures.Add($"{key} must be greater than zero.");
        }
    }
}
