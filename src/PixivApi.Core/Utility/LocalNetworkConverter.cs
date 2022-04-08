using PixivApi.Core.Local;
using PixivApi.Core.Network;

namespace PixivApi.Core;

public static class LocalNetworkConverter
{
    public static async ValueTask<Artwork> ConvertAsync(ArtworkResponseContent source, ITagDatabase tagDatabase, IToolDatabase toolDatabase, IUserDatabase userDatabase, CancellationToken token)
    {
        Artwork answer = new()
        {
            Id = source.Id,
            UserId = source.User.Id,
            TotalView = source.TotalView,
            TotalBookmarks = source.TotalBookmarks,
            PageCount = source.PageCount,
            Width = source.Width,
            Height = source.Height,
            Type = source.Type,
            Extension = source.ConvertToFileExtensionKind(),
            CreateDate = source.CreateDate,
            FileDate = ParseFileDate(source),
            Title = source.Title ?? "",
            Caption = source.Caption ?? "",
            IsXRestricted = source.XRestrict != 0,
            IsBookmarked = source.IsBookmarked,
            IsMuted = source.IsMuted,
            IsVisible = source.Visible,
            Tags = await tagDatabase.CalculateTagsAsync(source.Tags, token).ConfigureAwait(false),
            Tools = await toolDatabase.CalculateToolsAsync(source.Tools, token).ConfigureAwait(false),
        };
        
        return answer;
    }

    public static async ValueTask OverwriteAsync(Artwork destination, ArtworkResponseContent source, ITagDatabase tagDatabase, IToolDatabase toolDatabase, IUserDatabase userDatabase, CancellationToken token)
    {
        if (destination.Id != source.Id)
        {
            return;
        }

        if (destination.UserId != source.User.Id || source.User.Id == 0)
        {
            destination.IsOfficiallyRemoved = true;
        }

        if (destination.TotalView <= source.TotalView)
        {
            destination.TotalView = source.TotalView;
            destination.TotalBookmarks = source.TotalBookmarks;
        }

        destination.PageCount = source.PageCount;
        destination.Width = source.Width;
        destination.Height = source.Height;
        destination.Type = source.Type;
        destination.Extension = source.ConvertToFileExtensionKind();
        destination.IsXRestricted = source.XRestrict != 0;
        destination.IsBookmarked = source.IsBookmarked;
        destination.IsVisible = source.Visible;
        destination.IsMuted = source.IsMuted;
        destination.CreateDate = source.CreateDate;
        destination.FileDate = ParseFileDate(source);
        destination.Tags = await tagDatabase.CalculateTagsAsync(source.Tags, token).ConfigureAwait(false);
        destination.Tools = await toolDatabase.CalculateToolsAsync(source.Tools, token).ConfigureAwait(false);
        destination.Title = source.Title ?? string.Empty;
        destination.Caption = source.Caption ?? string.Empty;
        if (destination.ExtraHideReason == HideReason.NotHidden && source.IsUnknown())
        {
            destination.ExtraHideReason = HideReason.TemporaryHidden;
        }
    }

    public static User Convert(this UserPreviewResponseContent user)
    {
        var answer = user.User.Convert();
        answer.IsMuted = user.IsMuted;
        return answer;
    }

    public static void Overwrite(this User destination, in UserPreviewResponseContent source)
    {
        Overwrite(destination, source.User);
        destination.IsMuted = source.IsMuted;
    }

    public static void Overwrite(this User destination, in UserResponse source)
    {
        if (destination.Id != source.Id)
        {
            return;
        }

        OverwriteExtensions.Overwrite(ref destination.Name, source.Name);
        OverwriteExtensions.Overwrite(ref destination.Account, source.Account);
        destination.IsFollowed = source.IsFollowed;
        OverwriteExtensions.Overwrite(ref destination.ImageUrls, source.ProfileImageUrls.Medium);
        OverwriteExtensions.Overwrite(ref destination.Comment, source.Comment);
    }

    public static void Overwrite(this User destination, in UserDetailResponseData source)
    {
        if (destination.Id != source.User.Id)
        {
            return;
        }

        Overwrite(destination, source.User);
        if (source.Workspace is { } workspace)
        {
            if (destination.Workspace is null)
            {
                destination.Workspace = Convert(workspace);
            }
            else
            {
                Overwrite(destination.Workspace, workspace);
            }
        }

        if (source.ProfilePublicity is { } profilePublicity)
        {
            if (destination.ProfilePublicity is null)
            {
                destination.ProfilePublicity = Convert(profilePublicity);
            }
            else
            {
                Overwrite(destination.ProfilePublicity, profilePublicity);
            }
        }

        if (source.Profile is { } profile)
        {
            if (destination.Profile is null)
            {
                destination.Profile = Convert(profile);
            }
            else
            {
                Overwrite(destination.Profile, profile);
            }
        }
    }

    public static User Convert(this UserResponse user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Account = user.Account,
        IsFollowed = user.IsFollowed,
        ImageUrls = user.ProfileImageUrls.Medium,
        Comment = user.Comment,
    };

    public static User.DetailProfile Convert(this UserDetailProfile source) => new()
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

    public static void Overwrite(User.DetailProfile destination, UserDetailProfile source)
    {
        OverwriteExtensions.Overwrite(ref destination.Webpage, source.Webpage);
        OverwriteExtensions.Overwrite(ref destination.Gender, source.Gender);
        OverwriteExtensions.Overwrite(ref destination.Birth, source.Birth);
        OverwriteExtensions.Overwrite(ref destination.BirthDay, source.BirthDay);
        destination.BirthYear = source.BirthYear;
        OverwriteExtensions.Overwrite(ref destination.Region, source.Region);
        destination.AddressId = source.AddressId;
        OverwriteExtensions.Overwrite(ref destination.CountryCode, source.CountryCode);
        OverwriteExtensions.Overwrite(ref destination.Job, source.Job);
        destination.JobId = source.JobId;
        destination.TotalFollowUsers = source.TotalFollowUsers;
        destination.TotalMypixivUsers = source.TotalMypixivUsers;
        destination.TotalIllusts = source.TotalIllusts;
        destination.TotalManga = source.TotalManga;
        destination.TotalNovels = source.TotalNovels;
        destination.TotalIllustBookmarksPublic = source.TotalIllustBookmarksPublic;
        destination.TotalIllustSeries = source.TotalIllustSeries;
        destination.TotalNovelSeries = source.TotalNovelSeries;
        OverwriteExtensions.Overwrite(ref destination.BackgroundImageUrl, source.BackgroundImageUrl);
        OverwriteExtensions.Overwrite(ref destination.TwitterAccount, source.TwitterAccount);
        OverwriteExtensions.Overwrite(ref destination.TwitterUrl, source.TwitterUrl);
        OverwriteExtensions.Overwrite(ref destination.PawooUrl, source.PawooUrl);
        destination.IsPremium = source.IsPremium;
        destination.IsUsingCustomProfileImage = source.IsUsingCustomProfileImage;
    }

    public static User.DetailProfilePublicity Convert(UserDetailProfilePublicity source) => new()
    {
        Gender = source.Gender,
        Region = source.Region,
        BirthDay = source.BirthDay,
        BirthYear = source.BirthYear,
        Job = source.Job,
        Pawoo = source.Pawoo,
    };

    public static void Overwrite(User.DetailProfilePublicity destination, UserDetailProfilePublicity source)
    {
        OverwriteExtensions.Overwrite(ref destination.Gender, source.Gender);
        OverwriteExtensions.Overwrite(ref destination.Region, source.Region);
        OverwriteExtensions.Overwrite(ref destination.BirthDay, source.BirthDay);
        OverwriteExtensions.Overwrite(ref destination.BirthYear, source.BirthYear);
        OverwriteExtensions.Overwrite(ref destination.Job, source.Job);
        destination.Pawoo = source.Pawoo;
    }

    public static User.DetailWorkspace Convert(UserDetailWorkspace source) => new()
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

    public static void Overwrite(User.DetailWorkspace destination, UserDetailWorkspace source)
    {
        OverwriteExtensions.Overwrite(ref destination.Pc, source.Pc);
        OverwriteExtensions.Overwrite(ref destination.Monitor, source.Monitor);
        OverwriteExtensions.Overwrite(ref destination.Tool, source.Tool);
        OverwriteExtensions.Overwrite(ref destination.Scanner, source.Scanner);
        OverwriteExtensions.Overwrite(ref destination.Tablet, source.Tablet);
        OverwriteExtensions.Overwrite(ref destination.Mouse, source.Mouse);
        OverwriteExtensions.Overwrite(ref destination.Printer, source.Printer);
        OverwriteExtensions.Overwrite(ref destination.Desktop, source.Desktop);
        OverwriteExtensions.Overwrite(ref destination.Music, source.Music);
        OverwriteExtensions.Overwrite(ref destination.Desk, source.Desk);
        OverwriteExtensions.Overwrite(ref destination.Chair, source.Chair);
        OverwriteExtensions.Overwrite(ref destination.Comment, source.Comment);
        OverwriteExtensions.Overwrite(ref destination.WorkspaceImageUrl, source.WorkspaceImageUrl);
    }

    public static FileExtensionKind ConvertToFileExtensionKind(this in ArtworkResponseContent source)
    {
        var ext = source.MetaSinglePage.OriginalImageUrl is string url ? url.AsSpan(url.LastIndexOf('.')) : source.MetaPages?[0].ImageUrls.Original is string original ? original.AsSpan(original.LastIndexOf('.')) : throw new NullReferenceException();
        if (ext.SequenceEqual(".jpg") || ext.SequenceEqual(".jpeg"))
        {
            return FileExtensionKind.Jpg;
        }
        else if (ext.SequenceEqual(".png"))
        {
            return FileExtensionKind.Png;
        }
        else if (ext.SequenceEqual(".zip"))
        {
            return FileExtensionKind.Zip;
        }
        else if (ext.SequenceEqual(".gif"))
        {
            return FileExtensionKind.Gif;
        }
        else
        {
            return FileExtensionKind.None;
        }
    }

    public static DateTime ParseFileDate(in ArtworkResponseContent source)
    {
        var page = (source.MetaSinglePage.OriginalImageUrl ?? source.MetaPages?[0].ImageUrls.Original).AsSpan();
        if (!TryParseDate(page, out var answer))
        {
            answer = source.CreateDate.ToLocalTime();
        }

        return answer;
    }

    private static bool TryParseDate(ReadOnlySpan<char> page, out DateTime dateTime)
    {
        Unsafe.SkipInit(out dateTime);
        page = page[..page.LastIndexOf('/')];
        var secondIndex = page.LastIndexOf('/');
        if (secondIndex == -1 || !byte.TryParse(page[(secondIndex + 1)..], out var second))
        {
            return false;
        }

        page = page[..secondIndex];
        var minuteIndex = page.LastIndexOf('/');
        if (minuteIndex == -1 || !byte.TryParse(page[(minuteIndex + 1)..], out var minute))
        {
            return false;
        }

        page = page[..minuteIndex];
        var hourIndex = page.LastIndexOf('/');
        if (hourIndex == -1 || !byte.TryParse(page[(hourIndex + 1)..], out var hour))
        {
            return false;
        }

        page = page[..hourIndex];
        var dayIndex = page.LastIndexOf('/');
        if (dayIndex == -1 || !byte.TryParse(page[(dayIndex + 1)..], out var day))
        {
            return false;
        }
        page = page[..dayIndex];
        var monthIndex = page.LastIndexOf('/');
        if (monthIndex == -1 || !byte.TryParse(page[(monthIndex + 1)..], out var month))
        {
            return false;
        }
        page = page[..monthIndex];
        var yearIndex = page.LastIndexOf('/');
        if (yearIndex == -1 || !uint.TryParse(page[(yearIndex + 1)..], out var year))
        {
            return false;
        }

        dateTime = new((int)year, month, day, hour, minute, second);
        return true;
    }
}
