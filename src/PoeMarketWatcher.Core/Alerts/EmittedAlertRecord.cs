namespace PoeMarketWatcher.Core.Alerts;

public sealed record EmittedAlertRecord(
    string PairMarketId,
    AlertSignal Signal,
    DateTimeOffset EmittedAt);
