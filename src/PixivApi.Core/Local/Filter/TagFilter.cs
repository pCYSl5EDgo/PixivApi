﻿namespace PixivApi.Core.Local;

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

    private StringSet? tagSet;
    [JsonIgnore] public HashSet<uint>? PartialSet;
    [JsonIgnore] public HashSet<uint>? IgnorePartialSet;

    public async ValueTask InitializeAsync(StringSet? set, CancellationToken cancellationToken)
    {
        if (ReferenceEquals(tagSet, set))
        {
            return;
        }

        tagSet = set;
        if (set is not { Reverses.IsEmpty: false })
        {
            return;
        }

        ConcurrentBag<uint>? bag = null;
        if (Partials is { Length: > 0 })
        {
            if (bag is null)
            {
                bag = new();
            }
            else
            {
                bag.Clear();
            }

            await Parallel.ForEachAsync(set.Values, cancellationToken, (pair, token) =>
            {
                var (key, value) = pair;
                if (value is { Length: > 0 })
                {
                    foreach (var text in Partials)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return ValueTask.FromCanceled(token);
                        }

                        if (value.Contains(text))
                        {
                            bag.Add(key);
                        }
                    }
                }

                return ValueTask.CompletedTask;
            });
            PartialSet = new(bag);
        }

        if (IgnorePartials is { Length: > 0 })
        {
            if (bag is null)
            {
                bag = new();
            }
            else
            {
                bag.Clear();
            }

            await Parallel.ForEachAsync(set.Values, cancellationToken, (pair, token) =>
            {
                var (key, value) = pair;
                if (value is { Length: > 0 })
                {
                    foreach (var text in IgnorePartials)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return ValueTask.FromCanceled(token);
                        }

                        if (value.Contains(text))
                        {
                            bag.Add(key);
                        }
                    }
                }

                return ValueTask.CompletedTask;
            });
            IgnorePartialSet = new(bag);
        }
    }

    public bool Filter(uint[] tags, uint[]? extraTags, uint[]? extraFakeTags)
    {
        var set = new HashSet<uint>(tags);
        if (extraTags is { Length: > 0 })
        {
            foreach (var tag in tags)
            {
                set.Add(tag);
            }
        }

        if (extraFakeTags is { Length: > 0 })
        {

            foreach (var tag in extraFakeTags)
            {
                set.Remove(tag);
            }
        }

        if (Exacts is { Length: > 0 })
        {
            if (tagSet is not { Reverses: { Count: > 0 } dictionary })
            {
                return false;
            }

            if (ExactOr)
            {
                foreach (var item in Exacts)
                {
                    if (dictionary.TryGetValue(item, out var tag) && set.Contains(tag))
                    {
                        goto OK;
                    }
                }

                return false;
            OK:;
            }
            else
            {
                foreach (var item in Exacts)
                {
                    if (!dictionary.TryGetValue(item, out var tag) || !set.Contains(tag))
                    {
                        return false;
                    }
                }
            }
        }

        if (IgnoreExacts is { Length: > 0 })
        {
            if (tagSet is { Reverses: { Count: > 0 } dictionary })
            {
                if (IgnoreExactOr)
                {
                    foreach (var item in IgnoreExacts)
                    {
                        if (dictionary.TryGetValue(item, out var key) && set.Contains(key))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    foreach (var item in IgnoreExacts)
                    {
                        if (!dictionary.TryGetValue(item, out var key) || !set.Contains(key))
                        {
                            goto OK;
                        }
                    }

                    return false;
                OK:;
                }
            }
        }

        if (PartialSet is not null)
        {
            if (PartialOr)
            {
                foreach (var item in set)
                {
                    if (PartialSet.Contains(item))
                    {
                        goto OK;
                    }
                }

                return false;
            OK:;
            }
            else
            {
                foreach (var item in set)
                {
                    if (!PartialSet.Contains(item))
                    {
                        return false;
                    }
                }
            }
        }

        if (IgnorePartialSet is not null)
        {
            if (IgnorePartialOr)
            {
                foreach (var item in set)
                {
                    if (IgnorePartialSet.Contains(item))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var item in set)
                {
                    if (!IgnorePartialSet.Contains(item))
                    {
                        goto OK;
                    }
                }

                return false;
            OK:;
            }
        }

        return true;
    }
}