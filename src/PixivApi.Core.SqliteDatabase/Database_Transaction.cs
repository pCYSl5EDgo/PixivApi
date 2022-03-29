namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? beginTransactionStatement;
    private sqlite3_stmt? endTransactionStatement;
    private sqlite3_stmt? rollbackTransactionStatement;

    public async ValueTask BeginTransactionAsync(CancellationToken token)
    {
        beginTransactionStatement ??= Prepare(Literal_Begin_Transaction(), true, out _);
        try
        {
            int code;
            while ((code = Step(beginTransactionStatement)) == SQLITE_BUSY)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
            }
        }
        finally
        {
            Reset(beginTransactionStatement);
        }
    }

    [StringLiteral.Utf8("BEGIN IMMEDIATE TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_Begin_Transaction();
    [StringLiteral.Utf8("END TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_End_Transaction();
    [StringLiteral.Utf8("ROLLBACK TRANSACTION")] private static partial ReadOnlySpan<byte> Literal_Rollback_Transaction();

    public void EndTransaction()
    {
        endTransactionStatement ??= Prepare(Literal_End_Transaction(), true, out _);
        Step(endTransactionStatement);
        Reset(endTransactionStatement);
    }

    public void RollbackTransaction()
    {
        rollbackTransactionStatement ??= Prepare(Literal_Rollback_Transaction(), true, out _);
        Step(rollbackTransactionStatement);
        Reset(rollbackTransactionStatement);
    }
}
