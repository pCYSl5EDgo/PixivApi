namespace PixivApi;

[MessagePackObject]
public sealed class FileExtraInfo : IOverwrite<FileExtraInfo>
{
    [Key(0)] public string? Memo;
    [Key(1)] public string[]? Tags;
    [Key(2)] public HideReason HideReason;
    [Key(3)] public bool HideLast;
    [Key(4)] public Dictionary<uint, FilePageExtraInfo>? PageExtraInfoDictionary;
    [Key(5)] public string[]? FakeTags;

    public void Overwrite(FileExtraInfo source)
    {
        OverwriteExtensions.Overwrite(ref Memo, source.Memo);
        OverwriteExtensions.Overwrite(ref Tags, source.Tags);
        HideReason = source.HideReason;
        HideLast = source.HideLast;
        OverwriteExtensions.Overwrite(ref PageExtraInfoDictionary, source.PageExtraInfoDictionary);
        OverwriteExtensions.Overwrite(ref FakeTags, source.FakeTags);
    }
}
