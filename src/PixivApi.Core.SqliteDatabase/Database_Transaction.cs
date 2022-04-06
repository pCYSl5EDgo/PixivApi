namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? beginExclusiveTransactionStatement;
    private sqlite3_stmt? beginTransactionStatement;
    private sqlite3_stmt? endTransactionStatement;
    private sqlite3_stmt? rollbackTransactionStatement;

    [StringLiteral.Utf8("BEGIN TRANSACTION;")] private static partial ReadOnlySpan<byte> Literal_Begin_Transaction();
    
    public ValueTask BeginTransactionAsync(CancellationToken token)
    {
        logger.LogDebug("Begin Transaction");
        if (beginTransactionStatement is null)
        {
            beginTransactionStatement = Prepare(Literal_Begin_Transaction(), true, out _);
        }
        else
        {
            Reset(beginTransactionStatement);
        }

        return ExecuteAsync(beginTransactionStatement, token);
    }

    [StringLiteral.Utf8("BEGIN EXCLUSIVE TRANSACTION;")] private static partial ReadOnlySpan<byte> Literal_Begin_Exclusive_Transaction();

    public ValueTask BeginExclusiveTransactionAsync(CancellationToken token)
    {
        logger.LogDebug("Begin Exclusive Transaction");
        if (beginExclusiveTransactionStatement is null)
        {
            beginExclusiveTransactionStatement = Prepare(Literal_Begin_Exclusive_Transaction(), true, out _);
        }
        else
        {
            Reset(beginExclusiveTransactionStatement);
        }

        return ExecuteAsync(beginExclusiveTransactionStatement, token);
    }
    
    [StringLiteral.Utf8("END TRANSACTION;")] private static partial ReadOnlySpan<byte> Literal_End_Transaction();
    
    public async ValueTask EndTransactionAsync(CancellationToken token)
    {
        logger.LogDebug("End Transaction");
        if (endTransactionStatement is null)
        {
            endTransactionStatement = Prepare(Literal_End_Transaction(), true, out _);
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

    [StringLiteral.Utf8("ROLLBACK TRANSACTION;")] private static partial ReadOnlySpan<byte> Literal_Rollback_Transaction();
    
    public ValueTask RollbackTransactionAsync(CancellationToken token)
    {
        logger.LogDebug("Rollback Transaction");
        if (rollbackTransactionStatement is null)
        {
            rollbackTransactionStatement = Prepare(Literal_Rollback_Transaction(), true, out _);
        }
        else
        {
            Reset(rollbackTransactionStatement);
        }

        return ExecuteAsync(rollbackTransactionStatement, token);
    }
}
