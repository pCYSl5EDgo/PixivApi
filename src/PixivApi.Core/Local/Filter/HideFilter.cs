namespace PixivApi.Core.Local;

public sealed partial class HideFilter
{
    [JsonPropertyName("allow")] public HideReason[]? AllowedReason;
    [JsonPropertyName("disallow")] public HideReason[]? DisallowedReason;

    public bool Filter(HideReason reason)
    {
        if (AllowedReason is { Length: > 0 })
        {
            return MemoryMarshal.Cast<HideReason, byte>(AllowedReason.AsSpan()).Contains((byte)reason);
        }
        else if (DisallowedReason is { Length: > 0 })
        {
            return !MemoryMarshal.Cast<HideReason, byte>(DisallowedReason.AsSpan()).Contains((byte)reason);
        }
        else
        {
            return true;
        }
    }
}
