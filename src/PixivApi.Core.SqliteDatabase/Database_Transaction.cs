namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? beginTransactionStatement;
    private sqlite3_stmt? endTransactionStatement;
    private sqlite3_stmt? rollbackTransactionStatement;

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

    [StringLiteral.Utf8("BEGIN EXCLUSIVE TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_Begin_Transaction();
    [StringLiteral.Utf8("END TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_End_Transaction();
    [StringLiteral.Utf8("ROLLBACK TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_Rollback_Transaction();

    public ValueTask EndTransactionAsync(CancellationToken token)
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

        return ExecuteAsync(endTransactionStatement, token);
    }

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
