using System.Globalization;
using Microsoft.Data.Sqlite;
using PoeMarketWatcher.Core.Alerts;

namespace PoeMarketWatcher.Infrastructure.Storage;

public sealed class SqliteAlertRepository
{
    private readonly string connectionString;

    public SqliteAlertRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString();
    }

    public async Task SaveEmittedAlertsAsync(
        IEnumerable<EmittedAlertRecord> alerts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alerts);

        await using var connection = await SqliteSchema.OpenConnectionAsync(connectionString, cancellationToken);
        using var transaction = connection.BeginTransaction();

        foreach (var alert in alerts)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO alerts (market_id, signal, emitted_at_utc)
                VALUES ($market_id, $signal, $emitted_at_utc);
                """;
            command.Parameters.AddWithValue("$market_id", alert.PairMarketId);
            command.Parameters.AddWithValue("$signal", alert.Signal.ToString());
            command.Parameters.AddWithValue("$emitted_at_utc", FormatDateTime(alert.EmittedAt));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyList<EmittedAlertRecord>> GetRecentAlertsAsync(
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteSchema.OpenConnectionAsync(connectionString, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT market_id, signal, emitted_at_utc
            FROM alerts
            WHERE emitted_at_utc >= $since_utc
            ORDER BY emitted_at_utc DESC, id DESC;
            """;
        command.Parameters.AddWithValue("$since_utc", FormatDateTime(sinceUtc));

        var alerts = new List<EmittedAlertRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            alerts.Add(new EmittedAlertRecord(
                reader.GetString(0),
                Enum.Parse<AlertSignal>(reader.GetString(1)),
                ParseDateTime(reader.GetString(2))));
        }

        return alerts;
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ParseDateTime(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }
}
