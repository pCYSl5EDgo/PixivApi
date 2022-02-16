namespace PixivApi.Core.Network;

public sealed class Artwork
{
    [JsonPropertyName("id")] public ulong Id;
    [JsonPropertyName("type")] public ArtworkType Type;
    [JsonPropertyName("image_urls")] public ImageUrls ImageUrls;
    [JsonPropertyName("title")] public string Title = string.Empty;
    [JsonPropertyName("caption")] public string Caption = string.Empty;
    [JsonPropertyName("user")] public User User;
    [JsonPropertyName("tags")] public Tag[] Tags = Array.Empty<Tag>();
    [JsonPropertyName("tools")] public string[] Tools = Array.Empty<string>();
    [JsonPropertyName("create_date")] public DateTime CreateDate;
    [JsonPropertyName("page_count")] public uint PageCount;
    [JsonPropertyName("width")] public uint Width;
    [JsonPropertyName("height")] public uint Height;
    [JsonPropertyName("sanity_level")] public uint SanityLevel;
    [JsonPropertyName("x_restrict")] public uint XRestrict;
    [JsonPropertyName("meta_single_page")] public InnerMetaSinglePage MetaSinglePage;
    [JsonPropertyName("meta_pages")] public InnerMetaPage[] MetaPages = Array.Empty<InnerMetaPage>();
    [JsonPropertyName("total_view")] public ulong TotalView;
    [JsonPropertyName("total_bookmarks")] public ulong TotalBookmarks;
    [JsonPropertyName("is_bookmarked")] public bool IsBookmarked;
    [JsonPropertyName("visible")] public bool Visible;
    [JsonPropertyName("is_muted")] public bool IsMuted;
    [JsonPropertyName("total_comments")] public uint TotalComments;

    public override string ToString() => $"{Id} {Title}";

    public override int GetHashCode() => Id.GetHashCode();

    public struct InnerMetaSinglePage
    {
        [JsonPropertyName("original_image_url")]
        public string? OriginalImageUrl;
    }

    public struct InnerMetaPage
    {
        [JsonPropertyName("image_urls")]
        public ImageUrls ImageUrls;
    }
}

public struct Tag : ITag
{
    [JsonPropertyName("name")]
    public string Name;
    [JsonPropertyName("translated_name")]
    public string? TranslatedName;

    [JsonIgnore] string ITag.Tag => Name;
}

public struct ImageUrls
{
    [JsonPropertyName("square_medium")] public string? SquareMedium;
    [JsonPropertyName("medium")] public string? Medium;
    [JsonPropertyName("large")] public string? Large;
    [JsonPropertyName("original")] public string? Original;
}

public struct User
{
    [JsonPropertyName("id")] public ulong Id;
    [JsonPropertyName("name")] public string Name;
    [JsonPropertyName("account")] public string Account;
    [JsonPropertyName("is_followed")] public bool IsFollowed;
    [JsonPropertyName("profile_image_urls")] public ImageUrls ProfileImageUrls;
    [JsonPropertyName("comment")] public string? Comment;
}

public sealed class UserPreview
{
    [JsonPropertyName("user")] public User User;
    [JsonPropertyName("illusts")] public Artwork[]? Illusts;
    [JsonPropertyName("is_muted")] public bool IsMuted;
}

public struct UgoiraMetadataResponseData
{
    [JsonPropertyName("ugoira_metadata")]
    public InnerData Value;

    public struct InnerData
    {
        [JsonPropertyName("zip_urls")]
        public ZipUrls ZipUrls;

        [JsonPropertyName("frames")]
        public Frame[] Frames;
    }

    public struct ZipUrls
    {
        [JsonPropertyName("medium")]
        public string Medium;
    }

    public struct Frame
    {
        [JsonPropertyName("file")]
        public string File;

        [JsonPropertyName("delay")]
        public uint Delay;
    }
}

public struct UserDetailInfo
{
    [JsonPropertyName("user")] public User User;
    [JsonPropertyName("profile")] public UserDetailProfile Profile;
    [JsonPropertyName("profile_publicity")] public UserDetailProfilePublicity ProfilePublicity;
    [JsonPropertyName("workspace")] public UserDetailWorkspace Workspace;
}

public struct UserDetailProfile
{
    [JsonPropertyName("webpage")] public string? Webpage;
    [JsonPropertyName("gender")] public string? Gender;
    [JsonPropertyName("birth")] public string? Birth;
    [JsonPropertyName("birth_day")] public string? BirthDay;
    [JsonPropertyName("birth_year")] public uint BirthYear;
    [JsonPropertyName("region")] public string? Region;
    [JsonPropertyName("address_id")] public long AddressId;
    [JsonPropertyName("country_code")] public string? CountryCode;
    [JsonPropertyName("job")] public string? Job;
    [JsonPropertyName("job_id")] public long JobId;
    [JsonPropertyName("total_follow_users")] public ulong TotalFollowUsers;
    [JsonPropertyName("total_mypixiv_users")] public ulong TotalMypixivUsers;
    [JsonPropertyName("total_illusts")] public ulong TotalIllusts;
    [JsonPropertyName("total_manga")] public ulong TotalManga;
    [JsonPropertyName("total_novels")] public ulong TotalNovels;
    [JsonPropertyName("total_illust_bookmarks_public")] public ulong TotalIllustBookmarksPublic;
    [JsonPropertyName("total_illust_series")] public ulong TotalIllustSeries;
    [JsonPropertyName("total_novel_series")] public ulong TotalNovelSeries;
    [JsonPropertyName("background_image_url")] public string? BackgroundImageUrl;
    [JsonPropertyName("twitter_account")] public string? TwitterAccount;
    [JsonPropertyName("twitter_url")] public string? TwitterUrl;
    [JsonPropertyName("pawoo_url")] public string? PawooUrl;
    [JsonPropertyName("is_premium")] public bool IsPremium;
    [JsonPropertyName("is_using_custom_profile_image")] public bool IsUsingCustomProfileImage;
}

public struct UserDetailProfilePublicity
{
    [JsonPropertyName("gender")] public string? Gender;
    [JsonPropertyName("region")] public string? Region;
    [JsonPropertyName("birth_day")] public string? BirthDay;
    [JsonPropertyName("birth_year")] public string? BirthYear;
    [JsonPropertyName("job")] public string? Job;
    [JsonPropertyName("pawoo")] public bool Pawoo;
}

public struct UserDetailWorkspace
{
    [JsonPropertyName("pc")] public string? Pc;
    [JsonPropertyName("monitor")] public string? Monitor;
    [JsonPropertyName("tool")] public string? Tool;
    [JsonPropertyName("scanner")] public string? Scanner;
    [JsonPropertyName("tablet")] public string? Tablet;
    [JsonPropertyName("mouse")] public string? Mouse;
    [JsonPropertyName("printer")] public string? Printer;
    [JsonPropertyName("desktop")] public string? Desktop;
    [JsonPropertyName("music")] public string? Music;
    [JsonPropertyName("desk")] public string? Desk;
    [JsonPropertyName("chair")] public string? Chair;
    [JsonPropertyName("comment")] public string? Comment;
    [JsonPropertyName("workspace_image_url")] public string? WorkspaceImageUrl;
}

public struct IllustsResponseData : INext, IArrayContainer<Artwork>
{
    [JsonPropertyName("illusts")] public Artwork[] Illusts;
    [JsonPropertyName("next_url")] public string? NextUrl { get; set; }

    public Artwork[] GetContainer() => Illusts;
}

public struct UserPreviewsResponseData : INext, IArrayContainer<UserPreview>
{
    [JsonPropertyName("user_previews")] public UserPreview[] UserPreviews;
    [JsonPropertyName("next_url")] public string? NextUrl { get; set; }

    public UserPreview[] GetContainer() => UserPreviews;
}

public struct UserDetailResponseData
{
    [JsonPropertyName("user")] public User User;
    [JsonPropertyName("profile")] public UserDetailProfile? Profile;
    [JsonPropertyName("profile_publicity")] public UserDetailProfilePublicity? ProfilePublicity;
    [JsonPropertyName("workspace")] public UserDetailWorkspace? Workspace;
}

public struct IllustDateilResponseData
{
    [JsonPropertyName("illust")] public Artwork Illust;
}
