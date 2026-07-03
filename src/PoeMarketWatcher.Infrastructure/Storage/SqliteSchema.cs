using Microsoft.Data.Sqlite;

namespace PoeMarketWatcher.Infrastructure.Storage;

public static class SqliteSchema
{
    public static async Task EnsureCreatedAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS snapshots (
                change_id INTEGER PRIMARY KEY,
                snapshot_at_utc TEXT NOT NULL,
                raw_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS pair_metrics (
                change_id INTEGER NOT NULL,
                market_id TEXT NOT NULL,
                left_currency TEXT NOT NULL,
                right_currency TEXT NOT NULL,
                exchange_rate TEXT NOT NULL,
                traded_volume TEXT NOT NULL,
                liquidity_score TEXT NOT NULL,
                PRIMARY KEY (change_id, market_id),
                FOREIGN KEY (change_id) REFERENCES snapshots(change_id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS alerts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                market_id TEXT NOT NULL,
                signal TEXT NOT NULL,
                emitted_at_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_snapshots_snapshot_at_utc_change_id
                ON snapshots (snapshot_at_utc DESC, change_id DESC);

            CREATE INDEX IF NOT EXISTS idx_alerts_emitted_at_utc_id
                ON alerts (emitted_at_utc DESC, id DESC);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static async Task<SqliteConnection> OpenConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);

        await EnsureCreatedAsync(connection, cancellationToken);
        return connection;
    }
}
