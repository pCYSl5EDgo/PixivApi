using PixivApi.Core.Local;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PixivApi.Core.SqliteDatabase;

public sealed class DatabaseFactory : IDatabaseFactory
{
    private readonly string path;

    public DatabaseFactory(ConfigSettings configSettings)
    {
        path = configSettings.DatabaseFilePath ?? throw new NullReferenceException();
    }

    private readonly ConcurrentBag<Database> Returned = new();

    public ValueTask<IDatabase> RentAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<IDatabase>(token);
        }

        if (!Returned.TryTake(out var database))
        {
            database = new(path);
        }

        return ValueTask.FromResult<IDatabase>(database);
    }

    public ValueTask DisposeAsync()
    {
        while (Returned.TryTake(out var database))
        {
            database.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    public void Return([MaybeNull] ref IDatabase database)
    {
        Returned.Add((Database)database);
        database = null;
    }

}
