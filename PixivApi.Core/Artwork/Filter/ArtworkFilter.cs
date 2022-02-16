namespace PixivApi.Core.Local.Filter;

public sealed class ArtworkFilter : IComparer<Artwork>, IFilter<Artwork>, IJsonOnDeserialized
{
    [JsonPropertyName("bookmark")] public bool? IsBookmark = null;
    [JsonPropertyName("count")] public int? Count = null;
    [JsonPropertyName("date")] public DateTimeFilter? DateTimeFilter = null;
    [JsonPropertyName("file-filter")] public FileExistanceFilter? FileExistanceFilter = null;
    [JsonPropertyName("height")] public MinMaxFilter? Height = null;
    [JsonPropertyName("id-filter")] public IdFilter? IdFilter = null;
    [JsonPropertyName("mute")] public bool? IsMuted = null;
    [JsonPropertyName("offset")] public int Offset = 0;
    [JsonPropertyName("order")] public string? Order = null;
    [JsonPropertyName("page-count")] public MinMaxFilter? PageCount = null;
    [JsonPropertyName("r18")] public bool? R18;
    [JsonPropertyName("tag-filter")] public TagFilter? TagFilter = null;
    [JsonPropertyName("title-filter")] public TagFilter? TitleFilter = null;
    [JsonPropertyName("total-bookmarks")] public MinMaxFilter? TotalBookmarks = null;
    [JsonPropertyName("total-view")] public MinMaxFilter? TotalView = null;
    [JsonPropertyName("type")] public ArtworkType? Type = null;
    [JsonPropertyName("user-filter")] public UserFilter? UserFilter = null;
    [JsonPropertyName("visible")] public bool? IsVisible = null;
    [JsonPropertyName("width")] public MinMaxFilter? Width = null;

    public IEnumerable<Artwork> Limit(IEnumerable<Artwork> collection)
    {
        if (Offset == 0)
        {
            if (Count.HasValue)
            {
                if (Count.Value == 0)
                {
                    return Array.Empty<Artwork>();
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
                    return Array.Empty<Artwork>();
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

    public bool Filter(Artwork artwork)
    {
        if (IsVisible.HasValue && IsVisible.Value != artwork.IsVisible)
        {
            return false;
        }

        if (IsMuted.HasValue && IsMuted.Value != artwork.IsMuted)
        {
            return false;
        }

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

        if (R18.HasValue && R18.Value != artwork.IsXRestricted)
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

        if (UserFilter is not null && !UserFilter.Filter(artwork.UserId))
        {
            return false;
        }


        if (TitleFilter is not null)
        {

        }

        if (TagFilter is not null && !TagFilter.Filter(artwork.Tags, artwork.ExtraTags, artwork.ExtraFakeTags))
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

    public int Compare(Artwork? x, Artwork? y)
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
                c = x.UserId.CompareTo(y.UserId);
                if (c != 0)
                {
                    return c;
                }

                goto default;
            case ArtworkOrderKind.ReverseUserId:
                c = y.UserId.CompareTo(x.UserId);
                if (c != 0)
                {
                    return c;
                }

                goto default;
        }
    }

    public void OnDeserialized()
    {
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

    public ValueTask InitializeAsync(ConcurrentDictionary<ulong, User> userDictionary, StringSet tagSet, CancellationToken cancellationToken)
    {
        if (UserFilter is not null)
        {
            UserFilter.Dictionary = userDictionary;
        }

        return TagFilter?.InitializeAsync(tagSet, cancellationToken) ?? ValueTask.CompletedTask;
    }
}
