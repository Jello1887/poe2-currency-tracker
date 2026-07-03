using System.Globalization;
using Microsoft.Data.Sqlite;
using PoeMarketWatcher.Core.MarketData;

namespace PoeMarketWatcher.Infrastructure.Storage;

public sealed class SqliteMarketRepository
{
    private readonly string connectionString;

    public SqliteMarketRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString();
    }

    public async Task SaveSnapshotAsync(MarketSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await using var connection = await SqliteSchema.OpenConnectionAsync(connectionString, cancellationToken);
        using var transaction = connection.BeginTransaction();

        await using (var snapshotCommand = connection.CreateCommand())
        {
            snapshotCommand.Transaction = transaction;
            snapshotCommand.CommandText = """
                INSERT OR REPLACE INTO snapshots (change_id, snapshot_at_utc, raw_json)
                VALUES ($change_id, $snapshot_at_utc, $raw_json);
                """;
            snapshotCommand.Parameters.AddWithValue("$change_id", snapshot.ChangeId);
            snapshotCommand.Parameters.AddWithValue("$snapshot_at_utc", FormatDateTime(snapshot.SnapshotAt));
            snapshotCommand.Parameters.AddWithValue("$raw_json", snapshot.RawJson);
            await snapshotCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteMetricsCommand = connection.CreateCommand())
        {
            deleteMetricsCommand.Transaction = transaction;
            deleteMetricsCommand.CommandText = "DELETE FROM pair_metrics WHERE change_id = $change_id;";
            deleteMetricsCommand.Parameters.AddWithValue("$change_id", snapshot.ChangeId);
            await deleteMetricsCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var metric in snapshot.Metrics)
        {
            await using var metricCommand = connection.CreateCommand();
            metricCommand.Transaction = transaction;
            metricCommand.CommandText = """
                INSERT INTO pair_metrics (
                    change_id,
                    market_id,
                    left_currency,
                    right_currency,
                    exchange_rate,
                    traded_volume,
                    liquidity_score)
                VALUES (
                    $change_id,
                    $market_id,
                    $left_currency,
                    $right_currency,
                    $exchange_rate,
                    $traded_volume,
                    $liquidity_score);
                """;
            metricCommand.Parameters.AddWithValue("$change_id", snapshot.ChangeId);
            metricCommand.Parameters.AddWithValue("$market_id", metric.Pair.MarketId);
            metricCommand.Parameters.AddWithValue("$left_currency", metric.Pair.Left);
            metricCommand.Parameters.AddWithValue("$right_currency", metric.Pair.Right);
            metricCommand.Parameters.AddWithValue("$exchange_rate", FormatDecimal(metric.ExchangeRate));
            metricCommand.Parameters.AddWithValue("$traded_volume", FormatDecimal(metric.TradedVolume));
            metricCommand.Parameters.AddWithValue("$liquidity_score", FormatDecimal(metric.LiquidityScore));
            await metricCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    public async Task<MarketSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteSchema.OpenConnectionAsync(connectionString, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT change_id, snapshot_at_utc, raw_json
            FROM snapshots
            ORDER BY snapshot_at_utc DESC, change_id DESC
            LIMIT 1;
            """;

        return await ReadSingleSnapshotAsync(connection, command, cancellationToken);
    }

    public async Task<MarketSnapshot?> GetPreviousSnapshotAsync(
        long changeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteSchema.OpenConnectionAsync(connectionString, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT change_id, snapshot_at_utc, raw_json
            FROM snapshots
            WHERE change_id < $change_id
            ORDER BY change_id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$change_id", changeId);

        return await ReadSingleSnapshotAsync(connection, command, cancellationToken);
    }

    public async Task<IReadOnlyList<MarketSnapshot>> GetRecentSnapshotsAsync(
        DateTimeOffset sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return Array.Empty<MarketSnapshot>();
        }

        await using var connection = await SqliteSchema.OpenConnectionAsync(connectionString, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT change_id, snapshot_at_utc, raw_json
            FROM (
                SELECT change_id, snapshot_at_utc, raw_json
                FROM snapshots
                WHERE snapshot_at_utc >= $since_utc
                ORDER BY snapshot_at_utc DESC, change_id DESC
                LIMIT $limit
            )
            ORDER BY snapshot_at_utc ASC, change_id ASC;
            """;
        command.Parameters.AddWithValue("$since_utc", FormatDateTime(sinceUtc));
        command.Parameters.AddWithValue("$limit", limit);

        var rows = new List<SnapshotRow>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(ReadSnapshotRow(reader));
            }
        }

        var snapshots = new List<MarketSnapshot>(rows.Count);
        foreach (var row in rows)
        {
            snapshots.Add(new MarketSnapshot(
                row.ChangeId,
                row.SnapshotAt,
                await ReadMetricsAsync(connection, row.ChangeId, cancellationToken),
                row.RawJson));
        }

        return snapshots;
    }

    private static async Task<MarketSnapshot?> ReadSingleSnapshotAsync(
        SqliteConnection connection,
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        SnapshotRow? row = null;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                row = ReadSnapshotRow(reader);
            }
        }

        if (row is null)
        {
            return null;
        }

        return new MarketSnapshot(
            row.ChangeId,
            row.SnapshotAt,
            await ReadMetricsAsync(connection, row.ChangeId, cancellationToken),
            row.RawJson);
    }

    private static async Task<IReadOnlyList<PairMetric>> ReadMetricsAsync(
        SqliteConnection connection,
        long changeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT left_currency, right_currency, exchange_rate, traded_volume, liquidity_score
            FROM pair_metrics
            WHERE change_id = $change_id
            ORDER BY rowid ASC;
            """;
        command.Parameters.AddWithValue("$change_id", changeId);

        var metrics = new List<PairMetric>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            metrics.Add(new PairMetric(
                new CurrencyPair(reader.GetString(0), reader.GetString(1)),
                ParseDecimal(reader.GetString(2)),
                ParseDecimal(reader.GetString(3)),
                ParseDecimal(reader.GetString(4))));
        }

        return metrics;
    }

    private static SnapshotRow ReadSnapshotRow(SqliteDataReader reader)
    {
        return new SnapshotRow(
            reader.GetInt64(0),
            ParseDateTime(reader.GetString(1)),
            reader.GetString(2));
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ParseDateTime(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }

    private sealed record SnapshotRow(long ChangeId, DateTimeOffset SnapshotAt, string RawJson);
}
