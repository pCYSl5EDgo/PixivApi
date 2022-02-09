namespace PixivApi;

[MessagePackObject]
public sealed class FileExtraInfo : IOverwrite<FileExtraInfo>
{
    [Key(0), JsonPropertyName("memo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Memo;
    [Key(1), JsonPropertyName("tags"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string[]? Tags;
    [Key(2), JsonPropertyName("hide")] public HideReason HideReason;
    [Key(3), JsonPropertyName("hide-last")] public bool HideLast;
    [Key(4), JsonPropertyName("pages"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<uint, FilePageExtraInfo>? PageExtraInfoDictionary;

    public void Overwrite(FileExtraInfo source)
    {
        OverwriteExtensions.Overwrite(ref Memo, source.Memo);
        OverwriteExtensions.Overwrite(ref Tags, source.Tags);
        HideReason = source.HideReason;
        HideLast = source.HideLast;
        OverwriteExtensions.Overwrite(ref PageExtraInfoDictionary, source.PageExtraInfoDictionary);
    }
}
