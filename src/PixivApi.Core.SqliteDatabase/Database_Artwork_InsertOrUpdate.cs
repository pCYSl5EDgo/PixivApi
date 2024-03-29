﻿namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? existsArtworkStatement;
    private sqlite3_stmt? deleteHidesStatement;
    private sqlite3_stmt? updateArtworkStatement;
    private sqlite3_stmt? insertArtworkStatement;
    private sqlite3_stmt? insertOrUpdateArtwork_ArtworkResponseContent_Statement;
    private sqlite3_stmt?[]? insertUgoiraFramesStatementArray;
    private sqlite3_stmt?[]? insertHidesStatementArray;
    private sqlite3_stmt?[]? insertTagsOfArtworkStatementArray;
    private sqlite3_stmt?[]? insertArtworkToolCrossTableStatementArray;
    private sqlite3_stmt?[]? insertArtworkTagCrossTableStatementArray;

    private async ValueTask InsertAsync(Artwork answer, CancellationToken token)
    {
        logger.LogTrace("Insert Async");
        await InsertArtworkAsync(answer, token).ConfigureAwait(false);
        await DeleteTagsOfArtworkStatementAsync(answer.Id, token).ConfigureAwait(false);
        await InsertTagsOfArtworkAsync(answer.Id, answer.CalculateTags(), token).ConfigureAwait(false);
        await DeleteToolsOfArtworkStatementAsync(answer.Id, token).ConfigureAwait(false);
        await InsertToolsOfArtworkAsync(answer.Id, answer.Tools, token).ConfigureAwait(false);
        await DeleteHidesAsync(answer.Id, token).ConfigureAwait(false);
        await InsertHidesAsync(answer.Id, answer.ExtraPageHideReasonDictionary, token).ConfigureAwait(false);
        if (answer.Type == ArtworkType.Ugoira && answer.UgoiraFrames is { Length: > 0 })
        {
            await InsertUgoiraFramesAsync(answer.Id, answer.UgoiraFrames, token).ConfigureAwait(false);
        }
    }

    private async ValueTask<bool> ExistsArtworkAsync(ulong id, CancellationToken token)
    {
        if (existsArtworkStatement is null)
        {
            existsArtworkStatement = Prepare("SELECT \"Id\" FROM \"ArtworkTable\" WHERE \"Id\" = ?"u8, true, out _);
        }
        else
        {
            Reset(existsArtworkStatement);
        }

        var statement = existsArtworkStatement;
        Bind(statement, 1, id);
        do
        {
            token.ThrowIfCancellationRequested();
            var code = Step(statement);
            if (code == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                continue;
            }

            return code == SQLITE_ROW;
        } while (true);
    }

    public async ValueTask<bool> AddOrUpdateAsync(Artwork artwork, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var exists = await ExistsArtworkAsync(artwork.Id, CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await InsertArtworkAsync(artwork, CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await DeleteTagsOfArtworkStatementAsync(artwork.Id, CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await InsertTagsOfArtworkAsync(artwork.Id, artwork.CalculateTags(), CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await DeleteToolsOfArtworkStatementAsync(artwork.Id, CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await InsertToolsOfArtworkAsync(artwork.Id, artwork.Tools, CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await DeleteHidesAsync(artwork.Id, CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await InsertHidesAsync(artwork.Id, artwork.ExtraPageHideReasonDictionary, CancellationToken.None).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (artwork.Type == ArtworkType.Ugoira && artwork.UgoiraFrames is { Length: > 0 })
        {
            await InsertUgoiraFramesAsync(artwork.Id, artwork.UgoiraFrames, CancellationToken.None).ConfigureAwait(false);
        }

        return !exists;
    }

    public async IAsyncEnumerable<bool> AddOrUpdateAsync(IEnumerable<Artwork> collection, [EnumeratorCancellation] CancellationToken token)
    {
        foreach (var answer in collection)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var exists = await ExistsArtworkAsync(answer.Id, CancellationToken.None).ConfigureAwait(false);
            await InsertArtworkAsync(answer, CancellationToken.None).ConfigureAwait(false);
            await DeleteTagsOfArtworkStatementAsync(answer.Id, CancellationToken.None).ConfigureAwait(false);
            await InsertTagsOfArtworkAsync(answer.Id, answer.CalculateTags(), CancellationToken.None).ConfigureAwait(false);
            await DeleteToolsOfArtworkStatementAsync(answer.Id, CancellationToken.None).ConfigureAwait(false);
            await InsertToolsOfArtworkAsync(answer.Id, answer.Tools, CancellationToken.None).ConfigureAwait(false);
            await DeleteHidesAsync(answer.Id, CancellationToken.None).ConfigureAwait(false);
            await InsertHidesAsync(answer.Id, answer.ExtraPageHideReasonDictionary, CancellationToken.None).ConfigureAwait(false);
            if (answer.Type == ArtworkType.Ugoira && answer.UgoiraFrames is { Length: > 0 })
            {
                await InsertUgoiraFramesAsync(answer.Id, answer.UgoiraFrames, CancellationToken.None).ConfigureAwait(false);
            }

            yield return !exists;
        }
    }

    private async ValueTask UpdateAsync(Artwork answer, CancellationToken token)
    {
        logger.LogTrace("Update Async");
        await UpdateArtworkAsync(answer, token).ConfigureAwait(false);
        await DeleteTagsOfArtworkStatementAsync(answer.Id, token).ConfigureAwait(false);
        await InsertTagsOfArtworkAsync(answer.Id, answer.CalculateTags(), token).ConfigureAwait(false);
        await DeleteToolsOfArtworkStatementAsync(answer.Id, token).ConfigureAwait(false);
        await InsertToolsOfArtworkAsync(answer.Id, answer.Tools, token).ConfigureAwait(false);
        await DeleteHidesAsync(answer.Id, token).ConfigureAwait(false);
        await InsertHidesAsync(answer.Id, answer.ExtraPageHideReasonDictionary, token).ConfigureAwait(false);
        if (answer.Type == ArtworkType.Ugoira && answer.UgoiraFrames is { Length: > 0 })
        {
            await InsertUgoiraFramesAsync(answer.Id, answer.UgoiraFrames, token).ConfigureAwait(false);
        }
    }

    private ValueTask InsertUgoiraFramesAsync(ulong id, ushort[] frames, CancellationToken token)
    {
        ref var statement = ref At(ref insertUgoiraFramesStatementArray, frames.Length);
        if (statement is null)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral("INSERT INTO \"UgoiraFrameTable\" VALUES (?1, ?2, ?3"u8);
            for (int i = 1, index = 3; i < frames.Length; i++)
            {
                builder.AppendLiteral("), (?1, ?"u8);
                builder.Append(++index);
                builder.AppendLiteral(", ?"u8);
                builder.Append(++index);
            }

            builder.AppendAscii(')');
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
        }
        else
        {
            Reset(statement);
        }

        logger.LogTrace("Insert Ugoira Frames");
        Bind(statement, 1, id);
        for (int i = 0, offset = 1; i < frames.Length; i++)
        {
            Bind(statement, ++offset, i);
            Bind(statement, ++offset, frames[i]);
        }

        return ExecuteAsync(statement, token);
    }

    private ValueTask InsertHidesAsync(ulong id, Dictionary<uint, HideReason>? dictionary, CancellationToken token)
    {
        if (dictionary is not { Count: > 0 })
        {
            return ValueTask.CompletedTask;
        }

        logger.LogTrace("Insert Hides");
        ref var statement = ref At(ref insertHidesStatementArray, dictionary.Count);
        if (statement is null)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral("INSERT INTO \"HidePageTable\" VALUES (?1, ?2, ?3"u8);
            for (int i = 1, length = dictionary.Count, index = 3; i < length; i++)
            {
                builder.AppendLiteral("), (?1, ?"u8);
                builder.Append(++index);
                builder.AppendLiteral(", ?"u8);
                builder.Append(++index);
            }

            builder.AppendAscii(')');
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
        }
        else
        {
            Reset(statement);
        }

        Bind(statement, 1, id);
        var offset = 1;
        foreach (var (pageIndex, reason) in dictionary)
        {
            Bind(statement, ++offset, pageIndex);
            Bind(statement, ++offset, reason);
        }

        return ExecuteAsync(statement, token);
    }

    private ValueTask DeleteHidesAsync(ulong id, CancellationToken token)
    {
        if (deleteHidesStatement is null)
        {
            deleteHidesStatement = Prepare("DELETE FROM \"HidePageTable\" WHERE \"Id\" = ?"u8, true, out _);
        }
        else
        {
            Reset(deleteHidesStatement);
        }

        logger.LogTrace("Delete Hides");
        var statement = deleteHidesStatement;
        Bind(statement, 1, id);
        return ExecuteAsync(statement, token);
    }

    private sqlite3_stmt PrepareInsertToolsStatement(int length)
    {
        ref var statement = ref At(ref insertArtworkToolCrossTableStatementArray, length);
        if (statement is null)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral("INSERT OR IGNORE INTO \"ArtworkToolCrossTable\" VALUES (?1, ?2"u8);
            for (int i = 1, index = 2; i < length; i++)
            {
                builder.AppendLiteral("), (?1, ?"u8);
                builder.Append(++index);
            }

            builder.AppendAscii(')');
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
        }
        else
        {
            Reset(statement);
        }

        return statement;
    }

    private ValueTask InsertToolsOfArtworkAsync(ulong id, uint[] array, CancellationToken token)
    {
        if (array.Length == 0)
        {
            return ValueTask.CompletedTask;
        }

        var statement = PrepareInsertToolsStatement(array.Length);
        logger.LogTrace("Insert Tools");
        Bind(statement, 1, id);
        for (var i = 0; i < array.Length; i++)
        {
            var vid = array[i];
            if (vid == 0)
            {
                Console.WriteLine("vid");
            }

            Bind(statement, i + 2, vid);
        }

        return ExecuteAsync(statement, token);
    }

    private async ValueTask InsertTagsOfArtworkAsync(ulong id, Dictionary<uint, uint> dictionary, CancellationToken token)
    {
        logger.LogTrace("Insert Tags");
        if (dictionary.Count == 0)
        {
            return;
        }

        sqlite3_stmt PrepareStatement()
        {
            ref var statement = ref At(ref insertTagsOfArtworkStatementArray, dictionary.Count);
            if (statement is null)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                builder.AppendLiteral("INSERT INTO \"ArtworkTagCrossTable\" VALUES (?1, ?2, ?3"u8);
                for (int i = 1, length = dictionary.Count, index = 3; i < length; i++)
                {
                    builder.AppendLiteral("), (?1, ?"u8);
                    builder.Append(++index);
                    builder.AppendLiteral(", ?"u8);
                    builder.Append(++index);
                }

                builder.AppendAscii(')');
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
            }
            else
            {
                Reset(statement);
            }

            return statement;
        }

        var statement = PrepareStatement();
        Bind(statement, 1, id);
        var offset = 1;
        foreach (var (tagId, valueKind) in dictionary)
        {
            Bind(statement, ++offset, tagId);
            Bind(statement, ++offset, valueKind);
        }

        await ExecuteAsync(statement, token).ConfigureAwait(false);
    }

    private ValueTask InsertArtworkAsync(Artwork answer, CancellationToken token)
    {
        if (insertArtworkStatement is null)
        {
            insertArtworkStatement = Prepare("INSERT INTO \"ArtworkTable\""u8 +
                " VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19, ?20) "u8 +
                "ON CONFLICT (\"Id\") DO UPDATE SET \"IsVisible\" = \"excluded\".\"IsVisible\", \"IsMuted\" = \"excluded\".\"IsMuted\", "u8 +
                "\"CreateDate\" = \"excluded\".\"CreateDate\", \"FileDate\" = \"excluded\".\"FileDate\", \"TotalView\" = \"excluded\".\"TotalView\","u8 +
                "\"TotalBookmarks\" = \"excluded\".\"TotalBookmarks\", \"HideReason\" = \"excluded\".\"HideReason\", "u8 +
                "\"IsOfficiallyRemoved\" = \"excluded\".\"IsOfficiallyRemoved\", \"IsBookmarked\" = \"excluded\".\"IsBookmarked\", \"Title\" = \"excluded\".\"Title\","u8 +
                "\"Caption\" = \"excluded\".\"Caption\", \"Memo\" = \"excluded\".\"Memo\""u8, true, out _);
        }
        else
        {
            Reset(insertArtworkStatement);
        }

        var statement = insertArtworkStatement;
        Bind(statement, 0x01, answer.Id);
        Bind(statement, 0x02, answer.UserId);
        Bind(statement, 0x03, answer.PageCount);
        Bind(statement, 0x04, answer.Width);
        Bind(statement, 0x05, answer.Height);
        Bind(statement, 0x06, answer.Type);
        Bind(statement, 0x07, answer.Extension);
        Bind(statement, 0x08, answer.IsXRestricted);
        Bind(statement, 0x09, answer.IsVisible);
        Bind(statement, 0x0a, answer.IsMuted);
        Bind(statement, 0x0b, answer.CreateDate);
        Bind(statement, 0x0c, answer.FileDate);
        Bind(statement, 0x0d, answer.TotalView);
        Bind(statement, 0x0e, answer.TotalBookmarks);
        Bind(statement, 0x0f, answer.ExtraHideReason);
        Bind(statement, 0x10, answer.IsOfficiallyRemoved);
        Bind(statement, 0x11, answer.IsBookmarked);
        Bind(statement, 0x12, answer.Title);
        Bind(statement, 0x13, answer.Caption);
        Bind(statement, 0x14, answer.ExtraMemo);
        return ExecuteAsync(statement, token);
    }

    private ValueTask UpdateArtworkAsync(Artwork answer, CancellationToken token)
    {
        if (updateArtworkStatement is null)
        {
            updateArtworkStatement = Prepare("UPDATE OR IGNORE \"ArtworkTable\" SET "u8 +
                "(\"IsVisible\", \"IsMuted\", \"CreateDate\", \"FileDate\", \"TotalView\", \"TotalBookmarks\", \"HideReason\", \"IsOfficiallyRemoved\", \"IsBookmarked\", \"Title\", \"Caption\", \"Memo\") = "u8 +
                "(?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13) "u8 +
                "WHERE \"Id\" = ?1"u8, true, out _);
        }
        else
        {
            Reset(updateArtworkStatement);
        }

        var statement = updateArtworkStatement;
        Bind(statement, 0x1, answer.Id);
        Bind(statement, 0x2, answer.IsVisible);
        Bind(statement, 0x3, answer.IsMuted);
        Bind(statement, 0x4, answer.CreateDate);
        Bind(statement, 0x5, answer.FileDate);
        Bind(statement, 0x6, answer.TotalView);
        Bind(statement, 0x7, answer.TotalBookmarks);
        Bind(statement, 0x8, answer.ExtraHideReason);
        Bind(statement, 0x9, answer.IsOfficiallyRemoved);
        Bind(statement, 0xa, answer.IsBookmarked);
        Bind(statement, 0xb, answer.Title);
        Bind(statement, 0xc, answer.Caption);
        Bind(statement, 0xd, answer.ExtraMemo);
        return ExecuteAsync(statement, token);
    }

    public async ValueTask<bool> ArtworkAddOrUpdateAsync(ArtworkResponseContent answer, CancellationToken token)
    {
        await InsertOrUpdateUserAsync(answer.User, token).ConfigureAwait(false);
        await InsertOrUpdateArtworkAsync(answer, token).ConfigureAwait(false);
        var rowId = GetLastInsertRowId();
        await DeleteTagsOfArtworkWhereValueKindEquals1StatementAsync(answer.Id, token).ConfigureAwait(false);
        await DeleteToolsOfArtworkStatementAsync(answer.Id, token).ConfigureAwait(false);

        if (answer.Tags is { Length: > 0 })
        {
            logger.LogTrace("Insert Tags");
            sqlite3_stmt PrepareStatement(int length)
            {
                ref var statement = ref At(ref insertArtworkTagCrossTableStatementArray, length);
                if (statement is null)
                {
                    var builder = ZString.CreateUtf8StringBuilder();
                    builder.AppendLiteral("INSERT INTO \"ArtworkTagCrossTable\" (\"Id\", \"TagId\") VALUES (?1, ?2"u8);
                    for (var i = 1; i < length; i++)
                    {
                        builder.AppendLiteral("), (?1, ?"u8);
                        builder.Append(i + 2);
                    }

                    builder.AppendLiteral(") ON CONFLICT (\"Id\", \"TagId\") DO UPDATE SET \"ValueKind\" = CASE WHEN \"ValueKind\" = 0 THEN 0 ELSE 1 END"u8);
                    statement = Prepare(ref builder, true, out _);
                    builder.Dispose();
                }
                else
                {
                    Reset(statement);
                }

                return statement;
            }

            var ids = ArrayPool<uint>.Shared.Rent(answer.Tags.Length);
            for (var i = 0; i < answer.Tags.Length; i++)
            {
                ids[i] = await RegisterTagAsync(answer.Tags[i].Name, token).ConfigureAwait(false);
            }

            var statetment = PrepareStatement(answer.Tags.Length);
            Bind(statetment, 1, answer.Id);
            for (var i = 0; i < answer.Tags.Length; i++)
            {
                Bind(statetment, i + 2, ids[i]);
            }

            ArrayPool<uint>.Shared.Return(ids);

            await ExecuteAsync(statetment, token).ConfigureAwait(false);
            logger.LogTrace("Insert Tags Done");
        }

        if (answer.Tools is { Length: > 0 })
        {
            logger.LogTrace("Insert Tools");
            var ids = ArrayPool<uint>.Shared.Rent(answer.Tools.Length);
            for (var i = 0; i < answer.Tools.Length; i++)
            {
                ids[i] = await RegisterToolAsync(answer.Tools[i], token).ConfigureAwait(false);
            }

            var statetment = PrepareInsertToolsStatement(answer.Tools.Length);
            Bind(statetment, 1, answer.Id);
            for (var i = 0; i < answer.Tools.Length; i++)
            {
                Bind(statetment, i + 2, ids[i]);
            }

            ArrayPool<uint>.Shared.Return(ids);
            await ExecuteAsync(statetment, token).ConfigureAwait(false);
            logger.LogTrace("Insert Tools Done");
        }

        return rowId == answer.Id;
    }

    private ValueTask InsertOrUpdateArtworkAsync(ArtworkResponseContent answer, CancellationToken token)
    {
        if (insertOrUpdateArtwork_ArtworkResponseContent_Statement is null)
        {
            insertOrUpdateArtwork_ArtworkResponseContent_Statement = Prepare("INSERT INTO \"ArtworkTable\" VALUES "u8 +
                "(?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, 0, ?16, ?17, ?18, NULL) "u8 +
                "ON CONFLICT (\"Id\") DO UPDATE SET \"IsVisible\" = \"excluded\".\"IsVisible\", \"IsMuted\" = \"excluded\".\"IsMuted\","u8 +
                "\"CreateDate\" = \"excluded\".\"CreateDate\", \"FileDate\" = \"excluded\".\"FileDate\","u8 +
                "\"TotalView\" = \"excluded\".\"TotalView\","u8 +
                "\"TotalBookmarks\" = \"excluded\".\"TotalBookmarks\","u8 +
                "\"IsOfficiallyRemoved\" = 0, \"Type\" = \"excluded\".\"Type\","u8 +
                "\"HideReason\" = CASE WHEN \"HideReason\" = 0 THEN \"excluded\".\"HideReason\" ELSE \"HideReason\" END,"u8 +
                "\"IsBookmarked\" = \"excluded\".\"IsBookmarked\", \"Extension\" = \"excluded\".\"Extension\","u8 +
                "\"Title\" = \"excluded\".\"Title\", \"Caption\" = \"excluded\".\"Caption\""u8, true, out _);
        }
        else
        {
            Reset(insertOrUpdateArtwork_ArtworkResponseContent_Statement);
        }

        logger.LogTrace("Insert Or Update Artwork by response content");
        var statement = insertOrUpdateArtwork_ArtworkResponseContent_Statement;
        Bind(statement, 0x01, answer.Id);
        Bind(statement, 0x02, answer.User.Id);
        Bind(statement, 0x03, answer.PageCount);
        Bind(statement, 0x04, answer.Width);
        Bind(statement, 0x05, answer.Height);
        Bind(statement, 0x06, answer.Type);
        Bind(statement, 0x07, LocalNetworkConverter.ConvertToFileExtensionKind(answer));
        Bind(statement, 0x08, answer.XRestrict != 0);
        Bind(statement, 0x09, answer.Visible);
        Bind(statement, 0x0a, answer.IsMuted);
        Bind(statement, 0x0b, answer.CreateDate);
        Bind(statement, 0x0c, LocalNetworkConverter.ParseFileDate(answer));
        Bind(statement, 0x0d, answer.TotalView);
        Bind(statement, 0x0e, answer.TotalBookmarks);
        Bind(statement, 0x0f, answer.IsUnknown());
        Bind(statement, 0x10, answer.IsBookmarked);
        Bind(statement, 0x11, answer.Title);
        Bind(statement, 0x12, answer.Caption);
        return ExecuteAsync(statement, token);
    }
}
