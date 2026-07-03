using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Core.Digests;

public sealed record MarketDigest(
    MarketSnapshot Snapshot,
    IReadOnlyList<DigestRow> Rows);
