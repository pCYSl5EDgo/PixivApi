namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? countArtworkStatement;
    private sqlite3_stmt? countUserStatement;
    private sqlite3_stmt? countTagStatement;
    private sqlite3_stmt? countToolStatement;
    private sqlite3_stmt? countRankingStatement;

    private sqlite3_stmt PrepareCountStatement(ReadOnlySpan<byte> column, ReadOnlySpan<byte> table)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.AppendLiteral("SELECT count("u8);
        builder.AppendLiteral(column);
        builder.AppendLiteral(") FROM "u8);
        builder.AppendLiteral(table);
        var statement = Prepare(ref builder, true, out _);
        builder.Dispose();
        return statement;
    }

    private async ValueTask<ulong> CountAsync(sqlite3_stmt statement, CancellationToken token)
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

    public ValueTask<ulong> CountArtworkAsync(CancellationToken token)
    {
        if (countArtworkStatement is null)
        {
            countArtworkStatement = PrepareCountStatement("\"Id\""u8, "\"ArtworkTable\""u8);
        }
        else
        {
            Reset(countArtworkStatement);
        }

        return CountAsync(countArtworkStatement, token);
    }

    public ValueTask<ulong> CountRankingAsync(CancellationToken token)
    {
        if (countRankingStatement is null)
        {
            countRankingStatement = PrepareCountStatement("\"Date\""u8, "\"RankingTable\""u8);
        }
        else
        {
            Reset(countRankingStatement);
        }
        return CountAsync(countRankingStatement, token);
    }

    public ValueTask<ulong> CountTagAsync(CancellationToken token)
    {
        if (countTagStatement is null)
        {
            countTagStatement = PrepareCountStatement("\"Id\""u8, "\"TagTable\""u8);
        }
        else
        {
            Reset(countTagStatement);
        }

        return CountAsync(countTagStatement, token);
    }

    public ValueTask<ulong> CountToolAsync(CancellationToken token)
    {
        if (countToolStatement is null)
        {
            countToolStatement = PrepareCountStatement("\"Id\""u8, "\"ToolTable\""u8);
        }
        else
        {
            Reset(countToolStatement);
        }
        return CountAsync(countToolStatement, token);
    }

    public ValueTask<ulong> CountUserAsync(CancellationToken token)
    {
        if (countUserStatement is null)
        {
            countUserStatement = PrepareCountStatement("\"Id\""u8, "\"UserTable\""u8);
        }
        else
        {
            Reset(countUserStatement);
        }

        return CountAsync(countUserStatement, token);
    }

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
            var first = true;
            int intersectArtwork = -1, exceptArtwork = -1, intersectUser = -1, exceptUser = -1;
            FilterUtility.Preprocess(ref builder, filter, ref first, ref intersectArtwork, ref exceptArtwork, ref intersectUser, ref exceptUser);
            builder.AppendLiteral("SELECT count(\"Origin\".\"Id\") FROM \"ArtworkTable\" AS \"Origin\" WHERE "u8);
            var statement = FilterUtility.CreateStatement(database, ref builder, filter, logger, intersectArtwork, exceptArtwork, intersectUser, exceptUser);
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
