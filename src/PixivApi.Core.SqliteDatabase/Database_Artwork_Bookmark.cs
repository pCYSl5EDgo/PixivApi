namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    public async IAsyncEnumerable<ulong> DeleteBookmarksAsync(ArtworkFilter filter, [EnumeratorCancellation] CancellationToken token)
    {
        sqlite3_stmt PrepareStatement()
        {
            var builder = ZString.CreateUtf8StringBuilder();
            var first = true;
            int intersectArtwork = -1, exceptArtwork = -1, intersectUser = -1, exceptUser = -1;
            FilterUtility.Preprocess(ref builder, filter, ref first, ref intersectArtwork, ref exceptArtwork, ref intersectUser, ref exceptUser);
            builder.AppendLiteral("""UPDATE "ArtworkTable" AS "Origin" SET "IsBookmarked" = 0 WHERE """u8);
            var and = false;
            FilterUtility.Filter(ref builder, filter, ref and, "\"Origin\""u8, intersectArtwork, exceptArtwork, intersectUser, exceptUser);
            builder.AppendLiteral(" RETURNING \"Id\""u8);
            sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var statement);
            if (logger.IsEnabled(LogLevel.Debug))
            {
#pragma warning disable CA2254
                logger.LogDebug($"Query: {builder}");
#pragma warning restore CA2254
            }
            builder.Dispose();
            return statement;
        }

        if (token.IsCancellationRequested)
        {
            yield break;
        }

        var statement = PrepareStatement();
        try
        {
            do
            {
                var code = Step(statement);
                if (code == SQLITE_BUSY)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                    continue;
                }

                if (code == SQLITE_DONE)
                {
                    yield break;
                }

                var id = CU64(statement, 0);
                if (id == 0)
                {
                    continue;
                }

                yield return id;
            } while (!token.IsCancellationRequested);
        }
        finally
        {
            statement.manual_close();
        }
    }
}
