namespace PixivApi.Core.Local;

public enum HideReason : byte
{
    NotHidden,
    LowQuality,
    Irrelevant,
    ExternalLink,
    Dislike,
    Crop,
}
