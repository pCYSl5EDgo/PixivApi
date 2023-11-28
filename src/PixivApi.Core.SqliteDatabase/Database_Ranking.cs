namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
  private sqlite3_stmt? getRankingStatement;
  private sqlite3_stmt?[]? addOrUpdateRankingStatementArray;

  public async ValueTask<ulong[]?> GetRankingAsync(DateOnly date, RankingKind kind, CancellationToken token)
  {
    if (getRankingStatement is null)
    {
      getRankingStatement = Prepare("SELECT \"Id\" FROM \"RankingTable\" WHERE \"Date\" = ?1 AND \"RankingKind\" = ?2 ORDER BY \"Index\" ASC"u8, true, out _);
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
        builder.AppendLiteral("INSERT INTO \"RankingTable\" VALUES (?1, ?2, ?3, ?4"u8);
        for (int i = 1, offset = 4; i < length; i++)
        {
          builder.AppendLiteral("), (?1, ?2, ?"u8);
          builder.Append(++offset);
          builder.AppendLiteral(", ?"u8);
          builder.Append(++offset);
        }

        builder.AppendLiteral(") ON CONFLICT (\"Date\", \"RankingKind\", \"Index\") DO UPDATE SET \"Id\" = \"excluded\".\"Id\""u8);
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
