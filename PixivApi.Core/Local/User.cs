namespace PixivApi.Core.Local;

[MessagePackObject]
public sealed class User : IOverwrite<User>
{
    [Key(0x00)] public ulong Id;
    [Key(0x01), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Name;
    [Key(0x02), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Account;
    [Key(0x03)] public bool IsFollowed;
    [Key(0x04)] public bool IsMuted;
    [Key(0x05)] public HideReason ExtraHideReason;
    [Key(0x06), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? ImageUrls;
    [Key(0x07), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Comment;
    [Key(0x08), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public DetailProfile? Profile;
    [Key(0x09), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public DetailProfilePublicity? ProfilePublicity;
    [Key(0x0a), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public DetailWorkspace? Workspace;
    [Key(0x0b), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? ExtraMemo;
    [Key(0x0c), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public uint[]? ExtraTags;

    public static implicit operator User(Network.User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Account = user.Account,
        IsFollowed = user.IsFollowed,
        ImageUrls = user.ProfileImageUrls.Medium,
        Comment = user.Comment,
    };

    public static implicit operator User(Network.UserPreviewResponseContent user)
    {
        User answer = user.User;
        answer.IsMuted = user.IsMuted;
        return answer;
    }

    public void Overwrite(User source)
    {
        if (Id != source.Id)
        {
            return;
        }

        OverwriteExtensions.Overwrite(ref Name, source.Name);
        OverwriteExtensions.Overwrite(ref Account, source.Account);
        IsFollowed = source.IsFollowed;
        IsMuted = source.IsMuted;
        ExtraHideReason = source.ExtraHideReason;
        ImageUrls ??= source.ImageUrls;
        Comment ??= source.Comment;
        OverwriteExtensions.Overwrite(ref Profile, source.Profile);
        OverwriteExtensions.Overwrite(ref ProfilePublicity, source.ProfilePublicity);
        OverwriteExtensions.Overwrite(ref Workspace, source.Workspace);
        ExtraMemo ??= source.ExtraMemo;
        ExtraTags ??= source.ExtraTags;
    }

    [MessagePackObject]
    public sealed class DetailProfile : IOverwrite<DetailProfile>
    {
        [Key(0x00), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Webpage;
        [Key(0x01), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Gender;
        [Key(0x02), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Birth;
        [Key(0x03), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? BirthDay;
        [Key(0x04)] public uint BirthYear;
        [Key(0x05), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Region;
        [Key(0x06)] public long AddressId;
        [Key(0x07), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? CountryCode;
        [Key(0x08), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Job;
        [Key(0x09)] public long JobId;
        [Key(0x0a)] public ulong TotalFollowUsers;
        [Key(0x0b)] public ulong TotalMypixivUsers;
        [Key(0x0c)] public ulong TotalIllusts;
        [Key(0x0d)] public ulong TotalManga;
        [Key(0x0e)] public ulong TotalNovels;
        [Key(0x0f)] public ulong TotalIllustBookmarksPublic;
        [Key(0x10)] public ulong TotalIllustSeries;
        [Key(0x11)] public ulong TotalNovelSeries;
        [Key(0x12), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? BackgroundImageUrl;
        [Key(0x13), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? TwitterAccount;
        [Key(0x14), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? TwitterUrl;
        [Key(0x15), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? PawooUrl;
        [Key(0x16)] public bool IsPremium;
        [Key(0x17)] public bool IsUsingCustomProfileImage;

        public static implicit operator DetailProfile(Network.UserDetailProfile source) => new()
        {
            Webpage = source.Webpage,
            Gender = source.Gender,
            Birth = source.Birth,
            BirthDay = source.BirthDay,
            BirthYear = source.BirthYear,
            Region = source.Region,
            AddressId = source.AddressId,
            CountryCode = source.CountryCode,
            Job = source.Job,
            JobId = source.JobId,
            TotalFollowUsers = source.TotalFollowUsers,
            TotalMypixivUsers = source.TotalMypixivUsers,
            TotalIllusts = source.TotalIllusts,
            TotalManga = source.TotalManga,
            TotalNovels = source.TotalNovels,
            TotalIllustBookmarksPublic = source.TotalIllustBookmarksPublic,
            TotalIllustSeries = source.TotalIllustSeries,
            TotalNovelSeries = source.TotalNovelSeries,
            BackgroundImageUrl = source.BackgroundImageUrl,
            TwitterAccount = source.TwitterAccount,
            TwitterUrl = source.TwitterUrl,
            PawooUrl = source.PawooUrl,
            IsPremium = source.IsPremium,
            IsUsingCustomProfileImage = source.IsUsingCustomProfileImage,
        };

        public void Overwrite(DetailProfile source)
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
    public sealed class DetailProfilePublicity : IOverwrite<DetailProfilePublicity>
    {
        [Key(0x0), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Gender;
        [Key(0x1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Region;
        [Key(0x2), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? BirthDay;
        [Key(0x3), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? BirthYear;
        [Key(0x4), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Job;
        [Key(0x5)] public bool Pawoo;

        public static implicit operator DetailProfilePublicity(Network.UserDetailProfilePublicity source) => new()
        {
            Gender = source.Gender,
            Region = source.Region,
            BirthDay = source.BirthDay,
            BirthYear = source.BirthYear,
            Job = source.Job,
            Pawoo = source.Pawoo,
        };

        public void Overwrite(DetailProfilePublicity source)
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
    public sealed class DetailWorkspace : IOverwrite<DetailWorkspace>
    {
        [Key(0x00), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Pc;
        [Key(0x01), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Monitor;
        [Key(0x02), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Tool;
        [Key(0x03), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Scanner;
        [Key(0x04), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Tablet;
        [Key(0x05), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Mouse;
        [Key(0x06), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Printer;
        [Key(0x07), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Desktop;
        [Key(0x08), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Music;
        [Key(0x09), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Desk;
        [Key(0x0a), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Chair;
        [Key(0x0b), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Comment;
        [Key(0x0c), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? WorkspaceImageUrl;

        public static implicit operator DetailWorkspace(Network.UserDetailWorkspace source) => new()
        {
            Pc = source.Pc,
            Monitor = source.Monitor,
            Tool = source.Tool,
            Scanner = source.Scanner,
            Tablet = source.Tablet,
            Mouse = source.Mouse,
            Printer = source.Printer,
            Desktop = source.Desktop,
            Music = source.Music,
            Desk = source.Desk,
            Chair = source.Chair,
            Comment = source.Comment,
            WorkspaceImageUrl = source.WorkspaceImageUrl,
        };

        public void Overwrite(DetailWorkspace source)
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
}
