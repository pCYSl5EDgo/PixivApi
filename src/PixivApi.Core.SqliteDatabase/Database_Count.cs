namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? countArtworkStatement;
    private sqlite3_stmt? countUserStatement;
    private sqlite3_stmt? countTagStatement;
    private sqlite3_stmt? countToolStatement;
    private sqlite3_stmt? countRankingStatement;

    [StringLiteral.Utf8("SELECT count(")] private static partial ReadOnlySpan<byte> Literal_SelectCountFrom_0();

    [StringLiteral.Utf8(") FROM ")] private static partial ReadOnlySpan<byte> Literal_SelectCountFrom_1();

    [StringLiteral.Utf8("\"Id\"")] private static partial ReadOnlySpan<byte> Literal_Id();

    [StringLiteral.Utf8("\"Date\"")] private static partial ReadOnlySpan<byte> Literal_Date();

    private sqlite3_stmt PrepareCountStatement(ReadOnlySpan<byte> column, ReadOnlySpan<byte> table)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.AppendLiteral(Literal_SelectCountFrom_0());
        builder.AppendLiteral(column);
        builder.AppendLiteral(Literal_SelectCountFrom_1());
        builder.AppendLiteral(table);
        var statement = Prepare(ref builder, true, out _);
        builder.Dispose();
        return statement;
    }

    private async ValueTask<ulong> CountAsync(sqlite3_stmt statement, CancellationToken token)
    {
        try
        {
            do
            {
                token.ThrowIfCancellationRequested();
                var code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    continue;
                }

                if (code == SQLITE_ROW)
                {
                    return CU64(statement, 0);
                }

                throw new InvalidOperationException($"Error Code: {code} Message: {sqlite3_errmsg(database).utf8_to_string()}");
            } while (true);
        }
        finally
        {
            Reset(statement);
        }
    }

    public ValueTask<ulong> CountArtworkAsync(CancellationToken token) => CountAsync(countArtworkStatement ??= PrepareCountStatement(Literal_Id(), Literal_ArtworkTable()), token);

    public ValueTask<ulong> CountRankingAsync(CancellationToken token) => CountAsync(countRankingStatement ??= PrepareCountStatement(Literal_Date(), Literal_RankingTable()), token);

    public ValueTask<ulong> CountTagAsync(CancellationToken token) => CountAsync(countTagStatement ??= PrepareCountStatement(Literal_Id(), Literal_TagTable()), token);

    public ValueTask<ulong> CountToolAsync(CancellationToken token) => CountAsync(countToolStatement ??= PrepareCountStatement(Literal_Id(), Literal_ToolTable()), token);

    public ValueTask<ulong> CountUserAsync(CancellationToken token) => CountAsync(countUserStatement ??= PrepareCountStatement(Literal_Id(), Literal_UserTable()), token);

    [StringLiteral.Utf8(" AS \"Origin\" WHERE ")] private static partial ReadOnlySpan<byte> Literal_AsOriginWhere();

    [StringLiteral.Utf8("\"Origin\".")] private static partial ReadOnlySpan<byte> Literal_OriginDot();

    /// <summary>
    /// Ignore Count, Offset and FileExistanceFilter when FileExistanceFilter exists.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async ValueTask<ulong> CountArtworkAsync(ArtworkFilter filter, CancellationToken token)
    {
        sqlite3_stmt PrepareStatement()
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral(Literal_SelectCountFrom_0());
            builder.AppendLiteral(Literal_OriginDot());
            builder.AppendLiteral(Literal_Id());
            builder.AppendLiteral(Literal_SelectCountFrom_1());
            builder.AppendLiteral(Literal_ArtworkTable());
            builder.AppendLiteral(Literal_AsOriginWhere());
            var statement = ArtworkFilterUtility.CreateStatement(database, ref builder, filter, logger);
            builder.Dispose();
            return statement;
        }

        var statement = PrepareStatement();
        try
        {
            do
            {
                token.ThrowIfCancellationRequested();
                var code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    continue;
                }

                if (code == SQLITE_ROW)
                {
                    return CU64(statement, 0);
                }

                throw new InvalidOperationException($"Error Code: {code} Message: {sqlite3_errmsg(database).utf8_to_string()}");
            } while (true);
        }
        finally
        {
            statement.manual_close();
        }
    }
}
