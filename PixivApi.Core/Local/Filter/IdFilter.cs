namespace PixivApi.Core.Local;

public sealed class IdFilter : IFilter<ulong>
{
    [JsonPropertyName("id")]
    public ulong[]? Ids;

    [JsonPropertyName("ignore-id")]
    public ulong[]? IgnoreIds;

    public bool Filter(ulong id)
    {
        if (Ids is { Length: > 0 })
        {
            if (Array.IndexOf(Ids, id) == -1)
            {
                return false;
            }
        }

        if (IgnoreIds is { Length: > 0 })
        {
            if (Array.IndexOf(IgnoreIds, id) != -1)
            {
                return false;
            }
        }

        return true;
    }
}
