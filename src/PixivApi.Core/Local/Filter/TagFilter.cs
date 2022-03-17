namespace PixivApi.Core.Local;

public sealed class TagFilter
{
    [JsonPropertyName("exact")] public string[]? Exacts;
    [JsonPropertyName("partial")] public string[]? Partials;
    [JsonPropertyName("ignore-exact")] public string[]? IgnoreExacts;
    [JsonPropertyName("ignore-partial")] public string[]? IgnorePartials;

    [JsonPropertyName("or")] public bool Or = true;
    [JsonPropertyName("ignore-or")] public bool IgnoreOr = true;

    [JsonIgnore] public HashSet<uint>? IntersectSet;
    [JsonIgnore] public HashSet<uint>? ExceptSet;

    public async ValueTask InitializeAsync(ITagDatabase database, CancellationToken token)
    {
        IntersectSet = await CreatePartialAsync(database, Partials, token).ConfigureAwait(false);
        ExceptSet = await CreatePartialAsync(database, IgnorePartials, token).ConfigureAwait(false);

        if (Exacts is { Length: > 0 })
        {
            foreach (var item in Exacts)
            {
                var number = await database.FindTagAsync(item, token).ConfigureAwait(false);
                if (number > 0)
                {
                    (IntersectSet ??= new()).Add(number.Value);
                }
            }
        }

        if (IgnoreExacts is { Length: > 0 })
        {
            foreach (var item in IgnoreExacts)
            {
                var number = await database.FindTagAsync(item, token).ConfigureAwait(false);
                if (number > 0)
                {
                    (ExceptSet ??= new()).Add(number.Value);
                }
            }
        }
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
        if (IntersectSet is { Count: > 0 })
        {
            if (!notEmpty)
            {
                return false;
            }

            if (Or)
            {
                if (!ContainsAny(IntersectSet, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (!ContainsAll(IntersectSet, enumerable))
                {
                    return false;
                }
            }
        }

        if (ExceptSet is { Count: > 0 } && notEmpty)
        {
            if (IgnoreOr)
            {
                if (ContainsAny(ExceptSet, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (ContainsAll(ExceptSet, enumerable))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
