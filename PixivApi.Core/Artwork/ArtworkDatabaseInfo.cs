namespace PixivApi;

[MessagePackObject]
public sealed class ArtworkDatabaseInfo : IComparable<ArtworkDatabaseInfo>, IEquatable<ArtworkDatabaseInfo>, IOverwrite<ArtworkDatabaseInfo>
{
    [Key(0x00), JsonPropertyName("id")] public ulong Id;
    [Key(0x01), JsonPropertyName("type")] public ArtworkType Type;
    [Key(0x02), JsonPropertyName("image_urls")] public ImageUrls ImageUrls;
    [Key(0x03), JsonPropertyName("title")] public string Title = string.Empty;
    [Key(0x04), JsonPropertyName("caption")] public string Caption = string.Empty;
    [Key(0x05), JsonPropertyName("user")] public User User;
    [Key(0x06), JsonPropertyName("tags")] public Tag[] Tags = Array.Empty<Tag>();
    [Key(0x07), JsonPropertyName("tools")] public string[] Tools = Array.Empty<string>();
    [Key(0x08), JsonPropertyName("create_date")] public DateTime CreateDate;
    [Key(0x09), JsonPropertyName("page_count")] public uint PageCount;
    [Key(0x0a), JsonPropertyName("width")] public uint Width;
    [Key(0x0b), JsonPropertyName("height")] public uint Height;
    [Key(0x0c), JsonPropertyName("sanity_level")] public uint SanityLevel;
    [Key(0x0d), JsonPropertyName("x_restrict")] public uint XRestrict;
    [Key(0x0e), JsonPropertyName("meta_single_page")] public MetaSinglePage MetaSinglePage;
    [Key(0x0f), JsonPropertyName("meta_pages")] public MetaPage[] MetaPages = Array.Empty<MetaPage>();
    [Key(0x10), JsonPropertyName("total_view")] public ulong TotalView;
    [Key(0x11), JsonPropertyName("total_bookmarks")] public ulong TotalBookmarks;
    [Key(0x12), JsonPropertyName("is_bookmarked")] public bool IsBookmarked;
    [Key(0x13), JsonPropertyName("visible")] public bool Visible;
    [Key(0x14), JsonPropertyName("is_muted")] public bool IsMuted;

    [Key(0x15), JsonIgnore] public FileExtraInfo? ExtraInfo;

    public int CompareTo(ArtworkDatabaseInfo? other) => other is null ? 1 : Id.CompareTo(other.Id);

    public bool Equals(ArtworkDatabaseInfo? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is ArtworkDatabaseInfo artwork && Id == artwork.Id);

    public override int GetHashCode() => HashCode.Combine(Id);

    public override string ToString() => Id.ToString();

    public string GetOriginalUrl(uint pageIndex)
    {
        if (pageIndex == 0)
        {
            return MetaSinglePage.OriginalImageUrl ?? MetaPages[0].ImageUrls.Original ?? throw new NullReferenceException();
        }
        
        return MetaPages[pageIndex].ImageUrls.Original ?? throw new NullReferenceException();
    }

    public void Overwrite(ArtworkDatabaseInfo source)
    {
        if (Id != source.Id || source.User.Id == 0 || TotalView > source.TotalView)
        {
            return;
        }

        Type = source.Type;
        ImageUrls = source.ImageUrls;
        OverwriteExtensions.Overwrite(ref Title, source.Title);
        OverwriteExtensions.Overwrite(ref Caption, source.Caption);
        User = source.User;
        OverwriteExtensions.Overwrite(ref Tags, source.Tags);
        OverwriteExtensions.Overwrite(ref Tools, source.Tools);
        CreateDate = source.CreateDate;
        PageCount = source.PageCount;
        Width = source.Width;
        Height = source.Height;
        SanityLevel = source.SanityLevel;
        XRestrict = source.XRestrict;
        MetaSinglePage = source.MetaSinglePage;
        OverwriteExtensions.Overwrite(ref MetaPages, source.MetaPages);
        TotalView = source.TotalView;
        TotalBookmarks = source.TotalBookmarks;
        IsBookmarked = source.IsBookmarked;
        Visible = source.Visible;
        IsMuted = source.IsMuted;
        OverwriteExtensions.Overwrite(ref ExtraInfo, source.ExtraInfo);
    }

    public static bool operator ==(ArtworkDatabaseInfo left, ArtworkDatabaseInfo right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ArtworkDatabaseInfo left, ArtworkDatabaseInfo right) => !(left == right);

    public static bool operator <(ArtworkDatabaseInfo left, ArtworkDatabaseInfo right) => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(ArtworkDatabaseInfo left, ArtworkDatabaseInfo right) => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(ArtworkDatabaseInfo left, ArtworkDatabaseInfo right) => left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(ArtworkDatabaseInfo left, ArtworkDatabaseInfo right) => left is null ? right is null : left.CompareTo(right) >= 0;
}
