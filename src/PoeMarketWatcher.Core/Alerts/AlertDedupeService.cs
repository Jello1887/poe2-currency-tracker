namespace PoeMarketWatcher.Core.Alerts;

public static class AlertDedupeService
{
    public static MarketAlertScanResult Filter(
        IReadOnlyList<MarketAlert> alerts,
        IReadOnlyList<EmittedAlertRecord> emittedAlerts,
        DateTimeOffset now,
        AlertOptions? options = null)
    {
        options ??= new AlertOptions();
        var cooldown = options.EffectiveCooldown;
        var activeRecords = emittedAlerts
            .Where(record => now - record.EmittedAt < cooldown)
            .Select(record => (record.PairMarketId, record.Signal))
            .ToHashSet();

        var filteredAlerts = new List<MarketAlert>();
        foreach (var alert in alerts)
        {
            if (activeRecords.Add((alert.Pair.MarketId, alert.Signal)))
            {
                filteredAlerts.Add(alert);
            }
        }

        return new MarketAlertScanResult(
            filteredAlerts,
            alerts.Count - filteredAlerts.Count);
    }
}
