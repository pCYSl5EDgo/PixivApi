namespace PixivApi.Core.SqliteDatabase;

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

    [StringLiteral.Utf8("SELECT \"Id\" FROM \"ArtworkTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_ExistsArtwork();

    private async ValueTask<bool> ExistsArtworkAsync(ulong id, CancellationToken token)
    {
        var statement = existsArtworkStatement ??= Prepare(Literal_ExistsArtwork(), true, out _);
        Bind(statement, 1, id);
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

                return code == SQLITE_ROW;
            } while (true);
        }
        finally
        {
            Reset(statement);
        }
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


    [StringLiteral.Utf8("INSERT INTO \"UgoiraFrameTable\" VALUES (?1, ?2, ?3")]
    private static partial ReadOnlySpan<byte> Literal_InsertUgoiraFrames_Parts_0();

    private ValueTask InsertUgoiraFramesAsync(ulong id, ushort[] frames, CancellationToken token)
    {
        ref var statement = ref At(ref insertUgoiraFramesStatementArray, frames.Length);
        if (statement is null)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral(Literal_InsertUgoiraFrames_Parts_0());
            for (int i = 1, index = 3; i < frames.Length; i++)
            {
                builder.AppendLiteral(Literal_Insert_TagOrTool_Parts_1());
                builder.Append(++index);
                builder.AppendLiteral(Literal_Comma_Question());
                builder.Append(++index);
            }

            builder.AppendAscii(')');
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
        }

        Bind(statement, 1, id);
        for (int i = 0, offset = 1; i < frames.Length; i++)
        {
            Bind(statement, ++offset, i);
            Bind(statement, ++offset, frames[i]);
        }

        return ExecuteAsync(statement, token);
    }


    [StringLiteral.Utf8("INSERT INTO \"HidePageTable\" VALUES (?1, ?2, ?3")]
    private static partial ReadOnlySpan<byte> Literal_InsertHides_Parts_0();

    private ValueTask InsertHidesAsync(ulong id, Dictionary<uint, HideReason>? dictionary, CancellationToken token)
    {
        if (dictionary is not { Count: > 0 })
        {
            return ValueTask.CompletedTask;
        }

        ref var statement = ref At(ref insertHidesStatementArray, dictionary.Count);
        if (statement is null)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral(Literal_InsertHides_Parts_0());
            for (int i = 1, length = dictionary.Count, index = 3; i < length; i++)
            {
                builder.AppendLiteral(Literal_Insert_TagOrTool_Parts_1());
                builder.Append(++index);
                builder.AppendLiteral(Literal_Comma_Question());
                builder.Append(++index);
            }

            builder.AppendAscii(')');
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
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

    [StringLiteral.Utf8("DELETE FROM \"HidePageTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_DeleteHides();

    private ValueTask DeleteHidesAsync(ulong id, CancellationToken token)
    {
        var statement = deleteHidesStatement ??= Prepare(Literal_DeleteHides(), true, out _);
        Bind(statement, 1, id);
        return ExecuteAsync(statement, token);
    }

    private sqlite3_stmt PrepareInsertToolsStatement(int length)
    {
        ref var statement = ref At(ref insertArtworkToolCrossTableStatementArray, length);
        if (statement is null)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            builder.AppendLiteral(Literal_Update_Tool_Parts_0());
            for (int i = 1, index = 2; i < length; i++)
            {
                builder.AppendLiteral(Literal_Insert_TagOrTool_Parts_1());
                builder.Append(++index);
            }

            builder.AppendAscii(')');
            statement = Prepare(ref builder, true, out _);
            builder.Dispose();
        }

        return statement;
    }

    private async ValueTask InsertToolsOfArtworkAsync(ulong id, uint[] array, CancellationToken token)
    {
        if (array.Length == 0)
        {
            return;
        }

        var statement = PrepareInsertToolsStatement(array.Length);
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

        await ExecuteAsync(statement, token).ConfigureAwait(false);
    }

    [StringLiteral.Utf8("INSERT INTO \"ArtworkTagCrossTable\" VALUES (?1, ?2, ?3")]
    private static partial ReadOnlySpan<byte> Literal_Insert_TagsOfArtwork_Parts_0();

    [StringLiteral.Utf8("), (?1, ?")]
    private static partial ReadOnlySpan<byte> Literal_Insert_TagOrTool_Parts_1();

    private async ValueTask InsertTagsOfArtworkAsync(ulong id, Dictionary<uint, uint> dictionary, CancellationToken token)
    {
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
                builder.AppendLiteral(Literal_Insert_TagsOfArtwork_Parts_0());
                for (int i = 1, length = dictionary.Count, index = 3; i < length; i++)
                {
                    builder.AppendLiteral(Literal_Insert_TagOrTool_Parts_1());
                    builder.Append(++index);
                    builder.AppendLiteral(Literal_Comma_Question());
                    builder.Append(++index);
                }

                builder.AppendAscii(')');
                statement = Prepare(ref builder, true, out _);
                builder.Dispose();
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

    [StringLiteral.Utf8("INSERT INTO \"ArtworkTable\"" +
        " VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19, ?20) " +
        "ON CONFLICT (\"Id\") DO UPDATE SET \"IsVisible\" = \"excluded\".\"IsVisible\", \"IsMuted\" = \"excluded\".\"IsMuted\", " +
            "\"CreateDate\" = \"excluded\".\"CreateDate\", \"FileDate\" = \"excluded\".\"FileDate\", \"TotalView\" = \"excluded\".\"TotalView\"," +
            "\"TotalBookmarks\" = \"excluded\".\"TotalBookmarks\", \"HideReason\" = \"excluded\".\"HideReason\", " +
            "\"IsOfficiallyRemoved\" = \"excluded\".\"IsOfficiallyRemoved\", \"IsBookmarked\" = \"excluded\".\"IsBookmarked\", \"Title\" = \"excluded\".\"Title\"," +
            "\"Caption\" = \"excluded\".\"Caption\", \"Memo\" = \"excluded\".\"Memo\"")]
    private static partial ReadOnlySpan<byte> Literal_InsertArtwork_Parts_0();

    private ValueTask InsertArtworkAsync(Artwork answer, CancellationToken token)
    {
        var statement = insertArtworkStatement ??= Prepare(Literal_InsertArtwork_Parts_0(), true, out _);
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

    [StringLiteral.Utf8("UPDATE OR IGNORE \"ArtworkTable\" SET " +
        "(\"IsVisible\", \"IsMuted\", \"CreateDate\", \"FileDate\", \"TotalView\", \"TotalBookmarks\", \"HideReason\", \"IsOfficiallyRemoved\", \"IsBookmarked\", \"Title\", \"Caption\", \"Memo\") = " +
        "(?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13) " +
        "WHERE \"Id\" = ?1")]
    private static partial ReadOnlySpan<byte> Literal_Update_Artwowrk();

    private ValueTask UpdateArtworkAsync(Artwork answer, CancellationToken token)
    {
        var statement = updateArtworkStatement ??= Prepare(Literal_Update_Artwowrk(), true, out _);
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

    [StringLiteral.Utf8("INSERT INTO \"ArtworkTagCrossTable\" (\"Id\", \"TagId\") VALUES (?1, ?2")]
    private static partial ReadOnlySpan<byte> Literal_Update_Tag_Parts_0();

    [StringLiteral.Utf8("INSERT OR IGNORE INTO \"ArtworkToolCrossTable\" VALUES (?1, ?2")]
    private static partial ReadOnlySpan<byte> Literal_Update_Tool_Parts_0();

    [StringLiteral.Utf8("), (?1, ?")]
    private static partial ReadOnlySpan<byte> Literal_Update_TagOrTool_Parts_0();

    [StringLiteral.Utf8(") ON CONFLICT (\"Id\", \"TagId\") DO UPDATE SET \"ValueKind\" = CASE WHEN \"ValueKind\" = 0 THEN 0 ELSE 1 END")]
    private static partial ReadOnlySpan<byte> Literal_OnConflictIdTagId();

    public async ValueTask<bool> ArtworkAddOrUpdateAsync(ArtworkResponseContent answer, CancellationToken token)
    {
        await InsertOrUpdateArtworkAsync(answer, token).ConfigureAwait(false);
        var rowId = GetLastInsertRowId();
        await DeleteTagsOfArtworkWhereValueKindEquals1StatementAsync(answer.Id, token).ConfigureAwait(false);
        await DeleteToolsOfArtworkStatementAsync(answer.Id, token).ConfigureAwait(false);

        if (answer.Tags is { Length: > 0 })
        {
            sqlite3_stmt PrepareStatement(int length)
            {
                ref var statement = ref At(ref insertArtworkTagCrossTableStatementArray, length);
                if (statement is null)
                {
                    var builder = ZString.CreateUtf8StringBuilder();
                    builder.AppendLiteral(Literal_Update_Tag_Parts_0());
                    for (var i = 1; i < length; i++)
                    {
                        builder.AppendLiteral(Literal_Update_TagOrTool_Parts_0());
                        builder.Append(i + 2);
                    }

                    builder.AppendLiteral(Literal_OnConflictIdTagId());
                    statement = Prepare(ref builder, true, out _);
                    builder.Dispose();
                }

                return statement;
            }

            var statetment = PrepareStatement(answer.Tags.Length);
            Bind(statetment, 1, answer.Id);
            for (var i = 0; i < answer.Tags.Length; i++)
            {
                var id = await RegisterTagAsync(answer.Tags[i].Name, token).ConfigureAwait(false);
                if (id == 0)
                {
                    logger.LogError($"{id} : {answer.Tags[i].Name}");
                }

                Bind(statetment, i + 2, id);
            }

            await ExecuteAsync(statetment, token).ConfigureAwait(false);
        }

        if (answer.Tools is { Length: > 0 })
        {
            var statetment = PrepareInsertToolsStatement(answer.Tools.Length);
            Bind(statetment, 1, answer.Id);
            for (var i = 0; i < answer.Tools.Length; i++)
            {
                var id = await RegisterToolAsync(answer.Tools[i], token).ConfigureAwait(false);
                if (id == 0)
                {
                    logger.LogError($"{id} : {answer.Tools[i]}");
                }

                Bind(statetment, i + 2, id);
            }

            await ExecuteAsync(statetment, token).ConfigureAwait(false);
        }

        return rowId == answer.Id;

    }

    [StringLiteral.Utf8("INSERT INTO \"ArtworkTable\" VALUES " +
        "(?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, 0, 0, ?15, ?16, ?17, NULL) " +
        "ON CONFLICT (\"Id\") DO UPDATE SET \"IsVisible\" = \"excluded\".\"IsVisible\", \"IsMuted\" = \"excluded\".\"IsMuted\", " +
            "\"CreateDate\" = \"excluded\".\"CreateDate\", \"FileDate\" = \"excluded\".\"FileDate\", \"TotalView\" = \"excluded\".\"TotalView\"," +
            "\"TotalBookmarks\" = \"excluded\".\"TotalBookmarks\", " +
            "\"IsOfficiallyRemoved\" = 0, \"IsBookmarked\" = \"excluded\".\"IsBookmarked\", \"Title\" = \"excluded\".\"Title\"," +
            "\"Caption\" = \"excluded\".\"Caption\", \"Memo\" = \"excluded\".\"Memo\"")]
    private static partial ReadOnlySpan<byte> Literal_Update_Artwowrk_ArtworkResponseContent();

    private ValueTask InsertOrUpdateArtworkAsync(ArtworkResponseContent answer, CancellationToken token)
    {
        var statement = insertOrUpdateArtwork_ArtworkResponseContent_Statement ??= Prepare(Literal_Update_Artwowrk_ArtworkResponseContent(), true, out _);
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
        Bind(statement, 0x0f, answer.IsBookmarked);
        Bind(statement, 0x10, answer.Title);
        Bind(statement, 0x11, answer.Caption);
        return ExecuteAsync(statement, token);
    }
}
