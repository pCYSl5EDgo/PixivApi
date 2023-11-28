namespace PixivApi.Core.Local;

public sealed class TextFilter
{
  [JsonPropertyName("exact")] public string? Exact;
  [JsonPropertyName("partial")] public string[]? Partials;
  [JsonPropertyName("ignore-exact")] public string? IgnoreExact;
  [JsonPropertyName("ignore-partial")] public string[]? IgnorePartials;

  [JsonPropertyName("partial-or")] public bool PartialOr = true;
  [JsonPropertyName("ignore-partial-or")] public bool IgnorePartialOr = true;

  public bool Filter(ReadOnlySpan<string?> span)
  {
    if (Exact is { Length: > 0 })
    {
      foreach (var item in span)
      {
        if (Exact.AsSpan().SequenceEqual(item.AsSpan()))
        {
          goto OK;
        }
      }

      return false;
    OK:;
    }

    if (IgnoreExact is { Length: > 0 })
    {
      foreach (var item in span)
      {
        if (IgnoreExact.AsSpan().SequenceEqual(item.AsSpan()))
        {
          return false;
        }
      }
    }

    if (Partials is { Length: > 0 })
    {
      if (PartialOr)
      {
        foreach (var other in Partials)
        {
          foreach (var item in span)
          {
            if (item is not null && item.Contains(other, StringComparison.Ordinal))
            {
              goto OK;
            }
          }
        }

        return false;
      OK:;
      }
      else
      {
        foreach (var other in Partials)
        {
          foreach (var item in span)
          {
            if (item is not null && item.Contains(other, StringComparison.Ordinal))
            {
              goto OK;
            }
          }

          return false;
        OK:;
        }
      }
    }

    if (IgnorePartials is { Length: > 0 })
    {
      if (IgnorePartialOr)
      {
        foreach (var other in IgnorePartials)
        {
          foreach (var item in span)
          {
            if (item is not null && item.Contains(other, StringComparison.Ordinal))
            {
              return false;
            }
          }
        }
      }
      else
      {
        foreach (var other in IgnorePartials)
        {
          foreach (var item in span)
          {
            if (item is not null && item.Contains(other, StringComparison.Ordinal))
            {
              goto BREAK;
            }
          }

          goto OK;
        BREAK:;
        }

        return false;
      OK:;
      }
    }

    return true;
  }
}
