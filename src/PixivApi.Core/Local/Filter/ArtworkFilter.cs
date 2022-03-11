using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class ArtworkFilter : IFilter<Artwork>
{
    [JsonPropertyName("bookmark")] public bool? IsBookmark = null;
    [JsonPropertyName("count")] public int? Count = null;
    [JsonPropertyName("date")] public DateTimeFilter? DateTimeFilter = null;
    [JsonPropertyName("file-filter")] public FileExistanceFilter? FileExistanceFilter = null;
    [JsonPropertyName("height")] public MinMaxFilter? Height = null;
    [JsonPropertyName("id-filter")] public IdFilter? IdFilter = null;
    [JsonPropertyName("mute")] public bool? IsMuted = null;
    [JsonPropertyName("offset")] public int Offset = 0;
    [JsonPropertyName("officially-removed")] public bool? IsOfficiallyRemoved = null;
    [JsonPropertyName("order")] public ArtworkOrderKind Order = ArtworkOrderKind.None;
    [JsonPropertyName("page-count")] public MinMaxFilter? PageCount = null;
    [JsonPropertyName("r18")] public bool? R18;
    [JsonPropertyName("tag-filter")] public TagFilter? TagFilter = null;
    [JsonPropertyName("title-filter")] public TextFilter? TitleFilter = null;
    [JsonPropertyName("total-bookmarks")] public MinMaxFilter? TotalBookmarks = null;
    [JsonPropertyName("total-view")] public MinMaxFilter? TotalView = null;
    [JsonPropertyName("type")] public ArtworkType? Type = null;
    [JsonPropertyName("user-filter")] public UserFilter? UserFilter = null;
    [JsonPropertyName("visible")] public bool? IsVisible = null;
    [JsonPropertyName("width")] public MinMaxFilter? Width = null;
    [JsonPropertyName("hide-filter")] public HideFilter? HideFilter = null;

    private IDatabase database = null!;

    public bool HasSlowFilter => FileExistanceFilter is not null || UserFilter is not null;

    public bool FastFilter(Artwork artwork)
    {
        if (HideFilter is null)
        {
            if (artwork.ExtraHideReason != HideReason.NotHidden)
            {
                return false;
            }
        }
        else if (!HideFilter.Filter(artwork.ExtraHideReason))
        {
            return false;
        }


        if (IsOfficiallyRemoved.HasValue && IsOfficiallyRemoved.Value != artwork.IsOfficiallyRemoved)
        {
            return false;
        }

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

        if (TagFilter is not null && !TagFilter.Filter(artwork.Tags, artwork.ExtraTags, artwork.ExtraFakeTags))
        {
            return false;
        }

        if (TitleFilter is not null)
        {
            var (Title, _) = (artwork.Title, artwork.Caption);
            if (!TitleFilter.Filter(MemoryMarshal.CreateReadOnlySpan(ref Title, 2)!))
            {
                return false;
            }
        }

        return true;
    }

    public ulong GetKey(Artwork artwork) => Order switch
    {
        ArtworkOrderKind.View => artwork.TotalView,
        ArtworkOrderKind.ReverseView => ulong.MaxValue - artwork.TotalView,
        ArtworkOrderKind.Bookmarks => artwork.TotalBookmarks,
        ArtworkOrderKind.ReverseBookmarks => ulong.MaxValue - artwork.TotalBookmarks,
        ArtworkOrderKind.UserId => artwork.UserId,
        ArtworkOrderKind.ReverseUserId => ulong.MaxValue - artwork.UserId,
        ArtworkOrderKind.ReverseId => ulong.MaxValue - artwork.Id,
        _ => artwork.Id,
    };

    public async ValueTask InitializeAsync(IDatabase database, Func<FinderFacade> finderFacadeFunc, CancellationToken cancellationToken)
    {
        this.database = database;
        FileExistanceFilter?.Initialize(finderFacadeFunc());

        if (TagFilter is not null)
        {
            await TagFilter.InitializeAsync(database, cancellationToken).ConfigureAwait(false);
        }

        if (UserFilter is not null)
        {
            await UserFilter.InitializeAsync(database, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask<bool> SlowFilter(Artwork artwork, CancellationToken token)
    {
        if (UserFilter is not null)
        {
            var user = await database.GetUserAsync(artwork.UserId, token).ConfigureAwait(false);
            if (user is null || !UserFilter.Filter(user))
            {
                return false;
            }
        }

        if (FileExistanceFilter is not null && !FileExistanceFilter.Filter(artwork))
        {
            return false;
        }

        return true;
    }
}
