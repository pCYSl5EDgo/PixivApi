namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? enumerateHiddenPagesByUserStatement;
    private sqlite3_stmt? enumerateHiddenPagesByArtworkStatement;
    private sqlite3_stmt? enumerateHiddenPagesByPageStatement;

    public async IAsyncEnumerable<HiddenPageValueTuple> EnumerateHiddenPagesAsync([EnumeratorCancellation] CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            yield break;
        }

        if (enumerateHiddenPagesByUserStatement is null)
        {
            enumerateHiddenPagesByUserStatement = Prepare("SELECT \"A\".\"Id\", \"A\".\"Type\", \"A\".\"PageCount\", \"A\".\"Extension\", \"U\".\"HideReason\" "u8 +
                "FROM \"ArtworkTable\" AS \"A\" INNER JOIN \"UserTable\" AS \"U\" ON \"A\".\"UserId\"=\"U\".\"Id\" "u8 +
                "WHERE \"U\".\"HideReason\" > 1"u8, true, out _);
        }
        else
        {
            Reset(enumerateHiddenPagesByUserStatement);
        }

        while (!token.IsCancellationRequested)
        {
            var code = Step(enumerateHiddenPagesByUserStatement);
            if (code == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                continue;
            }

            if (code == SQLITE_DONE)
            {
                break;
            }

            if (code != SQLITE_ROW)
            {
                throw new InvalidOperationException($"Error: {sqlite3_errmsg(database).utf8_to_string()}");
            }

            var id = CU64(enumerateHiddenPagesByUserStatement, 0);
            var type = (ArtworkType)(byte)CI32(enumerateHiddenPagesByUserStatement, 1);
            var pageCount = CI32(enumerateHiddenPagesByUserStatement, 2);
            var extension = (FileExtensionKind)(byte)CI32(enumerateHiddenPagesByUserStatement, 3);
            var reason = (HideReason)(byte)CI32(enumerateHiddenPagesByUserStatement, 4);

            for (var index = 0U; index < pageCount && !token.IsCancellationRequested; index++)
            {
                yield return new(id, index, type, extension, reason);
            }
        }

        if (token.IsCancellationRequested)
        {
            yield break;
        }

        if (enumerateHiddenPagesByArtworkStatement is null)
        {
            enumerateHiddenPagesByArtworkStatement = Prepare("SELECT \"A\".\"Id\", \"A\".\"Type\", \"A\".\"PageCount\", \"A\".\"Extension\", \"A\".\"HideReason\" "u8 +
                "FROM \"ArtworkTable\" AS \"A\" INNER JOIN \"UserTable\" AS \"U\" ON \"A\".\"UserId\"=\"U\".\"Id\" "u8 +
                "WHERE \"A\".\"HideReason\" > 1 AND \"U\".\"HideReason\" <= 1"u8, true, out _);
        }
        else
        {
            Reset(enumerateHiddenPagesByArtworkStatement);
        }

        while (!token.IsCancellationRequested)
        {
            var code = Step(enumerateHiddenPagesByArtworkStatement);
            if (code == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                continue;
            }

            if (code == SQLITE_DONE)
            {
                break;
            }

            if (code != SQLITE_ROW)
            {
                throw new InvalidOperationException($"Error: {sqlite3_errmsg(database).utf8_to_string()}");
            }

            var id = CU64(enumerateHiddenPagesByArtworkStatement, 0);
            var type = (ArtworkType)(byte)CI32(enumerateHiddenPagesByArtworkStatement, 1);
            var pageCount = CI32(enumerateHiddenPagesByArtworkStatement, 2);
            var extension = (FileExtensionKind)(byte)CI32(enumerateHiddenPagesByArtworkStatement, 3);
            var reason = (HideReason)(byte)CI32(enumerateHiddenPagesByArtworkStatement, 4);

            for (var index = 0U; index < pageCount && !token.IsCancellationRequested; index++)
            {
                yield return new(id, index, type, extension, reason);
            }
        }

        if (token.IsCancellationRequested)
        {
            yield break;
        }

        if (enumerateHiddenPagesByPageStatement is null)
        {
            enumerateHiddenPagesByPageStatement = Prepare("SELECT \"H\".\"Id\", \"A\".\"Type\", \"H\".\"Index\", \"A\".\"Extension\", \"A\".\"HideReason\" "u8 +
                "FROM \"HidePageTable\" AS \"H\" INNER JOIN \"ArtworkTable\" AS \"A\" ON \"H\".\"Id\"=\"A\".\"Id\" INNER JOIN \"UserTable\" AS \"U\" ON \"A\".\"UserId\"=\"U\".\"Id\" "u8 +
                "WHERE \"H\".HideReason > 1 AND \"A\".\"HideReason\" <= 1 AND \"U\".\"HideReason\" <= 1"u8, true, out _);
        }
        else
        {
            Reset(enumerateHiddenPagesByPageStatement);
        }

        while (!token.IsCancellationRequested)
        {
            var code = Step(enumerateHiddenPagesByPageStatement);
            if (code == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                continue;
            }

            if (code == SQLITE_DONE)
            {
                break;
            }

            if (code != SQLITE_ROW)
            {
                throw new InvalidOperationException($"Error: {sqlite3_errmsg(database).utf8_to_string()}");
            }

            var id = CU64(enumerateHiddenPagesByPageStatement, 0);
            var type = (ArtworkType)(byte)CI32(enumerateHiddenPagesByPageStatement, 1);
            var index = CU32(enumerateHiddenPagesByPageStatement, 2);
            var extension = (FileExtensionKind)(byte)CI32(enumerateHiddenPagesByPageStatement, 3);
            var reason = (HideReason)(byte)CI32(enumerateHiddenPagesByPageStatement, 4);
            yield return new(id, index, type, extension, reason);
        }
    }
}
