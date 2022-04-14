namespace PixivApi.Core.Local;

public readonly struct HiddenPageValueTuple
{
    public readonly ulong Id;
    public readonly uint Index;
    public readonly ArtworkType Type;
    public readonly FileExtensionKind Extension;
    public readonly HideReason Reason;

    public HiddenPageValueTuple(ulong id, uint index, ArtworkType type, FileExtensionKind extension, HideReason reason)
    {
        Id = id;
        Index = index;
        Type = type;
        Extension = extension;
        Reason = reason;
    }

    public override string ToString() => $"{{\"Id\": {Id}, \"Index\": {Index}, \"Type\": \"{Type}\", \"Extension\": \"{Extension}\", \"Reason\":\"{Reason}\"}}";
}
