namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? beginTransactionStatement;
    private sqlite3_stmt? endTransactionStatement;
    private sqlite3_stmt? rollbackTransactionStatement;

    public async ValueTask BeginTransactionAsync(CancellationToken token)
    {
        logger.LogDebug("Begin Transaction");
        beginTransactionStatement ??= Prepare(Literal_Begin_Transaction(), true, out _);
        await ExecuteAsync(beginTransactionStatement, token).ConfigureAwait(false);
    }

    [StringLiteral.Utf8("BEGIN IMMEDIATE TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_Begin_Transaction();
    [StringLiteral.Utf8("END TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_End_Transaction();
    [StringLiteral.Utf8("ROLLBACK TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_Rollback_Transaction();

    public void EndTransaction()
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

        Step(endTransactionStatement);
    }

    public void RollbackTransaction()
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

        Step(rollbackTransactionStatement);
    }
}
