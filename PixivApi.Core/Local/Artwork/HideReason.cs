namespace PixivApi;

public enum HideReason : byte
{
    NotHidden,
    LowQuality,
    NotMuch,
    Irrelevant,
    ExternalLink,
    Dislike,
    Unfollow,
    Crop,
}
