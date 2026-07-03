using Microsoft.Data.Sqlite;
using PoeMarketWatcher.Infrastructure.Storage;

namespace PoeMarketWatcher.Infrastructure.Tests.Storage;

public sealed class SqliteSchemaTests
{
    [Fact]
    public async Task EnsureCreatedAsyncCreatesTimestampIndexesForRecentQueries()
    {
        await using var database = TempSqliteDatabase.Create();
        await using var connection = new SqliteConnection($"Data Source={database.Path};Pooling=False");
        await connection.OpenAsync();

        await SqliteSchema.EnsureCreatedAsync(connection);

        var indexes = await ReadIndexSqlAsync(connection);

        Assert.Contains(
            indexes,
            sql => sql.Contains("snapshots", StringComparison.OrdinalIgnoreCase) &&
                sql.Contains("snapshot_at_utc", StringComparison.OrdinalIgnoreCase) &&
                sql.Contains("change_id", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            indexes,
            sql => sql.Contains("alerts", StringComparison.OrdinalIgnoreCase) &&
                sql.Contains("emitted_at_utc", StringComparison.OrdinalIgnoreCase) &&
                sql.Contains("id", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<IReadOnlyList<string>> ReadIndexSqlAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT sql
            FROM sqlite_master
            WHERE type = 'index'
                AND sql IS NOT NULL
            ORDER BY name;
            """;

        var indexes = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        return indexes;
    }
}
