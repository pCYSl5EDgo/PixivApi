using PixivApi.Core.Plugin;

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
        logger.LogTrace("Get Artwork");
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

            if (code != SQLITE_ROW)
            {
                throw new InvalidOperationException($"Error: {sqlite3_errmsg(database).utf8_to_string()}");
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
        answer.ExtraPageHideReasonDictionary = await ColumnHideReasonsAsync(answer.Id, token).ConfigureAwait(false);
        await ColumnUgoiraFramesAsync(answer, token).ConfigureAwait(false);
    }

    private void ColumnArtwork(Artwork answer, sqlite3_stmt statement, int offset)
    {
        logger.LogTrace("Column Artwork");
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
        logger.LogTrace("Column Ugoira Frames");
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

    private async ValueTask<Dictionary<uint, HideReason>> ColumnHideReasonsAsync(ulong id, CancellationToken token)
    {
        logger.LogTrace("Column Hide Reasons");
        if (getHideReasonsStatement is null)
        {
            getHideReasonsStatement = Prepare(Literal_SelectHideReasons(), true, out _);
        }
        else
        {
            Reset(getHideReasonsStatement);
        }

        var statement = getHideReasonsStatement;
        Bind(statement, 1, id);
        var answer = new Dictionary<uint, HideReason>();
        do
        {
            token.ThrowIfCancellationRequested();
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
            answer[index] = reason;
        } while (true);
        return answer;
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
                    var pageCount = CU32(statement, 1);
                    var type = (ArtworkType)CU32(statement, 2);
                    var extensionKind = (FileExtensionKind)CU32(statement, 3);
                    if (!await FileFilterAsync(filter.FileExistanceFilter, id, pageCount, type, extensionKind, token).ConfigureAwait(false))
                    {
                        continue;
                    }
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

    private async ValueTask<bool> FileFilterAsync(FileExistanceFilter filter, ulong id, uint pageCount, ArtworkType artworkType, FileExtensionKind extensionKind, CancellationToken token)
    {
        if (filter.Original is null)
        {
            if (filter.Thumbnail is null)
            {
                if (artworkType != ArtworkType.Ugoira || filter.Ugoira is null)
                {
                    return true;
                }
                else
                {
                    var ugoiraValue = filter.Ugoira.HasValue && filter.finder.UgoiraZipFinder.Find(id, extensionKind).Exists == filter.Ugoira.Value;
                    return ugoiraValue;
                }
            }
            else
            {
                var thumbnailValue = await PrivateFilter(id, pageCount, artworkType, extensionKind, filter.Thumbnail, filter.finder.IllustThumbnailFinder, filter.finder.MangaThumbnailFinder, filter.finder.UgoiraThumbnailFinder, token).ConfigureAwait(false);
                if (artworkType != ArtworkType.Ugoira || filter.Ugoira is null)
                {
                    return thumbnailValue;
                }
                else
                {
                    var ugoiraValue = filter.Ugoira.HasValue && filter.finder.UgoiraZipFinder.Find(id, extensionKind).Exists == filter.Ugoira.Value;
                    return filter.Relationship.Calc_Thumbnail_Ugoira(thumbnailValue, ugoiraValue);
                }
            }
        }
        else
        {
            var originalValue = await PrivateFilter(id, pageCount, artworkType, extensionKind, filter.Original, filter.finder.IllustOriginalFinder, filter.finder.MangaOriginalFinder, filter.finder.UgoiraOriginalFinder, token).ConfigureAwait(false);
            if (filter.Thumbnail is null)
            {
                if (artworkType != ArtworkType.Ugoira || filter.Ugoira is null)
                {
                    return originalValue;
                }
                else
                {
                    var ugoiraValue = filter.Ugoira.HasValue && filter.finder.UgoiraZipFinder.Find(id, extensionKind).Exists == filter.Ugoira.Value;
                    return filter.Relationship.Calc_Ogirinal_Ugoira(originalValue, ugoiraValue);
                }
            }
            else
            {
                var thumbnailValue = await PrivateFilter(id, pageCount, artworkType, extensionKind, filter.Thumbnail, filter.finder.IllustThumbnailFinder, filter.finder.MangaThumbnailFinder, filter.finder.UgoiraThumbnailFinder, token).ConfigureAwait(false);
                if (artworkType != ArtworkType.Ugoira || filter.Ugoira is null)
                {
                    return filter.Relationship.Calc_Original_Thumbnail(originalValue, thumbnailValue);
                }
                else
                {
                    var ugoiraValue = filter.Ugoira.HasValue && filter.finder.UgoiraZipFinder.Find(id, extensionKind).Exists == filter.Ugoira.Value;
                    return filter.Relationship.Calc_Original_Thumbnail_Ugoira(originalValue, thumbnailValue, ugoiraValue);
                }
            }
        }
    }

    private ValueTask<bool> PrivateFilter(ulong id, uint pageCount, ArtworkType artworkType, FileExtensionKind extensionKind, FileExistanceFilter.InnerFilter filter, IFinderWithIndex illustFinder, IFinderWithIndex mangaFinder, IFinder ugoiraFinder, CancellationToken token) => artworkType switch
    {
        ArtworkType.Illust => PrivateFilter(id, pageCount, extensionKind, filter, illustFinder, token),
        ArtworkType.Manga => PrivateFilter(id, pageCount, extensionKind, filter, mangaFinder, token),
        ArtworkType.Ugoira => PrivateFilter(id, pageCount, extensionKind, filter, ugoiraFinder, token),
        _ => ValueTask.FromResult(false),
    };

    private async ValueTask<bool> PrivateFilter(ulong id, uint pageCount, FileExtensionKind extensionKind, FileExistanceFilter.InnerFilter filter, IFinderWithIndex finder, CancellationToken token)
    {
        var hideDictionary = await ColumnHideReasonsAsync(id, token).ConfigureAwait(false);
        uint filterPageCount = 0, filterPassCount = 0;
        for (uint i = 0; i < pageCount; i++)
        {
            if (hideDictionary.TryGetValue(i, out var reason) && reason != HideReason.NotHidden)
            {
                continue;
            }

            ++filterPageCount;
            if (finder.Find(id, extensionKind, i).Exists)
            {
                ++filterPassCount;
            }
        }

        var answer = PrivateFilter(filter, filterPassCount, filterPageCount);
        if (logTrace)
        {
#pragma warning disable CA2254
            logger.LogTrace($"Id: {id} PageCount: {pageCount} Extension: {extensionKind} FilterPageCount: {filterPageCount} FilterPassCount: {filterPassCount} Answer: {answer}");
        }

        return answer;
    }

    private async ValueTask<bool> PrivateFilter(ulong id, uint pageCount, FileExtensionKind extensionKind, FileExistanceFilter.InnerFilter filter, IFinder finder, CancellationToken token)
    {
        uint filterPageCount = 0, filterPassCount = 0;
        if (pageCount == 0)
        {
            goto RETURN;
        }

        var hideDictionary = await ColumnHideReasonsAsync(id, token).ConfigureAwait(false);
        if (hideDictionary.TryGetValue(0, out var reason) && reason != HideReason.NotHidden)
        {
            goto RETURN;
        }

        filterPassCount = finder.Find(id, extensionKind).Exists ? 1U : 0U;
        filterPageCount = 1;
    RETURN:
        var answer = PrivateFilter(filter, filterPassCount, filterPageCount);
        if (logTrace)
        {
            logger.LogTrace($"Id: {id} PageCount: {pageCount} Extension: {extensionKind} FilterPageCount: {filterPageCount} FilterPassCount: {filterPassCount} Answer: {answer}");
        }

        return answer;
    }

    private bool PrivateFilter(FileExistanceFilter.InnerFilter filter, uint count, uint pageCount)
    {
        if (filter.IsAllMin)
        {
            return count == pageCount;
        }

        if (count < (filter.Min < 0 ? pageCount : 0) + filter.Min)
        {
            return false;
        }

        if (filter.Max is { } max)
        {
            return count <= (max < 0 ? pageCount : 0) + max;
        }

        return true;
    }
}
