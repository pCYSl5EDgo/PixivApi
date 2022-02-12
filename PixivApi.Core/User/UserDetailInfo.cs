namespace PixivApi;

[MessagePackObject]
public sealed class UserDetailInfo : IEquatable<UserDetailInfo>, IComparable<UserDetailInfo>, IOverwrite<UserDetailInfo>
{
    [Key(0), JsonPropertyName("user")] public User User;
    [Key(1), JsonPropertyName("profile")] public UserDetailProfile? Profile;
    [Key(2), JsonPropertyName("profile_publicity")] public UserDetailProfilePublicity? ProfilePublicity;
    [Key(3), JsonPropertyName("workspace")] public UserDetailWorkspace? Workspace;

    public bool Equals(UserDetailInfo? other) => other is not null && User.Id == other.User.Id;

    public int CompareTo(UserDetailInfo? other) => other is null ? 1 : User.Id.CompareTo(other.User.Id);

    public void Overwrite(UserDetailInfo source)
    {
        if (source.User.Id != User.Id)
        {
            return;
        }

        OverwriteExtensions.Overwrite(ref Profile, source.Profile);
        OverwriteExtensions.Overwrite(ref ProfilePublicity, source.ProfilePublicity);
        OverwriteExtensions.Overwrite(ref Workspace, source.Workspace);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as UserDetailInfo);

    public override int GetHashCode() => HashCode.Combine(User.Id);

    public static bool operator ==(UserDetailInfo left, UserDetailInfo right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(UserDetailInfo left, UserDetailInfo right) => !(left == right);

    public static bool operator <(UserDetailInfo left, UserDetailInfo right) => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(UserDetailInfo left, UserDetailInfo right) => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(UserDetailInfo left, UserDetailInfo right) => left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(UserDetailInfo left, UserDetailInfo right) => left is null ? right is null : left.CompareTo(right) >= 0;
}

[MessagePackObject]
public sealed class UserDetailProfile : IOverwrite<UserDetailProfile>
{
    [Key(0x00), JsonPropertyName("webpage")] public string? Webpage;
    [Key(0x01), JsonPropertyName("gender")] public string? Gender;
    [Key(0x02), JsonPropertyName("birth")] public string? Birth;
    [Key(0x03), JsonPropertyName("birth_day")] public string? BirthDay;
    [Key(0x04), JsonPropertyName("birth_year")] public uint BirthYear;
    [Key(0x05), JsonPropertyName("region")] public string? Region;
    [Key(0x06), JsonPropertyName("address_id")] public long AddressId;
    [Key(0x07), JsonPropertyName("country_code")] public string? CountryCode;
    [Key(0x08), JsonPropertyName("job")] public string? Job;
    [Key(0x09), JsonPropertyName("job_id")] public long JobId;
    [Key(0x0a), JsonPropertyName("total_follow_users")] public ulong TotalFollowUsers;
    [Key(0x0b), JsonPropertyName("total_mypixiv_users")] public ulong TotalMypixivUsers;
    [Key(0x0c), JsonPropertyName("total_illusts")] public ulong TotalIllusts;
    [Key(0x0d), JsonPropertyName("total_manga")] public ulong TotalManga;
    [Key(0x0e), JsonPropertyName("total_novels")] public ulong TotalNovels;
    [Key(0x0f), JsonPropertyName("total_illust_bookmarks_public")] public ulong TotalIllustBookmarksPublic;
    [Key(0x10), JsonPropertyName("total_illust_series")] public ulong TotalIllustSeries;
    [Key(0x11), JsonPropertyName("total_novel_series")] public ulong TotalNovelSeries;
    [Key(0x12), JsonPropertyName("background_image_url")] public string? BackgroundImageUrl;
    [Key(0x13), JsonPropertyName("twitter_account")] public string? TwitterAccount;
    [Key(0x14), JsonPropertyName("twitter_url")] public string? TwitterUrl;
    [Key(0x15), JsonPropertyName("pawoo_url")] public string? PawooUrl;
    [Key(0x16), JsonPropertyName("is_premium")] public bool IsPremium;
    [Key(0x17), JsonPropertyName("is_using_custom_profile_image")] public bool IsUsingCustomProfileImage;

    public void Overwrite(UserDetailProfile source)
    {
        OverwriteExtensions.Overwrite(ref Webpage, source.Webpage);
        OverwriteExtensions.Overwrite(ref Gender, source.Gender);
        OverwriteExtensions.Overwrite(ref Birth, source.Birth);
        OverwriteExtensions.Overwrite(ref BirthDay, source.BirthDay);
        BirthYear = source.BirthYear;
        OverwriteExtensions.Overwrite(ref Region, source.Region);
        AddressId = source.AddressId;
        OverwriteExtensions.Overwrite(ref CountryCode, source.CountryCode);
        OverwriteExtensions.Overwrite(ref Job, source.Job);
        JobId = source.JobId;
        TotalFollowUsers = source.TotalFollowUsers;
        TotalMypixivUsers = source.TotalMypixivUsers;
        TotalIllusts = source.TotalIllusts;
        TotalManga = source.TotalManga;
        TotalNovels = source.TotalNovels;
        TotalIllustBookmarksPublic = source.TotalIllustBookmarksPublic;
        TotalIllustSeries = source.TotalIllustSeries;
        TotalNovelSeries = source.TotalNovelSeries;
        OverwriteExtensions.Overwrite(ref BackgroundImageUrl, source.BackgroundImageUrl);
        OverwriteExtensions.Overwrite(ref TwitterAccount, source.TwitterAccount);
        OverwriteExtensions.Overwrite(ref TwitterUrl, source.TwitterUrl);
        OverwriteExtensions.Overwrite(ref PawooUrl, source.PawooUrl);
        IsPremium = source.IsPremium;
        IsUsingCustomProfileImage = source.IsUsingCustomProfileImage;
    }
}

[MessagePackObject]
public sealed class UserDetailProfilePublicity : IOverwrite<UserDetailProfilePublicity>
{
    [Key(0x0), JsonPropertyName("gender")] public string? Gender;
    [Key(0x1), JsonPropertyName("region")] public string? Region;
    [Key(0x2), JsonPropertyName("birth_day")] public string? BirthDay;
    [Key(0x3), JsonPropertyName("birth_year")] public string? BirthYear;
    [Key(0x4), JsonPropertyName("job")] public string? Job;
    [Key(0x5), JsonPropertyName("pawoo")] public bool Pawoo;

    public void Overwrite(UserDetailProfilePublicity source)
    {
        OverwriteExtensions.Overwrite(ref Gender, source.Gender);
        OverwriteExtensions.Overwrite(ref Region, source.Region);
        OverwriteExtensions.Overwrite(ref BirthDay, source.BirthDay);
        OverwriteExtensions.Overwrite(ref BirthYear, source.BirthYear);
        OverwriteExtensions.Overwrite(ref Job, source.Job);
        Pawoo = source.Pawoo;
    }
}

[MessagePackObject]
public sealed class UserDetailWorkspace : IOverwrite<UserDetailWorkspace>
{
    [Key(0x00), JsonPropertyName("pc")] public string? Pc;
    [Key(0x01), JsonPropertyName("monitor")] public string? Monitor;
    [Key(0x02), JsonPropertyName("tool")] public string? Tool;
    [Key(0x03), JsonPropertyName("scanner")] public string? Scanner;
    [Key(0x04), JsonPropertyName("tablet")] public string? Tablet;
    [Key(0x05), JsonPropertyName("mouse")] public string? Mouse;
    [Key(0x06), JsonPropertyName("printer")] public string? Printer;
    [Key(0x07), JsonPropertyName("desktop")] public string? Desktop;
    [Key(0x08), JsonPropertyName("music")] public string? Music;
    [Key(0x09), JsonPropertyName("desk")] public string? Desk;
    [Key(0x0a), JsonPropertyName("chair")] public string? Chair;
    [Key(0x0b), JsonPropertyName("comment")] public string? Comment;
    [Key(0x0c), JsonPropertyName("workspace_image_url")] public string? WorkspaceImageUrl;

    public void Overwrite(UserDetailWorkspace source)
    {
        OverwriteExtensions.Overwrite(ref Pc, source.Pc);
        OverwriteExtensions.Overwrite(ref Monitor, source.Monitor);
        OverwriteExtensions.Overwrite(ref Tool, source.Tool);
        OverwriteExtensions.Overwrite(ref Scanner, source.Scanner);
        OverwriteExtensions.Overwrite(ref Tablet, source.Tablet);
        OverwriteExtensions.Overwrite(ref Mouse, source.Mouse);
        OverwriteExtensions.Overwrite(ref Printer, source.Printer);
        OverwriteExtensions.Overwrite(ref Desktop, source.Desktop);
        OverwriteExtensions.Overwrite(ref Music, source.Music);
        OverwriteExtensions.Overwrite(ref Desk, source.Desk);
        OverwriteExtensions.Overwrite(ref Chair, source.Chair);
        OverwriteExtensions.Overwrite(ref Comment, source.Comment);
        OverwriteExtensions.Overwrite(ref WorkspaceImageUrl, source.WorkspaceImageUrl);
    }
}
