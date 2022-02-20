namespace PixivApi.Core.Local;

public sealed class TextFilter
{
    [JsonPropertyName("exact")] public string[]? Exacts;
    [JsonPropertyName("partial")] public string[]? Partials;
    [JsonPropertyName("ignore-exact")] public string[]? IgnoreExacts;
    [JsonPropertyName("ignore-partial")] public string[]? IgnorePartials;

    [JsonPropertyName("exact-or")] public bool ExactOr = true;
    [JsonPropertyName("partial-or")] public bool PartialOr = true;
    [JsonPropertyName("ignore-exact-or")] public bool IgnoreExactOr = true;
    [JsonPropertyName("ignore-partial-or")] public bool IgnorePartialOr = true;

    public bool Filter(ReadOnlySpan<string?> span)
    {
        if (Exacts is { Length: > 0 })
        {
            if (ExactOr)
            {
                foreach (var other in Exacts)
                {
                    foreach (var item in span)
                    {
                        if (item is not null && item.SequenceEqual(other))
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
                foreach (var other in Exacts)
                {
                    foreach (var item in span)
                    {
                        if (item is not null && item.SequenceEqual(other))
                        {
                            goto OK;
                        }
                    }

                    return false;
                OK:;
                }
            }
        }

        if (IgnoreExacts is { Length: > 0 })
        {
            if (IgnoreExactOr)
            {
                foreach (var other in IgnoreExacts)
                {
                    foreach (var item in span)
                    {
                        if (item is not null && item.SequenceEqual(other))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                foreach (var other in IgnoreExacts)
                {
                    foreach (var item in span)
                    {
                        if (item is not null && item.SequenceEqual(other))
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
