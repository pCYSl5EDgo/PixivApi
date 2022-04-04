namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? getRankingStatement;
    private sqlite3_stmt?[]? addOrUpdateRankingStatementArray;

    [StringLiteral.Utf8("SELECT \"Id\" FROM \"RankingTable\" WHERE \"Date\" = ?1 AND \"RankingKind\" = ?2 ORDER BY \"Index\" ASC")]
    private static partial ReadOnlySpan<byte> Literal_GetRanking();

    public async ValueTask<ulong[]?> GetRankingAsync(DateOnly date, RankingKind kind, CancellationToken token)
    {
        if (getRankingStatement is null)
        {
            getRankingStatement = Prepare(Literal_GetRanking(), true, out _);
        }
        else
        {
            Reset(getRankingStatement);
        }

        var statement = getRankingStatement;
        Bind(statement, 1, date);
        Bind(statement, 2, kind);
        var answer = await CU64ArrayAsync(statement, token).ConfigureAwait(false);
        return answer.Length == 0 ? null : answer;
    }

    [StringLiteral.Utf8("INSERT INTO \"RankingTable\" VALUES (?1, ?2, ?3, ?4")]
    private static partial ReadOnlySpan<byte> Literal_Insert_Into_RankingTable_Part_0();
    
    [StringLiteral.Utf8("), (?1, ?2, ?")]
    private static partial ReadOnlySpan<byte> Literal_Insert_Into_RankingTable_Part_1();

    [StringLiteral.Utf8(") ON CONFLICT (\"Date\", \"RankingKind\", \"Index\") DO UPDATE SET \"Id\" = \"excluded\".\"Id\"")]
    private static partial ReadOnlySpan<byte> Literal_Insert_Into_RankingTable_Part_2();

    [StringLiteral.Utf8(", ?")]
    private static partial ReadOnlySpan<byte> Literal_Comma_Question();

    public async ValueTask AddOrUpdateRankingAsync(DateOnly date, RankingKind kind, ulong[] values, CancellationToken token)
    {
        if (values.Length == 0)
        {
            return;
        }

        sqlite3_stmt PrepareStatement(int length)
        {
            ref var statement = ref At(ref addOrUpdateRankingStatementArray, length);
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.AppendLiteral(Literal_Insert_Into_RankingTable_Part_0());
                for (int i = 1, offset = 4; i < length; i++)
                {
                    builder.AppendLiteral(Literal_Insert_Into_RankingTable_Part_1());
                    builder.Append(++offset);
                    builder.AppendLiteral(Literal_Comma_Question());
                    builder.Append(++offset);
                }

                builder.AppendLiteral(Literal_Insert_Into_RankingTable_Part_2());
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
            }
            else
            {
                Reset(statement);
            }

            return statement;
        }

        var statement = PrepareStatement(values.Length);
        Bind(statement, 1, date);
        Bind(statement, 2, kind);
        for (int i = 0, offset = 2; i < values.Length; i++)
        {
            Bind(statement, ++offset, i);
            Bind(statement, ++offset, values[i]);
        }

        while (Step(statement) == SQLITE_BUSY && !token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
        }
    }

}
