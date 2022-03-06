namespace PixivApi.Core.Local;

public sealed partial class HideFilter
{
    [JsonPropertyName("allow")] public readonly HashSet<HideReason>? AllowedReason;
    [JsonPropertyName("disallow")] public readonly HashSet<HideReason>? DisallowedReason;

    public bool Filter(HideReason reason)
    {
        if (AllowedReason is { Count: > 0 })
        {
            return AllowedReason.Contains(reason);
        }
        else
        {
            if (DisallowedReason is { Count: > 0 })
            {
                return !DisallowedReason.Contains(reason);
            }
            else
            {
                return true;
            }
        }
    }
}