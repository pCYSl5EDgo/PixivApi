namespace PixivApi.Core.Local;

public sealed class MinMaxFilter
{
  [JsonPropertyName("min")] public ulong? Min;
  [JsonPropertyName("max")] public ulong? Max;

  [JsonIgnore]
  public bool IsNoFilter => (!Min.HasValue || Min.Value == 0) && (!Max.HasValue || Max.Value == ulong.MaxValue);

  public bool Filter(ulong value)
  {
    if (Min.HasValue)
    {
      if (value < Min.Value)
      {
        return false;
      }
    }

    if (Max.HasValue)
    {
      if (value > Max.Value)
      {
        return false;
      }
    }

    return true;
  }
}
