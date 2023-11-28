namespace PixivApi.Core.Network;

public struct ArtworkResponseContent
{
  [JsonPropertyName("id")] public ulong Id;
  [JsonPropertyName("type")] public ArtworkType Type;
  [JsonPropertyName("image_urls")] public ImageUrlsResponse ImageUrls;
  [JsonPropertyName("title")] public string? Title;
  [JsonPropertyName("caption")] public string? Caption;
  [JsonPropertyName("user")] public UserResponse User;
  [JsonPropertyName("tags")] public Tag[]? Tags;
  [JsonPropertyName("tools")] public string[]? Tools;
  [JsonPropertyName("create_date")] public DateTime CreateDate;
  [JsonPropertyName("page_count")] public uint PageCount;
  [JsonPropertyName("width")] public uint Width;
  [JsonPropertyName("height")] public uint Height;
  [JsonPropertyName("sanity_level")] public uint SanityLevel;
  [JsonPropertyName("x_restrict")] public uint XRestrict;
  [JsonPropertyName("restrict")] public uint Restrict;
  [JsonPropertyName("meta_single_page")] public InnerMetaSinglePage MetaSinglePage;
  [JsonPropertyName("meta_pages")] public InnerMetaPage[]? MetaPages;
  [JsonPropertyName("total_view")] public ulong TotalView;
  [JsonPropertyName("total_bookmarks")] public ulong TotalBookmarks;
  [JsonPropertyName("is_bookmarked")] public bool IsBookmarked;
  [JsonPropertyName("visible")] public bool Visible;
  [JsonPropertyName("is_muted")] public bool IsMuted;
  [JsonPropertyName("total_comments")] public uint TotalComments;

  public readonly bool IsUnknown() => ImageUrls.SquareMedium?.EndsWith("limit_unknown_360.png") ?? false;

#if DEBUG
  public readonly override string ToString() => $"{Id} {Title}";

  public readonly override int GetHashCode() => Id.GetHashCode();
#endif

  public struct InnerMetaSinglePage
  {
    [JsonPropertyName("original_image_url")]
    public string? OriginalImageUrl;
  }

  public struct InnerMetaPage
  {
    [JsonPropertyName("image_urls")]
    public ImageUrlsResponse ImageUrls;
  }
}

public struct Tag
{
  [JsonPropertyName("name")]
  public string Name;
  [JsonPropertyName("translated_name")]
  public string? TranslatedName;
}

public struct ImageUrlsResponse
{
  [JsonPropertyName("square_medium")] public string? SquareMedium;
  [JsonPropertyName("medium")] public string? Medium;
  [JsonPropertyName("large")] public string? Large;
  [JsonPropertyName("original")] public string? Original;
}

public struct UserResponse
{
  [JsonPropertyName("id")] public ulong Id;
  [JsonPropertyName("name")] public string Name;
  [JsonPropertyName("account")] public string Account;
  [JsonPropertyName("is_followed")] public bool IsFollowed;
  [JsonPropertyName("profile_image_urls")] public ImageUrlsResponse ProfileImageUrls;
  [JsonPropertyName("comment")] public string? Comment;
}

public struct UserPreviewResponseContent
{
  [JsonPropertyName("user")] public UserResponse User;
  [JsonPropertyName("illusts")] public ArtworkResponseContent[]? Illusts;
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
  [JsonPropertyName("user")] public UserResponse User;
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

public struct IllustsResponseData
{
  [JsonPropertyName("illusts")] public ArtworkResponseContent[] Illusts;
  [JsonPropertyName("next_url")] public string? NextUrl;
}

public struct UserPreviewsResponseData
{
  [JsonPropertyName("user_previews")] public UserPreviewResponseContent[] UserPreviews;
  [JsonPropertyName("next_url")] public string? NextUrl;
}

public struct UserDetailResponseData
{
  [JsonPropertyName("user")] public UserResponse User;
  [JsonPropertyName("profile")] public UserDetailProfile? Profile;
  [JsonPropertyName("profile_publicity")] public UserDetailProfilePublicity? ProfilePublicity;
  [JsonPropertyName("workspace")] public UserDetailWorkspace? Workspace;
}

public struct IllustDateilResponseData
{
  [JsonPropertyName("illust")] public ArtworkResponseContent Illust;
}
