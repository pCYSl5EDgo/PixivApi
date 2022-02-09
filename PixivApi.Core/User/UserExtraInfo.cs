namespace PixivApi;

[MessagePackObject]
public sealed class UserExtraInfo
{
    [Key(0)] public HideReason HideReason;
    [Key(1)] public string? Memo;
    [Key(2)] public string[]? Tags;
}
