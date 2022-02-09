namespace PixivApi;

public sealed class TagFilter
{
    [JsonPropertyName("exact"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string[]? Exacts;
    [JsonPropertyName("partial"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string[]? Partials;
    [JsonPropertyName("ignore-exact"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string[]? IgnoreExacts;
    [JsonPropertyName("ignore-partial"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string[]? IgnorePartials;

    [JsonPropertyName("exact-or")] public bool ExactOr = true;
    [JsonPropertyName("partial-or")] public bool PartialOr = true;
    [JsonPropertyName("ignore-exact-or")] public bool IgnoreExactOr = true;
    [JsonPropertyName("ignore-partial-or")] public bool IgnorePartialOr = true;

    [JsonIgnore]
    public bool IsNoFilter => Exacts is not { Length: > 0 } && Partials is not { Length: > 0 } && IgnoreExacts is not { Length: > 0 } && IgnoreExacts is not { Length: > 0 };

    public bool IsMatch<T>(in StringCompareInfo compareInfo, T[] tags) where T : ITag
    {
        if (Exacts is { Length: > 0 })
        {
            if (tags.Length == 0)
            {
                return false;
            }

            if (ExactOr)
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in Exacts)
                    {
                        if (compareInfo.Equals(tagTag, _tag))
                        {
                            goto BREAK;
                        }
                    }
                }

                return false;
            BREAK:;
            }
            else
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in Exacts)
                    {
                        if (compareInfo.Equals(tagTag, _tag))
                        {
                            goto BREAK;
                        }
                    }
                    return false;
                BREAK:;
                }
            }
        }

        if (Partials is { Length: > 0 })
        {
            if (tags.Length == 0)
            {
                return false;
            }

            if (PartialOr)
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in Partials)
                    {
                        if (compareInfo.Contains(tagTag, _tag))
                        {
                            goto BREAK;
                        }
                    }
                }

                return false;
            BREAK:;
            }
            else
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in Partials)
                    {
                        if (compareInfo.Contains(tagTag, _tag))
                        {
                            goto BREAK;
                        }
                    }

                    return false;
                BREAK:;
                }
            }
        }

        if (tags.Length == 0)
        {
            return true;
        }

        if (IgnoreExacts is { Length: > 0 })
        {
            if (IgnoreExactOr)
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in IgnoreExacts)
                    {
                        if (compareInfo.Equals(tagTag, _tag))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in IgnoreExacts)
                    {
                        if (compareInfo.Equals(tagTag, _tag))
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

        if (IgnorePartials is { Length: > 0 })
        {
            if (IgnorePartialOr)
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in IgnorePartials)
                    {
                        if (compareInfo.Contains(tagTag, _tag))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                foreach (var tag in tags)
                {
                    var tagTag = tag.Tag;
                    foreach (var _tag in IgnorePartials)
                    {
                        if (compareInfo.Contains(tagTag, _tag))
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

    public bool IsMatch(in StringCompareInfo compareInfo, string[]? tags)
    {
        if (Exacts is { Length: > 0 })
        {
            if (tags is not { Length: > 0 })
            {
                return false;
            }

            if (ExactOr)
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in Exacts)
                    {
                        if (compareInfo.Equals(tag, _tag))
                        {
                            goto BREAK;
                        }
                    }
                }

                return false;
            BREAK:;
            }
            else
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in Exacts)
                    {
                        if (compareInfo.Equals(tag, _tag))
                        {
                            goto BREAK;
                        }
                    }
                    return false;
                BREAK:;
                }
            }
        }

        if (Partials is { Length: > 0 })
        {
            if (tags is not { Length: > 0 })
            {
                return false;
            }

            if (PartialOr)
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in Partials)
                    {
                        if (compareInfo.Contains(tag, _tag))
                        {
                            goto BREAK;
                        }
                    }
                }

                return false;
            BREAK:;
            }
            else
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in Partials)
                    {
                        if (compareInfo.Contains(tag, _tag))
                        {
                            goto BREAK;
                        }
                    }

                    return false;
                BREAK:;
                }
            }
        }

        if (tags is null or { Length: 0 })
        {
            return true;
        }

        if (IgnoreExacts is { Length: > 0 })
        {
            if (IgnoreExactOr)
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in IgnoreExacts)
                    {
                        if (compareInfo.Equals(tag, _tag))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in IgnoreExacts)
                    {
                        if (compareInfo.Equals(tag, _tag))
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

        if (IgnorePartials is { Length: > 0 })
        {
            if (IgnorePartialOr)
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in IgnorePartials)
                    {
                        if (compareInfo.Contains(tag, _tag))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                foreach (var tag in tags)
                {
                    foreach (var _tag in IgnorePartials)
                    {
                        if (compareInfo.Contains(tag, _tag))
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
