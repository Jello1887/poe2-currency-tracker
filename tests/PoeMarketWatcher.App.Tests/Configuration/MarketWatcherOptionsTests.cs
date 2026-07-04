using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PoeMarketWatcher.App.Configuration;

namespace PoeMarketWatcher.App.Tests.Configuration;

public sealed class MarketWatcherOptionsTests
{
    [Fact]
    public void AddMarketWatcherOptions_BindsValuesFromMarketWatcherSection()
    {
        MarketWatcherOptions options = BindAndValidate(new Dictionary<string, string?>
        {
            ["MarketWatcher:Ggg:ApiBaseUrl"] = "https://api.pathofexile.com",
            ["MarketWatcher:Ggg:OAuthTokenUrl"] = "https://www.pathofexile.com/oauth/token",
            ["MarketWatcher:Ggg:Scope"] = "service:cxapi",
            ["MarketWatcher:Ggg:ClientId"] = "client-id",
            ["MarketWatcher:Ggg:ClientSecret"] = "client-secret",
            ["MarketWatcher:Ggg:Contact"] = "owner@example.com",
            ["MarketWatcher:Storage:SqlitePath"] = "data/poe-market-watcher.db",
            ["MarketWatcher:Digest:PairCount"] = "10",
            ["MarketWatcher:Alerts:PriceMovementPercent"] = "5",
            ["MarketWatcher:Alerts:VolumeSpikeMultiplier"] = "2",
            ["MarketWatcher:Alerts:CombinedPriceMovementPercent"] = "3",
            ["MarketWatcher:Alerts:CombinedVolumeSpikeMultiplier"] = "1.5",
            ["MarketWatcher:Alerts:MinimumHistorySnapshots"] = "2",
            ["MarketWatcher:Alerts:CooldownHours"] = "6",
            ["MarketWatcher:Alerts:MaxAlertsPerRun"] = "5",
            ["MarketWatcher:Alerts:LowLiquidityVolume"] = "500"
        });

        Assert.Equal(new Uri("https://api.pathofexile.com"), options.Ggg.ApiBaseUrl);
        Assert.Equal(new Uri("https://www.pathofexile.com/oauth/token"), options.Ggg.OAuthTokenUrl);
        Assert.Equal("service:cxapi", options.Ggg.Scope);
        Assert.Equal("client-id", options.Ggg.ClientId);
        Assert.Equal("client-secret", options.Ggg.ClientSecret);
        Assert.Equal("owner@example.com", options.Ggg.Contact);
        Assert.Equal("data/poe-market-watcher.db", options.Storage.SqlitePath);
        Assert.Equal(10, options.Digest.PairCount);
        Assert.Equal(5m, options.Alerts.PriceMovementPercent);
        Assert.Equal(2m, options.Alerts.VolumeSpikeMultiplier);
        Assert.Equal(3m, options.Alerts.CombinedPriceMovementPercent);
        Assert.Equal(1.5m, options.Alerts.CombinedVolumeSpikeMultiplier);
        Assert.Equal(2, options.Alerts.MinimumHistorySnapshots);
        Assert.Equal(6m, options.Alerts.CooldownHours);
        Assert.Equal(5, options.Alerts.MaxAlertsPerRun);
        Assert.Equal(500m, options.Alerts.LowLiquidityVolume);
    }

    [Fact]
    public void Validate_DoesNotRequireDiscordWebhookForNonDiscordRun()
    {
        MarketWatcherOptions options = ValidOptions();
        options.Discord.WebhookUrl = "";

        ValidateOptionsResult result = new MarketWatcherOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RequiresNonDiscordRunValuesByKeyName()
    {
        MarketWatcherOptions options = ValidOptions();
        options.Ggg.ClientId = "";
        options.Ggg.ClientSecret = "";
        options.Ggg.Contact = " ";
        options.Storage.SqlitePath = "";

        ValidateOptionsResult result = new MarketWatcherOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        string[] failures = [.. result.Failures ?? []];
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Ggg:ClientId", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Ggg:ClientSecret", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Ggg:Contact", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Storage:SqlitePath", StringComparison.Ordinal));
        Assert.DoesNotContain("Discord", string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void Validate_RequiresPositiveDigestAndAlertThresholds()
    {
        MarketWatcherOptions options = ValidOptions();
        options.Digest.PairCount = 0;
        options.Alerts.PriceMovementPercent = 0;
        options.Alerts.VolumeSpikeMultiplier = -1;
        options.Alerts.CombinedPriceMovementPercent = 0;
        options.Alerts.CombinedVolumeSpikeMultiplier = 0;
        options.Alerts.MinimumHistorySnapshots = 0;
        options.Alerts.CooldownHours = 0;
        options.Alerts.LowLiquidityVolume = 0;
        options.Alerts.MaxAlertsPerRun = -1;

        ValidateOptionsResult result = new MarketWatcherOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        string[] failures = [.. result.Failures ?? []];
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Digest:PairCount", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:PriceMovementPercent", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:VolumeSpikeMultiplier", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:CombinedPriceMovementPercent", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:CombinedVolumeSpikeMultiplier", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:MinimumHistorySnapshots", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:CooldownHours", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:LowLiquidityVolume", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("MarketWatcher:Alerts:MaxAlertsPerRun", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DoesNotIncludeConfiguredSecretValuesWhenOtherMessagesFail()
    {
        const string secretValue = "sentinel-value-that-must-not-appear";
        MarketWatcherOptions options = ValidOptions();
        options.Digest.PairCount = 0;
        options.Ggg.ClientSecret = secretValue;

        ValidateOptionsResult result = new MarketWatcherOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        string[] failures = [.. result.Failures ?? []];
        Assert.DoesNotContain(secretValue, string.Join(Environment.NewLine, failures));
    }

    private static MarketWatcherOptions BindAndValidate(Dictionary<string, string?> values)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ServiceProvider provider = new ServiceCollection()
            .AddMarketWatcherOptions(configuration)
            .BuildServiceProvider();

        return provider.GetRequiredService<IOptions<MarketWatcherOptions>>().Value;
    }

    private static MarketWatcherOptions ValidOptions()
    {
        return new MarketWatcherOptions
        {
            Ggg =
            {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Contact = "owner@example.com"
            }
        };
    }
}
