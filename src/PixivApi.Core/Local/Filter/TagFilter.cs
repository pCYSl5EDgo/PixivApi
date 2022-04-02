namespace PixivApi.Core.Local;

public sealed class TagFilter
{
    [JsonPropertyName("exact")] public string[]? Exacts;
    [JsonPropertyName("partial")] public string[]? Partials;
    [JsonPropertyName("ignore-exact")] public string[]? IgnoreExacts;
    [JsonPropertyName("ignore-partial")] public string[]? IgnorePartials;

    [JsonPropertyName("or")] public bool Or = false;
    [JsonPropertyName("ignore-or")] public bool IgnoreOr = true;

    [JsonIgnore] public bool AlwaysFailIntersect;
    [JsonIgnore] public bool AlwaysFailExcept;
    [JsonIgnore] public uint[] IntersectArray = Array.Empty<uint>();
    [JsonIgnore] public uint[] ExceptArray = Array.Empty<uint>();
    [JsonIgnore] public uint[][] IntersectArrayArray = Array.Empty<uint[]>();
    [JsonIgnore] public uint[][] ExceptArrayArray = Array.Empty<uint[]>();

    public async ValueTask InitializeAsync(ITagDatabase database, CancellationToken token)
    {
        if (Or)
        {
            IntersectArrayArray = await CalculateSetsAsync(database, Partials, Exacts, token).ConfigureAwait(false);
        }
        else
        {
            (AlwaysFailIntersect, IntersectArray) = await CalculateArrayAsync(database, Exacts, token).ConfigureAwait(false);
            IntersectArrayArray = await CalculateSetsAsync(database, Partials, token).ConfigureAwait(false);
        }

        if (IgnoreOr)
        {
            ExceptArrayArray = await CalculateSetsAsync(database, IgnorePartials, IgnoreExacts, token).ConfigureAwait(false);
        }
        else
        {
            (AlwaysFailExcept, ExceptArray) = await CalculateArrayAsync(database, IgnoreExacts, token).ConfigureAwait(false);
            ExceptArrayArray = await CalculateSetsAsync(database, IgnorePartials, token).ConfigureAwait(false);
        }
    }

    private static async ValueTask<(bool, uint[])> CalculateArrayAsync(ITagDatabase database, string[]? exacts, CancellationToken token)
    {
        if (exacts is not { Length: > 0 })
        {
            return (false, Array.Empty<uint>());
        }

        var rental = ArrayPool<uint>.Shared.Rent(exacts.Length);
        try
        {
            var answerCount = 0;
            for (var i = 0; i < exacts.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                var item = await database.FindTagAsync(exacts[i], token).ConfigureAwait(false);
                if (!item.HasValue)
                {
                    return (true, Array.Empty<uint>());
                }

                rental[answerCount++] = item.Value;
            }

            var answer = new uint[answerCount];
            Array.Copy(rental, answer, answerCount);
            return (false, answer);
        }
        finally
        {
            ArrayPool<uint>.Shared.Return(rental);
        }
    }

    private static async ValueTask<uint[][]> CalculateSetsAsync(ITagDatabase database, string[]? partials, string[]? exacts, CancellationToken token)
    {
        if (exacts is not { Length: > 0 } && partials is not { Length: > 0 })
        {
            return Array.Empty<uint[]>();
        }

        var answer = new uint[1][];
        var set = new HashSet<uint>();
        if (exacts is { Length: > 0 })
        {
            foreach (var item in exacts)
            {
                token.ThrowIfCancellationRequested();
                var found = await database.FindTagAsync(item, token).ConfigureAwait(false);
                if (found.HasValue)
                {
                    set.Add(found.Value);
                }
            }
        }

        if (partials is { Length: > 0 })
        {
            foreach (var item in partials)
            {
                token.ThrowIfCancellationRequested();
                await foreach (var found in database.EnumeratePartialMatchTagAsync(item, token))
                {
                    set.Add(found);
                }
            }
        }

        answer[0] = set.ToArray();
        return answer;
    }

    private static async ValueTask<uint[][]> CalculateSetsAsync(ITagDatabase database, string[]? partials, CancellationToken token)
    {
        if (partials is not { Length: > 0 })
        {
            return Array.Empty<uint[]>();
        }

        var answer = new uint[partials.Length][];
        for (var i = 0; i < answer.Length; i++)
        {
            token.ThrowIfCancellationRequested();

            var item = partials[i];
            answer[i] = await database.EnumeratePartialMatchTagAsync(item, token).ToArrayAsync(token).ConfigureAwait(false);
        }

        return answer;
    }

    public bool Filter(Dictionary<uint, uint> dictionary)
    {
        if (AlwaysFailIntersect)
        {
            return false;
        }

        foreach (var item in IntersectArray)
        {
            if (!dictionary.TryGetValue(item, out var kind) || kind == 0)
            {
                return false;
            }
        }

        foreach (var array in IntersectArrayArray)
        {
            foreach (var item in array)
            {
                if (dictionary.TryGetValue(item, out var kind) && kind != 0)
                {
                    goto OK;
                }
            }

            return false;
        OK:;
        }

        if (!AlwaysFailExcept)
        {
            foreach (var item in ExceptArray)
            {
                if (!dictionary.TryGetValue(item, out var kind) || kind == 0)
                {
                    goto OK_Except;
                }
            }

            foreach (var array in ExceptArrayArray)
            {
                foreach (var item in array)
                {
                    if (dictionary.TryGetValue(item, out var kind) && kind != 0)
                    {
                        goto NG;
                    }
                }

                goto OK_Except;
            NG:;
            }

            return false;
        }

    OK_Except:
        return true;
    }
}
