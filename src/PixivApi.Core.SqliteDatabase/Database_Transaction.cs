namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
  private sqlite3_stmt? beginExclusiveTransactionStatement;
  private sqlite3_stmt? beginTransactionStatement;
  private sqlite3_stmt? endTransactionStatement;
  private sqlite3_stmt? rollbackTransactionStatement;

  public ValueTask BeginTransactionAsync(CancellationToken token)
  {
    logger.LogDebug("Begin Transaction");
    if (beginTransactionStatement is null)
    {
      beginTransactionStatement = Prepare("BEGIN TRANSACTION"u8, true, out _);
    }
    else
    {
      Reset(beginTransactionStatement);
    }

    return ExecuteAsync(beginTransactionStatement, token);
  }

  public ValueTask BeginExclusiveTransactionAsync(CancellationToken token)
  {
    logger.LogDebug("Begin Exclusive Transaction");
    if (beginExclusiveTransactionStatement is null)
    {
      beginExclusiveTransactionStatement = Prepare("BEGIN EXCLUSIVE TRANSACTION"u8, true, out _);
    }
    else
    {
      Reset(beginExclusiveTransactionStatement);
    }

    return ExecuteAsync(beginExclusiveTransactionStatement, token);
  }

  public async ValueTask EndTransactionAsync(CancellationToken token)
  {
    logger.LogDebug("End Transaction");
    if (endTransactionStatement is null)
    {
      endTransactionStatement = Prepare("END TRANSACTION"u8, true, out _);
    }
    else
    {
      Reset(endTransactionStatement);
    }

    var statement = endTransactionStatement;
    do
    {
      var code = Step(statement);
      if (code == SQLITE_BUSY)
      {
        await RollbackTransactionAsync(token).ConfigureAwait(false);
        logger.LogError("Error writing to the database because it is busy");
        break;
      }

      if (code == SQLITE_DONE)
      {
        break;
      }

      throw new InvalidOperationException($"Error: {code} - {sqlite3_errmsg(database).utf8_to_string()}");
    } while (!token.IsCancellationRequested);
  }

  public ValueTask RollbackTransactionAsync(CancellationToken token)
  {
    logger.LogDebug("Rollback Transaction");
    if (rollbackTransactionStatement is null)
    {
      rollbackTransactionStatement = Prepare("ROLLBACK TRANSACTION"u8, true, out _);
    }
    else
    {
      Reset(rollbackTransactionStatement);
    }

    return ExecuteAsync(rollbackTransactionStatement, token);
  }
}
