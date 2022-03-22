namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? countArtworkStatement;
    private sqlite3_stmt? countUserStatement;
    private sqlite3_stmt? countTagStatement;
    private sqlite3_stmt? countToolStatement;
    private sqlite3_stmt? countRankingStatement;

    [StringLiteral.Utf8("SELECT COUNT(*) FROM ")] private static partial ReadOnlySpan<byte> Literal_SelectCountFrom();

    private ulong SimpleCount([NotNull] ref sqlite3_stmt? statement, ReadOnlySpan<byte> table)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.AppendLiteral(Literal_SelectCountFrom());
        builder.AppendLiteral(table);
        statement ??= Prepare(ref builder, true, out _);
        builder.Dispose();
        var answer = Step(statement) == SQLITE_ROW ? (ulong)sqlite3_column_int64(statement, 0) : 0;
        Reset(statement);
        return answer;
    }

    public ValueTask<ulong> CountArtworkAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countArtworkStatement, Literal_ArtworkTable()));

    public ValueTask<ulong> CountRankingAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countRankingStatement, Literal_RankingTable()));

    public ValueTask<ulong> CountTagAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countTagStatement, Literal_TagTable()));

    public ValueTask<ulong> CountToolAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countToolStatement, Literal_ToolTable()));

    public ValueTask<ulong> CountUserAsync(CancellationToken token) => ValueTask.FromResult(SimpleCount(ref countUserStatement, Literal_UserTable()));

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
        builder.AppendLiteral(Literal_SelectCountFrom());
        builder.AppendLiteral(Literal_ArtworkTable());
        builder.AppendLiteral(Literal_AsOriginWhere());
        var statement = ArtworkFilterUtility.CreateStatement(database, ref builder, filter);
        var answer = Step(statement) == SQLITE_ROW ? (ulong)sqlite3_column_int64(statement, 0) : 0;
        statement.manual_close();
        return ValueTask.FromResult(answer);
    }
}
