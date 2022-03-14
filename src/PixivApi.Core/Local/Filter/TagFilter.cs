namespace PixivApi.Core.Local;

public sealed class TagFilter
{
    [JsonPropertyName("exact")] public string? Exact;
    [JsonPropertyName("partial")] public string[]? Partials;
    [JsonPropertyName("ignore-exact")] public string? IgnoreExact;
    [JsonPropertyName("ignore-partial")] public string[]? IgnorePartials;

    [JsonPropertyName("partial-or")] public bool PartialOr = true;
    [JsonPropertyName("ignore-partial-or")] public bool IgnorePartialOr = true;

    [JsonIgnore] public HashSet<uint>? SetOk;
    [JsonIgnore] public HashSet<uint>? SetNg;
    [JsonIgnore] public uint ExactNumber;
    [JsonIgnore] public uint IgnoreExactNumber;

    public async ValueTask InitializeAsync(ITagDatabase database, CancellationToken token)
    {
        ExactNumber = string.IsNullOrEmpty(Exact) ? 0 : (await database.FindTagAsync(Exact, token).ConfigureAwait(false)) ?? 0;
        IgnoreExactNumber = string.IsNullOrEmpty(IgnoreExact) ? 0 : (await database.FindTagAsync(IgnoreExact, token).ConfigureAwait(false)) ?? 0;
        SetOk = await CreatePartialAsync(database, Partials, token).ConfigureAwait(false);
        SetNg = await CreatePartialAsync(database, IgnorePartials, token).ConfigureAwait(false);
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
        if (ExactNumber != 0 && notEmpty)
        {
            foreach (var item in enumerable)
            {
                if (ExactNumber == item)
                {
                    goto OK;
                }
            }

            return false;
        OK:;
        }

        if (IgnoreExactNumber != 0 && notEmpty)
        {
            foreach (var item in enumerable)
            {
                if (IgnoreExactNumber == item)
                {
                    return false;
                }
            }
        }

        if (SetOk is not null)
        {
            if (!notEmpty)
            {
                return false;
            }

            if (PartialOr)
            {
                if (!ContainsAny(SetOk, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (!ContainsAll(SetOk, enumerable))
                {
                    return false;
                }
            }
        }

        if (SetNg is not null && notEmpty)
        {
            if (IgnorePartialOr)
            {
                if (ContainsAny(SetNg, enumerable))
                {
                    return false;
                }
            }
            else
            {
                if (ContainsAll(SetNg, enumerable))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
