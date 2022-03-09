namespace PixivApi.Core.Local;

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

    [JsonIgnore] private HashSet<uint>? setExact;
    [JsonIgnore] private HashSet<uint>? setIgnoreExact;
    [JsonIgnore] private HashSet<uint>? setPartial;
    [JsonIgnore] private HashSet<uint>? setIgnorePartial;

    public async ValueTask InitializeAsync(ITagDatabase database, CancellationToken token)
    {
        setExact = await CreateExactAsync(database, Exacts, token).ConfigureAwait(false);
        setIgnoreExact = await CreateExactAsync(database, IgnoreExacts, token).ConfigureAwait(false);
        setPartial = await CreatePartialAsync(database, Partials, token).ConfigureAwait(false);
        setIgnorePartial = await CreatePartialAsync(database, IgnorePartials, token).ConfigureAwait(false);
    }

    private static async ValueTask<HashSet<uint>?> CreateExactAsync(ITagDatabase database, string[]? array, CancellationToken token)
    {
        if (array is not { Length: > 0 })
        {
            return null;
        }

        var answer = new HashSet<uint>(array.Length);
        foreach (var item in array)
        {
            token.ThrowIfCancellationRequested();
            var tag = await database.FindTagAsync(item, token).ConfigureAwait(false);
            if (tag.HasValue)
            {
                answer.Add(tag.Value);
            }
        }

        return answer.Count == 0 ? null : answer;
    }

    private static async ValueTask<HashSet<uint>?> CreatePartialAsync(ITagDatabase database, string[]? array, CancellationToken token)
    {
        if (array is not { Length: > 0 })
        {
            return null;
        }

        var answer = new HashSet<uint>(array.Length);
        foreach (var item in array)
        {
            token.ThrowIfCancellationRequested();
            await foreach (var tag in database.EnumeratePartialMatchAsync(item, token))
            {
                answer.Add(tag);
            }
        }

        return answer.Count == 0 ? null : answer;
    }

    private static (bool, IEnumerable<uint>) CalculateHash(uint[] tags, uint[]? adds, uint[]? removes)
    {
        if (removes is { Length: > 0 })
        {
            if (adds is { Length: > 0 })
            {
                Array.Sort(removes);
                Array.Sort(adds);
                if (adds.SequenceEqual(removes))
                {
                    return (tags.Length != 0, tags);
                }
                else
                {
                    var set = new HashSet<uint>(tags);
                    foreach (var item in adds)
                    {
                        set.Add(item);
                    }

                    foreach (var item in removes)
                    {
                        set.Remove(item);
                    }

                    return (set.Count != 0, set);
                }
            }
            else
            {
                var set = new HashSet<uint>(tags);
                foreach (var item in removes)
                {
                    set.Remove(item);
                }

                return (set.Count != 0, set);
            }
        }
        else
        {
            if (adds is { Length: > 0 })
            {
                var set = new HashSet<uint>(tags);
                foreach (var item in adds)
                {
                    set.Add(item);
                }

                return (set.Count != 0, set);
            }
            else
            {
                return (tags.Length != 0, tags);
            }
        }
    }

    private static bool ContainsAll(HashSet<uint> set, IEnumerable<uint> tags)
    {
        foreach (var tag in tags)
        {
            if (!set.Contains(tag))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsAny(HashSet<uint> set, IEnumerable<uint> tags)
    {
        foreach (var tag in tags)
        {
            if (set.Contains(tag))
            {
                return true;
            }
        }

        return false;
    }

    public bool Filter(uint[] tags, uint[]? adds, uint[]? removes)
    {
        var (notEmpty, enumerable) = CalculateHash(tags, adds, removes);
        if (setExact is not null)
        {
            if (!notEmpty)
            {
                return false;
            }

            if (ExactOr)
            {
                if (!ContainsAny(setExact, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (!ContainsAll(setExact, enumerable))
                {
                    return false;
                }
            }
        }

        if (setIgnoreExact is not null && notEmpty)
        {
            if (IgnoreExactOr)
            {
                if (ContainsAny(setIgnoreExact, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (ContainsAll(setIgnoreExact, enumerable))
                {
                    return false;
                }
            }
        }

        if (setPartial is not null)
        {
            if (!notEmpty)
            {
                return false;
            }

            if (PartialOr)
            {
                if (!ContainsAny(setPartial, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (!ContainsAll(setPartial, enumerable))
                {
                    return false;
                }
            }
        }

        if (setIgnorePartial is not null && notEmpty)
        {
            if (IgnorePartialOr)
            {
                if (ContainsAny(setIgnorePartial, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (ContainsAll(setIgnorePartial, enumerable))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
