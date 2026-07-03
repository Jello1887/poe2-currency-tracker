namespace PoeMarketWatcher.Infrastructure.Tests.Storage;

internal sealed class TempSqliteDatabase : IAsyncDisposable
{
    private TempSqliteDatabase(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TempSqliteDatabase Create()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"poe-market-watcher-{Guid.NewGuid():N}.sqlite");

        return new TempSqliteDatabase(path);
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }

        return ValueTask.CompletedTask;
    }
}
