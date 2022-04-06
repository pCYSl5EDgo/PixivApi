namespace PixivApi.Core.Local;

public enum HideReason : byte
{
    NotHidden,
    TemporaryHidden,
    LowQuality,
    Irrelevant,
    ExternalLink,
    Dislike,
    Crop,
}
