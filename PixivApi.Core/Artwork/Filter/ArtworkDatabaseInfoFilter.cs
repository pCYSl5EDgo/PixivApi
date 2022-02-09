namespace PixivApi;

public sealed class ArtworkDatabaseInfoFilter : IComparer<ArtworkDatabaseInfo>, IAsyncInitailizable, IFilter<ArtworkDatabaseInfo>
{
    [JsonPropertyName("title")] public string[]? Titles;
    [JsonPropertyName("partial-title")] public string[]? PartialTitles;
    [JsonPropertyName("ignore-partial-title")] public string[]? IgnorePartialTitles;
    [JsonPropertyName("id")] public ulong[]? Ids;
    [JsonPropertyName("ignore-id")] public ulong[]? IgnoreIds;
    [JsonPropertyName("tag-filter")] public TagFilter? TagFilter = null;
    [JsonPropertyName("user-filter")] public UserFilter? UserFilter = null;
    [JsonPropertyName("file-filter")] public FileExistanceFilter? FileExistanceFilter = null;
    [JsonPropertyName("culture")] public string? Culture = null;
    [JsonPropertyName("total-view")] public MinMaxFilter? TotalView = null;
    [JsonPropertyName("total-bookmarks")] public MinMaxFilter? TotalBookmarks = null;
    [JsonPropertyName("page-count")] public MinMaxFilter? PageCount = null;
    [JsonPropertyName("type")] public ArtworkType? Type = null;
    [JsonPropertyName("since")] public DateTime? Since;
    [JsonPropertyName("until")] public DateTime? Until;
    [JsonPropertyName("r18")] public bool? R18;
    [JsonPropertyName("partial-title-or")] public bool PartialTitleOr = true;
    [JsonPropertyName("ignore-partial-title-or")] public bool IgnorePartialTitleOr = true;
    [JsonPropertyName("bookmark")] public bool? IsBookmark = null;
    [JsonPropertyName("ignore-case")] public bool IgnoreCase = true;
    [JsonPropertyName("count")] public int? Count = null;
    [JsonPropertyName("offset")] public int Offset = 0;
    [JsonPropertyName("order")] public string? Order = null;

    public IEnumerable<ArtworkDatabaseInfo> Limit(IEnumerable<ArtworkDatabaseInfo> collection)
    {
        if (Offset == 0)
        {
            if (Count.HasValue)
            {
                if (Count.Value == 0)
                {
                    return Array.Empty<ArtworkDatabaseInfo>();
                }
                else
                {
                    return collection.Take(Count.Value);
                }
            }
            else
            {
                return collection;
            }
        }
        else
        {
            if (Count.HasValue)
            {
                if (Count.Value == 0)
                {
                    return Array.Empty<ArtworkDatabaseInfo>();
                }
                else
                {
                    return collection.Skip(Offset).Take(Count.Value);
                }
            }
            else
            {
                return collection.Skip(Offset);
            }
        }
    }

    public bool IsDateDescending() => orderKind == OrderKind.ReverseId;

    public async ValueTask InitializeAsync(string? directory, CancellationToken token)
    {
        if (UserFilter is not null)
        {
            await UserFilter.InitializeAsync(directory, token).ConfigureAwait(false);
        }

        CalcOrderKind();
    }

    public bool Filter(ArtworkDatabaseInfo artwork)
    {
        if (TotalView is not null && !TotalView.Filter(artwork.TotalView))
        {
            return false;
        }

        if (TotalBookmarks is not null && !TotalBookmarks.Filter(artwork.TotalBookmarks))
        {
            return false;
        }

        if (PageCount is not null && !PageCount.Filter(artwork.PageCount))
        {
            return false;
        }

        if (IsBookmark != null && IsBookmark.Value != artwork.IsBookmarked)
        {
            return false;
        }

        if (Type.HasValue)
        {
            if (artwork.Type != Type.Value)
            {
                return false;
            }
        }

        if (R18 != null)
        {
            if (R18.Value)
            {
                if (artwork.XRestrict != 1)
                {
                    return false;
                }
            }
            else
            {
                if (artwork.XRestrict == 1)
                {
                    return false;
                }
            }
        }

        if (Since != null)
        {
            if (artwork.CreateDate.CompareTo(Since.Value) < 0)
            {
                return false;
            }
        }

        if (Until != null)
        {
            if (artwork.CreateDate.CompareTo(Until.Value) > 0)
            {
                return false;
            }
        }

        if (Ids is { Length: > 0 })
        {
            foreach (var id in Ids)
            {
                if (id == artwork.Id)
                {
                    goto OK;
                }
            }

            return false;
        OK:;
        }

        if (IgnoreIds is { Length: > 0 })
        {
            foreach (var id in IgnoreIds)
            {
                if (id == artwork.Id)
                {
                    return false;
                }
            }
        }

        if (UserFilter is not null && !UserFilter.Filter(artwork.User.Id))
        {
            return false;
        }

        var compareInfo = new StringCompareInfo(Culture, IgnoreCase);
        var title = artwork.Title;
        if (Titles is { Length: > 0 })
        {
            foreach (var _title in Titles)
            {
                if (Equals(title, _title))
                {
                    goto BREAK;
                }
            }

            return false;
        BREAK:;
        }

        var caption = artwork.Caption;
        if (PartialTitles is { Length: > 0 })
        {
            if (PartialTitleOr)
            {
                foreach (var _title in PartialTitles)
                {
                    if (compareInfo.Contains(title, _title) || compareInfo.Contains(caption, _title))
                    {
                        goto BREAK;
                    }
                }

                return false;
            BREAK:;
            }
            else
            {
                foreach (var _title in PartialTitles)
                {
                    if (!compareInfo.Contains(title, _title) && !compareInfo.Contains(caption, _title))
                    {
                        return false;
                    }
                }
            }
        }

        if (IgnorePartialTitles is { Length: > 0 })
        {
            if (IgnorePartialTitleOr)
            {
                foreach (var _title in IgnorePartialTitles)
                {
                    if (compareInfo.Contains(title, _title) || compareInfo.Contains(caption, _title))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var _title in IgnorePartialTitles)
                {
                    if (!compareInfo.Contains(title, _title) && !compareInfo.Contains(caption, _title))
                    {
                        goto OK;
                    }
                }

                return false;
            OK:;
            }
        }

        if (TagFilter is not null && !TagFilter.IsMatch(compareInfo, artwork.Tags))
        {
            return false;
        }

        if (FileExistanceFilter is not null && !FileExistanceFilter.Filter(artwork))
        {
            return false;
        }

        return true;
    }

    [JsonIgnore]
    public bool IsLimit => Count.HasValue || Offset > 0;

    [JsonIgnore]
    public bool IsOrder => orderKind != OrderKind.None;

    private OrderKind orderKind;

    private enum OrderKind
    {
        None,
        Id,
        ReverseId,
        View,
        ReverseView,
        Bookmarks,
        ReverseBookmarks,
        UserId,
        ReverseUserId,
        BookmarksPerView,
        ViewPerBookmarks,
        ViewReverseId,
        ReverseViewReverseId,
        BookmarksReverseId,
        ReverseBookmarksReverseId,
        UserIdReverseId,
        ReverseUserIdReverseId,
        BookmarksPerViewReverseId,
        ViewPerBookmarksReverseId,
    }

    private void CalcOrderKind()
    {
        orderKind = Order switch
        {
            "view" or "v" or "vi" => OrderKind.View,
            "reverse-view" or "rv" or "rvi" => OrderKind.ReverseView,
            "id" or "i" => OrderKind.Id,
            "reverse" or "r" or "reverse-id" or "ri" => OrderKind.ReverseId,
            "bookmarks" or "b" or "bi" => OrderKind.Bookmarks,
            "reverse-bookmarks" or "rb" or "rbi" => OrderKind.ReverseBookmarks,
            "user" or "u" or "ui" => OrderKind.UserId,
            "reverse-user" or "ru" or "rui" => OrderKind.ReverseUserId,
            "bookmarks/view" or "b/v" => OrderKind.BookmarksPerView,
            "view/bookmarks" or "v/b" => OrderKind.ViewPerBookmarks,
            "vri" => OrderKind.ViewReverseId,
            "rvri" => OrderKind.ReverseViewReverseId,
            "bri" => OrderKind.BookmarksReverseId,
            "rbri" => OrderKind.ReverseBookmarksReverseId,
            "uri" => OrderKind.UserIdReverseId,
            "ruri" => OrderKind.ReverseUserIdReverseId,
            "b/vri" => OrderKind.BookmarksPerViewReverseId,
            "v/bri" => OrderKind.ViewPerBookmarksReverseId,
            _ => OrderKind.None,
        };
    }

    public int Compare(ArtworkDatabaseInfo? x, ArtworkDatabaseInfo? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int c;
        switch (orderKind)
        {
            case OrderKind.Id:
            default:
                return x.Id.CompareTo(y.Id);
            case OrderKind.ReverseId:
                return y.Id.CompareTo(x.Id);
            case OrderKind.View:
                c = x.TotalView.CompareTo(y.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.ReverseView:
                c = y.TotalView.CompareTo(x.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.Bookmarks:
                c = x.TotalBookmarks.CompareTo(y.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.ReverseBookmarks:
                c = y.TotalBookmarks.CompareTo(x.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.UserId:
                c = x.User.Id.CompareTo(y.User.Id);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.ReverseUserId:
                c = y.User.Id.CompareTo(x.User.Id);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.BookmarksPerView:
                c = (x.TotalBookmarks / (double)x.TotalView).CompareTo(y.TotalBookmarks / (double)y.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.ViewPerBookmarks:
                c = (x.TotalView / (double)x.TotalBookmarks).CompareTo(y.TotalView / (double)y.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case OrderKind.ViewReverseId:
                c = x.TotalView.CompareTo(y.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
            case OrderKind.ReverseViewReverseId:
                c = y.TotalView.CompareTo(x.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
            case OrderKind.BookmarksReverseId:
                c = x.TotalBookmarks.CompareTo(y.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
            case OrderKind.ReverseBookmarksReverseId:
                c = y.TotalBookmarks.CompareTo(x.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
            case OrderKind.UserIdReverseId:
                c = x.User.Id.CompareTo(y.User.Id);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
            case OrderKind.ReverseUserIdReverseId:
                c = y.User.Id.CompareTo(x.User.Id);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
            case OrderKind.BookmarksPerViewReverseId:
                c = (x.TotalBookmarks / (double)x.TotalView).CompareTo(y.TotalBookmarks / (double)y.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
            case OrderKind.ViewPerBookmarksReverseId:
                c = (x.TotalView / (double)x.TotalBookmarks).CompareTo(y.TotalView / (double)y.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto case OrderKind.ReverseId;
        }
    }
}
