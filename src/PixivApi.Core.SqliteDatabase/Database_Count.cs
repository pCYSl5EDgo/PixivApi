namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? countArtworkStatement;
    private sqlite3_stmt? countUserStatement;
    private sqlite3_stmt? countTagStatement;
    private sqlite3_stmt? countToolStatement;
    private sqlite3_stmt? countRankingStatement;

    [StringLiteral.Utf8("SELECT COUNT(")] private static partial ReadOnlySpan<byte> Literal_SelectCountFrom_0();

    [StringLiteral.Utf8(") FROM ")] private static partial ReadOnlySpan<byte> Literal_SelectCountFrom_1();

    [StringLiteral.Utf8("\"Id\"")] private static partial ReadOnlySpan<byte> Literal_Id();

    [StringLiteral.Utf8("\"Date\"")] private static partial ReadOnlySpan<byte> Literal_Date();

    private ulong Count([NotNull] ref sqlite3_stmt? statement, ReadOnlySpan<byte> column, ReadOnlySpan<byte> table)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.AppendLiteral(Literal_SelectCountFrom_0());
        builder.AppendLiteral(column);
        builder.AppendLiteral(Literal_SelectCountFrom_1());
        builder.AppendLiteral(table);
        statement ??= Prepare(ref builder, true, out _);
        builder.Dispose();
        var answer = Step(statement) == SQLITE_ROW ? (ulong)sqlite3_column_int64(statement, 0) : 0;
        Reset(statement);
        return answer;
    }

    public ValueTask<ulong> CountArtworkAsync(CancellationToken token) => ValueTask.FromResult(Count(ref countArtworkStatement, Literal_Id(), Literal_ArtworkTable()));

    public ValueTask<ulong> CountRankingAsync(CancellationToken token) => ValueTask.FromResult(Count(ref countRankingStatement, Literal_Date(), Literal_RankingTable()));

    public ValueTask<ulong> CountTagAsync(CancellationToken token) => ValueTask.FromResult(Count(ref countTagStatement, Literal_Id(), Literal_TagTable()));

    public ValueTask<ulong> CountToolAsync(CancellationToken token) => ValueTask.FromResult(Count(ref countToolStatement, Literal_Id(), Literal_ToolTable()));

    public ValueTask<ulong> CountUserAsync(CancellationToken token) => ValueTask.FromResult(Count(ref countUserStatement, Literal_Id(), Literal_UserTable()));

    [StringLiteral.Utf8(" AS \"Origin\" WHERE ")] private static partial ReadOnlySpan<byte> Literal_AsOriginWhere();

    /// <summary>
    /// Ignore Count, Offset and FileExistanceFilter when FileExistanceFilter exists.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public ValueTask<ulong> CountArtworkAsync(ArtworkFilter filter, CancellationToken token)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.AppendLiteral(Literal_SelectCountFrom_0());
        builder.AppendLiteral(Literal_Id());
        builder.AppendLiteral(Literal_SelectCountFrom_1());
        builder.AppendLiteral(Literal_ArtworkTable());
        builder.AppendLiteral(Literal_AsOriginWhere());
        var statement = ArtworkFilterUtility.CreateStatement(database, ref builder, filter);
        var answer = Step(statement) == SQLITE_ROW ? (ulong)sqlite3_column_int64(statement, 0) : 0;
        statement.manual_close();
        return ValueTask.FromResult(answer);
    }
}
