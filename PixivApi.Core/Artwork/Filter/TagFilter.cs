namespace PixivApi;

public sealed class TagFilter
{
    [JsonPropertyName("exact")] public string[]? Exacts;
    [JsonPropertyName("partial")] public string[]? Partials;
    [JsonPropertyName("ignore-exact")] public string[]? IgnoreExacts;
    [JsonPropertyName("ignore-partial")] public string[]? IgnorePartials;

    [JsonPropertyName("exact-or")] public bool ExactOr = true;
    [JsonPropertyName("partial-or")] public bool PartialOr = true;
    [JsonPropertyName("ignore-exact-or")] public bool IgnoreExactOr = true;
    [JsonPropertyName("ignore-partial-or")] public bool IgnorePartialOr = true;

    [JsonIgnore]
    public bool IsNoFilter => Exacts is not { Length: > 0 } && Partials is not { Length: > 0 } && IgnoreExacts is not { Length: > 0 } && IgnoreExacts is not { Length: > 0 };

    public bool IsMatch<T>(in StringCompareInfo compareInfo, T[] tags, string[]? additionalTags, string[]? fakeTags) where T : ITag
    {
        var hasAddition = additionalTags is { Length: > 0 };
        var hasFake = fakeTags is { Length: > 0 };
        HashSet<string>? tagsSet = hasAddition || hasFake ? new HashSet<string>(compareInfo) : null;
        if (tagsSet is not null)
        {
            foreach (var t in tags)
            {
                tagsSet.Add(t.Tag);
            }

            if (hasAddition)
            {
                foreach (var t in additionalTags!)
                {
                    tagsSet.Add(t);
                }
            }

            if (hasFake)
            {
                foreach (var t in fakeTags!)
                {
                    tagsSet.Remove(t);
                }
            }
        }

        if (Exacts is { Length: > 0 })
        {
            if (ExactOr)
            {
                if (tagsSet is null)
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
                }
                else
                {
                    foreach (var _tag in Exacts)
                    {
                        if (tagsSet.Contains(_tag))
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
                if (tagsSet is null)
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
                else
                {
                    foreach (var _tag in Exacts)
                    {
                        if (!tagsSet.Contains(_tag))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        if (Partials is { Length: > 0 })
        {
            if (PartialOr)
            {
                if (tagsSet is null)
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
                }
                else
                {
                    foreach (var tag in tagsSet)
                    {
                        foreach (var _tag in Partials)
                        {
                            if (compareInfo.Contains(tag, _tag))
                            {
                                goto BREAK;
                            }
                        }
                    }
                }

                return false;
            BREAK:;
            }
            else
            {
                if (tagsSet is null)
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
                else
                {
                    foreach (var tag in tagsSet)
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
        }

        if (IgnoreExacts is { Length: > 0 })
        {
            if (IgnoreExactOr)
            {
                if (tagsSet is null)
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
                    foreach (var _tag in IgnoreExacts)
                    {
                        if (tagsSet.Contains(_tag))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (tagsSet is null)
                {
                    foreach (var tag in tags)
                    {
                        var tagTag = tag.Tag;
                        foreach (var _tag in IgnoreExacts)
                        {
                            if (!compareInfo.Equals(tagTag, _tag))
                            {
                                goto OK;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var _tag in IgnoreExacts)
                    {
                        if (!tagsSet.Contains(_tag))
                        {
                            goto OK;
                        }
                    }
                }

                return false;
            OK:;
            }
        }

        if (IgnorePartials is { Length: > 0 })
        {
            if (IgnorePartialOr)
            {
                if (tagsSet is null)
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
                    foreach (var tag in tagsSet)
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
            }
            else
            {
                if (tagsSet is null)
                {
                    foreach (var tag in tags)
                    {
                        var tagTag = tag.Tag;
                        foreach (var _tag in IgnorePartials)
                        {
                            if (!compareInfo.Contains(tagTag, _tag))
                            {
                                goto OK;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var tag in tagsSet)
                    {
                        foreach (var _tag in IgnorePartials)
                        {
                            if (!compareInfo.Contains(tag, _tag))
                            {
                                goto OK;
                            }
                        }
                    }
                }

                return false;
            OK:;
            }
        }

        return true;
    }

    public bool IsMatch(in StringCompareInfo compareInfo, ReadOnlySpan<string> tags)
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

        if (tags.IsEmpty)
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
                        if (!compareInfo.Equals(tag, _tag))
                        {
                            goto OK;
                        }
                    }
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
                        if (!compareInfo.Contains(tag, _tag))
                        {
                            goto OK;
                        }
                    }
                }

                return false;
            OK:;
            }
        }

        return true;
    }

    public bool IsMatch(StringCompareInfo compareInfo, HashSet<string> tags)
    {
        if (Exacts is { Length: > 0 })
        {
            if (ExactOr)
            {
                foreach (var _tag in Exacts)
                {
                    if (tags.Contains(_tag))
                    {
                        goto BREAK;
                    }
                }

                return false;
            BREAK:;
            }
            else
            {
                foreach (var _tag in Exacts)
                {
                    if (tags.Contains(_tag))
                    {
                        goto BREAK;
                    }
                }

                return false;
            BREAK:;
            }
        }

        if (Partials is { Length: > 0 })
        {
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

        if (IgnoreExacts is { Length: > 0 })
        {
            if (IgnoreExactOr)
            {
                foreach (var _tag in IgnoreExacts)
                {
                    if (tags.Contains(_tag))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var _tag in IgnoreExacts)
                {
                    if (!tags.Contains(_tag))
                    {
                        goto OK;
                    }
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
                        if (!compareInfo.Contains(tag, _tag))
                        {
                            goto OK;
                        }
                    }
                }

                return false;
            OK:;
            }
        }

        return true;
    }
}
