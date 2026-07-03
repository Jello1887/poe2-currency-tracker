namespace PoeMarketWatcher.Core.MarketData;

public sealed record MarketSnapshot(
    long ChangeId,
    DateTimeOffset SnapshotAt,
    IReadOnlyList<PairMetric> Metrics,
    string RawJson);
