namespace PixivApi.Core.Local;

[MessagePackObject]
public sealed class User
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
  [Key(0x0d)] public bool IsOfficiallyRemoved;

  [MessagePackObject]
  public sealed class DetailProfile
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
  }

  [MessagePackObject]
  public sealed class DetailProfilePublicity
  {
    [Key(0x0), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Gender;
    [Key(0x1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Region;
    [Key(0x2), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? BirthDay;
    [Key(0x3), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? BirthYear;
    [Key(0x4), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? Job;
    [Key(0x5)] public bool Pawoo;
  }

  [MessagePackObject]
  public sealed class DetailWorkspace
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
  }
}
