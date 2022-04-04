namespace PixivApi.Core.SqliteDatabase;

internal sealed partial class Database
{
    private sqlite3_stmt? getArtworkStatement;
    private sqlite3_stmt? getTagsOfArtworkStatement;
    private sqlite3_stmt? getToolsOfArtworkStatement;
    private sqlite3_stmt? getUgoiraFramesStatement;
    private sqlite3_stmt? getHideReasonsStatement;
    private sqlite3_stmt? enumerateArtworkStatement;
    private sqlite3_stmt? officiallyRemoveArtworkStatement;
    private sqlite3_stmt? selectHidePagesStatement;

    [StringLiteral.Utf8("SELECT \"UserId\", \"PageCount\", \"Width\", \"Height\", " +
        "\"Type\", \"Extension\", \"IsXRestricted\", \"IsVisible\", \"IsMuted\"," +
        "\"CreateDate\", \"FileDate\", \"TotalView\", \"TotalBookmarks\", \"HideReason\"," +
        "\"IsOfficiallyRemoved\", \"IsBookmarked\", \"Title\", \"Caption\", \"Memo\"" +
        "FROM \"ArtworkTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_SelectArtwork_FromArtworkTable_WhereId();

    [StringLiteral.Utf8("SELECT \"TagId\", \"ValueKind\" FROM \"ArtworkTagCrossTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_SelectTagId_FromArtworkTagCrossTable_WhereId();

    [StringLiteral.Utf8("SELECT \"ToolId\" FROM \"ArtworkToolCrossTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_SelectToolId_FromArtworkToolCrossTable_WhereId();

    private async ValueTask ColumnTagsAsync(Artwork artwork, CancellationToken token)
    {
        if (getTagsOfArtworkStatement is null)
        {
            getTagsOfArtworkStatement = Prepare(Literal_SelectTagId_FromArtworkTagCrossTable_WhereId(), true, out _);
        }
        else
        {
            Reset(getTagsOfArtworkStatement);
        }

        var statement = getTagsOfArtworkStatement;
        Bind(statement, 1, artwork.Id);
        do
        {
            var code = Step(statement);
            while (code == SQLITE_BUSY)
            {
                await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
                code = Step(statement);
            }

            if (code == SQLITE_DONE)
            {
                return;
            }

            var tagId = CU32(statement, 0);
            var kind = CU32(statement, 1);
            switch (kind)
            {
                case 0:
                    Array.Resize(ref artwork.ExtraFakeTags, (artwork.ExtraFakeTags?.Length ?? 0) + 1);
                    artwork.ExtraFakeTags[^1] = tagId;
                    break;
                case 1:
                    Array.Resize(ref artwork.Tags, artwork.Tags.Length + 1);
                    artwork.Tags[^1] = tagId;
                    break;
                case 2:
                    Array.Resize(ref artwork.ExtraTags, (artwork.ExtraTags?.Length ?? 0) + 1);
                    artwork.ExtraTags[^1] = tagId;
                    break;
                default:
                    break;
            }
        } while (true);
    }

    private async ValueTask ColumnToolsAsync(Artwork answer, CancellationToken token)
    {
        if (getToolsOfArtworkStatement is null)
        {
            getToolsOfArtworkStatement = Prepare(Literal_SelectToolId_FromArtworkToolCrossTable_WhereId(), true, out _);
        }
        else
        {
            Reset(getToolsOfArtworkStatement);
        }

        var statement = getToolsOfArtworkStatement;
        Bind(statement, 1, answer.Id);
        answer.Tools = await CU32ArrayAsync(statement, token).ConfigureAwait(false);
    }

    public async ValueTask<Artwork?> GetArtworkAsync(ulong id, CancellationToken token)
    {
        if (id == 0)
        {
            return null;
        }

        if (getArtworkStatement is null)
        {
            getArtworkStatement = Prepare(Literal_SelectArtwork_FromArtworkTable_WhereId(), true, out _);
        }
        else
        {
            Reset(getArtworkStatement);
        }

        var statement = getArtworkStatement;
        Bind(statement, 1, id);
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
                return null;
            }


            var answer = new Artwork()
            {
                Id = id,
            };

            await ColumnArtworkAsync(answer, statement, 0, token).ConfigureAwait(false);
            return answer;
        } while (true);
    }

    private async ValueTask ColumnArtworkAsync(Artwork answer, sqlite3_stmt statement, int offset, CancellationToken token)
    {
        ColumnArtwork(answer, statement, offset);
        await ColumnToolsAsync(answer, token).ConfigureAwait(false);
        await ColumnTagsAsync(answer, token).ConfigureAwait(false);
        await ColumnHideReasonsAsync(answer, token).ConfigureAwait(false);
        await ColumnUgoiraFramesAsync(answer, token).ConfigureAwait(false);
    }

    private static void ColumnArtwork(Artwork answer, sqlite3_stmt statement, int offset)
    {
        answer.UserId = CU64(statement, offset++);
        answer.PageCount = CU32(statement, offset++);
        answer.Width = CU32(statement, offset++);
        answer.Height = CU32(statement, offset++);
        answer.Type = (ArtworkType)sqlite3_column_int(statement, offset++);
        answer.Extension = (FileExtensionKind)sqlite3_column_int(statement, offset++);
        answer.IsXRestricted = CBool(statement, offset++);
        answer.IsVisible = CBool(statement, offset++);
        answer.IsMuted = CBool(statement, offset++);

        if (!DateTime.TryParse(CStr(statement, offset++), out answer.CreateDate))
        {
            answer.CreateDate = default;
        }

        if (!DateTime.TryParse(CStr(statement, offset++), out answer.FileDate))
        {
            answer.FileDate = default;
        }

        answer.TotalView = CU64(statement, offset++);
        answer.TotalBookmarks = CU64(statement, offset++);
        answer.ExtraHideReason = (HideReason)sqlite3_column_int(statement, offset++);
        answer.IsOfficiallyRemoved = CBool(statement, offset++);
        answer.IsBookmarked = CBool(statement, offset++);
        answer.Title = CStr(statement, offset++) ?? string.Empty;
        answer.Caption = CStr(statement, offset++) ?? string.Empty;
        answer.ExtraMemo = CStr(statement, offset++);
    }

    [StringLiteral.Utf8("SELECT \"Delay\" FROM \"UgoiraFrameTable\" WHERE \"Id\" = ? ORDER BY \"Index\" ASC")]
    private static partial ReadOnlySpan<byte> Literal_SelectUgoiraFrames();

    private async ValueTask ColumnUgoiraFramesAsync(Artwork answer, CancellationToken token)
    {
        if (getUgoiraFramesStatement is null)
        {
            getUgoiraFramesStatement = Prepare(Literal_SelectUgoiraFrames(), true, out _);
        }
        else
        {
            Reset(getUgoiraFramesStatement);
        }

        var statement = getUgoiraFramesStatement;
        Bind(statement, 1, answer.Id);
        answer.UgoiraFrames = await CU16ArrayAsync(statement, token).ConfigureAwait(false);
        if (answer.UgoiraFrames.Length == 0)
        {
            answer.UgoiraFrames = null;
        }
    }

    [StringLiteral.Utf8("SELECT \"Index\", \"HideReason\" FROM \"HidePageTable\" WHERE \"Id\" = ?")]
    private static partial ReadOnlySpan<byte> Literal_SelectHideReasons();

    private async ValueTask ColumnHideReasonsAsync(Artwork answer, CancellationToken token)
    {
        if (getHideReasonsStatement is null)
        {
            getHideReasonsStatement = Prepare(Literal_SelectHideReasons(), true, out _);
        }
        else
        {
            Reset(getHideReasonsStatement);
        }

        var statement = getHideReasonsStatement;
        Bind(statement, 1, answer.Id);
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
                break;
            }

            var index = CU32(statement, 0);
            var reason = (HideReason)(byte)CI32(statement, 1);
            (answer.ExtraPageHideReasonDictionary ??= new())[index] = reason;
        } while (!token.IsCancellationRequested);
    }

    private const string EnumerateArtworkQuery = "SELECT \"Origin\".\"Id\", \"Origin\".\"UserId\", \"Origin\".\"PageCount\", \"Origin\".\"Width\", \"Origin\".\"Height\", \"Origin\".\"Type\", \"Origin\".\"Extension\", \"Origin\".\"IsXRestricted\", \"Origin\".\"IsVisible\", \"Origin\".\"IsMuted\", \"Origin\".\"CreateDate\", \"Origin\".\"FileDate\", \"Origin\".\"TotalView\", \"Origin\".\"TotalBookmarks\", \"Origin\".\"HideReason\", \"Origin\".\"IsOfficiallyRemoved\", \"Origin\".\"IsBookmarked\", \"Origin\".\"Title\", \"Origin\".\"Caption\", \"Origin\".\"Memo\" FROM \"ArtworkTable\" AS \"Origin\"";

    [StringLiteral.Utf8(EnumerateArtworkQuery)]
    private static partial ReadOnlySpan<byte> Literal_EnumerateArtwork();

    public async IAsyncEnumerable<Artwork> EnumerateArtworkAsync([EnumeratorCancellation] CancellationToken token)
    {
        if (enumerateArtworkStatement is null)
        {
            enumerateArtworkStatement = Prepare(Literal_EnumerateArtwork(), true, out _);
        }
        else
        {
            Reset(enumerateArtworkStatement);
        }

        var statement = enumerateArtworkStatement;
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

            var answer = new Artwork
            {
                Id = id,
            };

            await ColumnArtworkAsync(answer, statement, 1, token).ConfigureAwait(false);
            yield return answer;
        } while (true);
    }

    [StringLiteral.Utf8("INSERT OR IGNORE INTO \"ArtworkRemoveTable\" VALUES (?)")]
    private static partial ReadOnlySpan<byte> Literal_Remove_Artwork();

    public async ValueTask OfficiallyRemoveArtwork(ulong id, CancellationToken token)
    {
        if (officiallyRemoveArtworkStatement is null)
        {
            officiallyRemoveArtworkStatement = Prepare(Literal_Remove_Artwork(), true, out _);
        }
        else
        {
            Reset(officiallyRemoveArtworkStatement);
        }

        var statement = officiallyRemoveArtworkStatement;
        Bind(statement, 1, id);
        do
        {
            var code = Step(statement);
            if (code != SQLITE_BUSY)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1d), token).ConfigureAwait(false);
        } while (!token.IsCancellationRequested);
    }

    [StringLiteral.Utf8("SELECT \"Index\" FROM \"HidePageTable\" WHERE \"Id\" = ? AND \"HideReason\" <> 0 ORDER BY \"Index\" ASC")]
    private static partial ReadOnlySpan<byte> Literal_SelectHidePageFromHidePageTableOfId();

    private async IAsyncEnumerable<uint> SelectHidePageOfId(ulong id, [EnumeratorCancellation] CancellationToken token)
    {
        if (selectHidePagesStatement is null)
        {
            selectHidePagesStatement = Prepare(Literal_SelectHidePageFromHidePageTableOfId(), true, out _);
        }
        else
        {
            Reset(selectHidePagesStatement);
        }

        var statement = selectHidePagesStatement;
        Bind(statement, 1, id);
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
                break;
            }

            if (code == SQLITE_ROW)
            {
                yield return CU32(statement, 0);
                continue;
            }

            throw new InvalidOperationException($"Error: {sqlite3_errmsg(database).utf8_to_string()}");
        } while (!token.IsCancellationRequested);
    }

    [StringLiteral.Utf8(" WHERE ")]
    private static partial ReadOnlySpan<byte> Literal_Where();

    [StringLiteral.Utf8("SELECT \"Origin\".\"Id\", \"Origin\".\"PageCount\", \"Origin\".\"Type\", \"Origin\".\"Extension\" FROM \"ArtworkTable\" AS \"Origin\"")]
    private static partial ReadOnlySpan<byte> Literal_EnumerateArtworkForFileExistance();

    /// <summary>
    /// When FileExistanceFilter exists, ignore Offset, Count and FileExistanceFilter
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<Artwork> FilterAsync(ArtworkFilter filter, [EnumeratorCancellation] CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            yield break;
        }

        sqlite3_stmt PrepareStatement()
        {
            var builder = ZString.CreateUtf8StringBuilder();
            var first = true;
            int intersectArtwork = -1, exceptArtwork = -1, intersectUser = -1, exceptUser = -1;
            FilterUtility.Preprocess(ref builder, filter, ref first, ref intersectArtwork, ref exceptArtwork, ref intersectUser, ref exceptUser);
            builder.AppendLiteral(Literal_EnumerateArtworkForFileExistance());
            builder.AppendLiteral(Literal_Where());
            var statement = FilterUtility.CreateStatement(database, ref builder, filter, logger, intersectArtwork, exceptArtwork, intersectUser, exceptUser);
            builder.Dispose();
            return statement;
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

                if (filter.ShouldHandleFileExistanceFilter)
                {
                    continue;
                }

                var answer = await GetArtworkAsync(id, token).ConfigureAwait(false);
                if (answer is null)
                {
                    continue;
                }

                yield return answer;
            } while (!token.IsCancellationRequested);
        }
        finally
        {
            statement.manual_close();
        }
    }
}
