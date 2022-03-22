using System.Collections.Concurrent;

namespace PixivApi.Core.SqliteDatabase;

public sealed partial class DatabaseFactory : IDatabaseFactory
{
    private readonly string path;

    public DatabaseFactory(ConfigSettings configSettings, ILogger<DatabaseFactory> logger)
    {
        Batteries_V2.Init();
        sqlite3_initialize();
        path = (configSettings.DatabaseFilePath ?? throw new NullReferenceException()) + ".sqlite3";
        this.logger = logger;
        var info = new FileInfo(path);
        if (!info.Exists || info.Length == 0)
        {
            File.Create(path).Dispose();
            Database database = new(logger, path);
            ReadOnlySpan<byte> span = GetInitSql(), outSpan;
            do
            {
                var pcode = sqlite3_prepare_v3(database.database, span, 0, out var statement, out outSpan);
                if (statement.IsInvalid)
                {
                    statement.manual_close();
                    break;
                }

                if (pcode != 0)
                {
                    ;
                }

                var code = sqlite3_step(statement);
                if (code != SQLITE_DONE)
                {
                    throw new InvalidOperationException(code.ToString());
                }

                statement.manual_close();
                span = outSpan;
            } while (!span.IsEmpty);
            database.Dispose();
        }
    }

    private readonly ConcurrentBag<Database> Returned = new();
    private readonly ILogger<DatabaseFactory> logger;

    public ValueTask<IDatabase> RentAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!Returned.TryTake(out var database))
        {
            database = new(logger, path);
        }

        return ValueTask.FromResult<IDatabase>(database);
    }

    [EmbedResourceCSharp.FileEmbed("init.sql")]
    private static partial ReadOnlySpan<byte> GetInitSql();

    public ValueTask DisposeAsync()
    {
        while (Returned.TryTake(out var database))
        {
            database.Dispose();
        }

        sqlite3_shutdown();
        return ValueTask.CompletedTask;
    }

    public void Return([MaybeNull] ref IDatabase database)
    {
        Returned.Add((Database)database);
        database = null;
    }
}
