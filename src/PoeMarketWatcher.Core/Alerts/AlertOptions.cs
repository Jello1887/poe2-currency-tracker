namespace PoeMarketWatcher.Core.Alerts;

public sealed record AlertOptions(
    decimal PriceMovementPercent = 5m,
    decimal VolumeSpikeMultiplier = 2m,
    decimal CombinedPriceMovementPercent = 3m,
    decimal CombinedVolumeSpikeMultiplier = 1.5m,
    int MinimumHistorySnapshots = 2,
    TimeSpan? Cooldown = null,
    int MaxAlertsPerRun = 5,
    decimal LowLiquidityVolume = 500m)
{
    public TimeSpan EffectiveCooldown => Cooldown ?? TimeSpan.FromHours(6);
}
