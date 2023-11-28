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
      logger.LogDebug("Initialize database with sql");
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

    logger.LogDebug($"Initialize database @ {path}");
  }

  private readonly ConcurrentBag<Database> Returned = new();
  private readonly ILogger<DatabaseFactory> logger;

  public ValueTask<IDatabase> RentAsync(CancellationToken token)
  {
    token.ThrowIfCancellationRequested();
    if (Returned.TryTake(out var database))
    {
      logger.LogDebug("Rent existing database");
    }
    else
    {
      logger.LogDebug("Create database");
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
      logger.LogDebug("Dispose database");
      database.Dispose();
    }

    logger.LogDebug("Shutdown database");
    sqlite3_shutdown();
    return ValueTask.CompletedTask;
  }

  public void Return([MaybeNull] ref IDatabase database)
  {
    logger.LogTrace("Return database");
    Returned.Add((Database)database);
    database = null;
  }
}
