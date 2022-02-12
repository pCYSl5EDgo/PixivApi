namespace PixivApi;

public sealed class ArtworkDatabaseInfoFilter : IComparer<ArtworkDatabaseInfo>, IAsyncInitailizable, IFilter<ArtworkDatabaseInfo>
{
    [JsonPropertyName("title-filter")] public TagFilter? TitleFilter = null;
    [JsonPropertyName("id-filter")] public IdFilter? IdFilter = null;
    [JsonPropertyName("tag-filter")] public TagFilter? TagFilter = null;
    [JsonPropertyName("user-filter")] public UserFilter? UserFilter = null;
    [JsonPropertyName("file-filter")] public FileExistanceFilter? FileExistanceFilter = null;
    [JsonPropertyName("culture")] public string? Culture = null;
    [JsonPropertyName("total-view")] public MinMaxFilter? TotalView = null;
    [JsonPropertyName("total-bookmarks")] public MinMaxFilter? TotalBookmarks = null;
    [JsonPropertyName("page-count")] public MinMaxFilter? PageCount = null;
    [JsonPropertyName("width")] public MinMaxFilter? Width = null;
    [JsonPropertyName("height")] public MinMaxFilter? Height = null;
    [JsonPropertyName("type")] public ArtworkType? Type = null;
    [JsonPropertyName("date")] public DateTimeFilter? DateTimeFilter = null;
    [JsonPropertyName("r18")] public bool? R18;
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

    public bool IsDateDescending() => OrderKind == ArtworkOrderKind.ReverseId;

    [MemberNotNull(nameof(CompareInfo))]
    public async ValueTask InitializeAsync(string? directory, CancellationToken token)
    {
        CompareInfo = new StringCompareInfo(Culture, IgnoreCase);
        if (UserFilter is not null)
        {
            await UserFilter.InitializeAsync(directory, token).ConfigureAwait(false);
        }

        OrderKind = Order switch
        {
            "view" or "v" => ArtworkOrderKind.View,
            "reverse-view" or "rv" => ArtworkOrderKind.ReverseView,
            "id" or "i" => ArtworkOrderKind.Id,
            "reverse" or "r" or "reverse-id" or "ri" => ArtworkOrderKind.ReverseId,
            "bookmarks" or "b" => ArtworkOrderKind.Bookmarks,
            "reverse-bookmarks" or "rb" => ArtworkOrderKind.ReverseBookmarks,
            "user" or "u" => ArtworkOrderKind.UserId,
            "reverse-user" or "ru" => ArtworkOrderKind.ReverseUserId,
            _ => ArtworkOrderKind.None,
        };
    }

    public bool R18Filter(uint xRestrict) => R18 == null || (R18.Value ? xRestrict == 1 : xRestrict != 1);

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

        if (Width is not null && !Width.Filter(artwork.Width))
        {
            return false;
        }

        if (Height is not null && !Height.Filter(artwork.Height))
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

        if (!R18Filter(artwork.XRestrict))
        {
            return false;
        }

        if (DateTimeFilter is not null && !DateTimeFilter.Filter(artwork.CreateDate))
        {
            return false;
        }

        if (IdFilter is not null && !IdFilter.Filter(artwork.Id))
        {
            return false;
        }

        if (UserFilter is not null && !UserFilter.Filter(artwork.User.Id))
        {
            return false;
        }
        
        
        if (TitleFilter is not null)
        {
            using var segment = ArraySegmentFromPool<string>.Rent(2);
            var span = segment.AsSpan();
            span[0] = artwork.Title;
            span[1] = artwork.Caption;
            if (!TitleFilter.IsMatch(CompareInfo, segment.AsReadOnlySpan()))
            {
                return false;
            }
        }

        if (TagFilter is not null && !TagFilter.IsMatch(CompareInfo, artwork.Tags, artwork.ExtraInfo?.Tags, artwork.ExtraInfo?.FakeTags))
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
    public bool IsOrder => OrderKind != ArtworkOrderKind.None;

    [JsonIgnore]
    public ArtworkOrderKind OrderKind;

    [JsonIgnore]
    public StringCompareInfo CompareInfo = null!;

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
        switch (OrderKind)
        {
            case ArtworkOrderKind.Id:
            default:
                return x.Id.CompareTo(y.Id);
            case ArtworkOrderKind.ReverseId:
                return y.Id.CompareTo(x.Id);
            case ArtworkOrderKind.View:
                c = x.TotalView.CompareTo(y.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case ArtworkOrderKind.ReverseView:
                c = y.TotalView.CompareTo(x.TotalView);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case ArtworkOrderKind.Bookmarks:
                c = x.TotalBookmarks.CompareTo(y.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case ArtworkOrderKind.ReverseBookmarks:
                c = y.TotalBookmarks.CompareTo(x.TotalBookmarks);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case ArtworkOrderKind.UserId:
                c = x.User.Id.CompareTo(y.User.Id);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case ArtworkOrderKind.ReverseUserId:
                c = y.User.Id.CompareTo(x.User.Id);
                if (c != 0)
                {
                    return c;
                }

                goto default;
        }
    }
}
