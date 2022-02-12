namespace PixivApi;

public sealed class StringSet<T>
{
    public StringSet(StringCompareInfo compareInfo)
    {
        this.compareInfo = compareInfo;
    }

    private readonly List<List<string>?> tagListList = new();
    private readonly List<List<T>?> valueListList = new();
    private readonly StringCompareInfo compareInfo;
    private bool hasEmpty;
    private T? empty;

    public bool ContainsExact(ReadOnlySpan<char> key)
    {
        if (key.Length == 0)
        {
            return hasEmpty;
        }

        var index = key.Length - 1;
        if (index >= tagListList.Count)
        {
            return false;
        }

        if (tagListList[index] is not { Count: > 0 } list)
        {
            return false;
        }

        foreach (var item in list)
        {
            if (compareInfo.Equals(item, key))
            {
                return true;
            }
        }

        return false;
    }

    public bool ContainsPartial(ReadOnlySpan<char> key)
    {
        if (key.Length == 0)
        {
            return hasEmpty;
        }

        var index = key.Length - 1;
        if (index >= tagListList.Count)
        {
            return false;
        }

        var tagListSpan = CollectionsMarshal.AsSpan(tagListList)[index..];
        foreach (var tagList in tagListSpan)
        {
            if (tagList is not { Count: > 0 })
            {
                continue;
            }

            foreach (var tag in CollectionsMarshal.AsSpan(tagList))
            {
                if (compareInfo.Contains(tag, key))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private ref T? Add(List<string> tagList, List<T> valueList, string key)
    {
        tagList.Add(key);
        valueList.Add(default!);
        return ref CollectionsMarshal.AsSpan(valueList)[^1]!;
    }

    public ref T? TryGetValueOrAddDefaultExact(string key, out bool exists)
    {
        if (key.Length == 0)
        {
            exists = hasEmpty;
            hasEmpty = true;
            empty = default;
            return ref empty;
        }

        var index = key.Length - 1;
        while (index >= tagListList.Count)
        {
            tagListList.Add(null);
            valueListList.Add(null);
        }

        var tagList = CollectionsMarshal.AsSpan(tagListList)[index] ??= new();
        var valueList = CollectionsMarshal.AsSpan(valueListList)[index] ??= new();
        for (int i = 0; i < tagList.Count; i++)
        {
            if (compareInfo.Equals(tagList[i], key))
            {
                exists = true;
                return ref CollectionsMarshal.AsSpan(valueList)[i]!;
            }
        }

        exists = false;
        return ref Add(tagList, valueList, key);
    }

    public ref T? TryGetValueOrAddDefaultPartial(string key, out bool exists)
    {
        if (key.Length == 0)
        {
            exists = hasEmpty;
            hasEmpty = true;
            empty = default;
            return ref empty;
        }

        var index = key.Length - 1;
        while (index >= tagListList.Count)
        {
            tagListList.Add(null);
            valueListList.Add(null);
        }

        var tagListSpan = CollectionsMarshal.AsSpan(tagListList);
        var valueListSpan = CollectionsMarshal.AsSpan(valueListList);
        var tagList = tagListSpan[index] ??= new();
        var valueList = valueListSpan[index] ??= new();
        for (int i = 0; i < tagList.Count; i++)
        {
            if (!compareInfo.Equals(tagList[i], key))
            {
                continue;
            }

            exists = true;
            return ref CollectionsMarshal.AsSpan(valueList)[i]!;
        }

        for (int i = index + 1; i < tagListSpan.Length; i++)
        {
            var tagListPartialMatch = tagListSpan[i];
            if (tagListPartialMatch is not { Count: > 0 })
            {
                continue;
            }

            for (int j = 0; j < tagListPartialMatch.Count; j++)
            {
                if (!compareInfo.Contains(tagListPartialMatch[j], key))
                {
                    continue;
                }

                exists = true;
                return ref CollectionsMarshal.AsSpan(valueListSpan[i])[j]!;
            }
        }

        exists = false;
        return ref Add(tagList, valueList, key);
    }

    public ref T? TryGetValueOrAddDefaultExact(ReadOnlySpan<char> key, out bool exists)
    {
        if (key.Length == 0)
        {
            exists = hasEmpty;
            hasEmpty = true;
            empty = default;
            return ref empty;
        }

        var index = key.Length - 1;
        while (index >= tagListList.Count)
        {
            tagListList.Add(null);
            valueListList.Add(null);
        }

        var tagList = CollectionsMarshal.AsSpan(tagListList)[index] ??= new();
        var valueList = CollectionsMarshal.AsSpan(valueListList)[index] ??= new();
        for (int i = 0; i < tagList.Count; i++)
        {
            if (compareInfo.Equals(tagList[i], key))
            {
                exists = true;
                return ref CollectionsMarshal.AsSpan(valueList)[i]!;
            }
        }

        exists = false;
        return ref Add(tagList, valueList, new(key));
    }

    public ref T? TryGetValueOrAddDefaultPartial(ReadOnlySpan<char> key, out bool exists)
    {
        if (key.Length == 0)
        {
            exists = hasEmpty;
            hasEmpty = true;
            empty = default;
            return ref empty;
        }

        var index = key.Length - 1;
        while (index >= tagListList.Count)
        {
            tagListList.Add(null);
            valueListList.Add(null);
        }

        var tagListSpan = CollectionsMarshal.AsSpan(tagListList);
        var valueListSpan = CollectionsMarshal.AsSpan(valueListList);
        var tagList = tagListSpan[index] ??= new();
        var valueList = valueListSpan[index] ??= new();
        for (int i = 0; i < tagList.Count; i++)
        {
            if (!compareInfo.Equals(tagList[i], key))
            {
                continue;
            }

            exists = true;
            return ref CollectionsMarshal.AsSpan(valueList)[i]!;
        }

        for (int i = index + 1; i < tagListSpan.Length; i++)
        {
            var tagListPartialMatch = tagListSpan[i];
            if (tagListPartialMatch is not { Count: > 0 })
            {
                continue;
            }

            for (int j = 0; j < tagListPartialMatch.Count; j++)
            {
                if (!compareInfo.Contains(tagListPartialMatch[j], key))
                {
                    continue;
                }

                exists = true;
                return ref CollectionsMarshal.AsSpan(valueListSpan[i])[j]!;
            }
        }

        exists = false;
        return ref Add(tagList, valueList, new(key));
    }

    public ref T TryGetValueExact(ReadOnlySpan<char> key)
    {
        if (key.Length == 0)
        {
            if (hasEmpty)
            {
                return ref empty!;
            }
            else
            {
                goto NULL;
            }
        }

        var index = key.Length - 1;
        if (index >= tagListList.Count)
        {
            goto NULL;
        }

        var tagList = tagListList[index];
        if (tagList is not { Count: > 0 })
        {
            goto NULL;
        }

        for (int i = 0; i < tagList.Count; i++)
        {
            if (compareInfo.Equals(tagList[i], key))
            {
                return ref CollectionsMarshal.AsSpan(valueListList[index])[i];
            }
        }

    NULL:
        return ref Unsafe.NullRef<T>();
    }

    public ref T TryGetValuePartial(ReadOnlySpan<char> key)
    {
        if (key.Length == 0)
        {
            if (hasEmpty)
            {
                return ref empty!;
            }
            else
            {
                goto NULL;
            }
        }

        var index = key.Length - 1;
        if (index >= tagListList.Count)
        {
            goto NULL;
        }

        for (int i = index; i < tagListList.Count; i++)
        {
            if (tagListList[i] is not { Count: > 0 } tagList)
            {
                continue;
            }

            for (int j = 0; j < tagList.Count; j++)
            {
                if (compareInfo.Contains(tagList[j], key))
                {
                    return ref CollectionsMarshal.AsSpan(CollectionsMarshal.AsSpan(valueListList)[i])[j];
                }
            }
        }

    NULL:
        return ref Unsafe.NullRef<T>();
    }
}

public sealed class StringSet
{
    public StringSet(StringCompareInfo compareInfo)
    {
        this.compareInfo = compareInfo;
    }

    private readonly List<List<string>?> tagListList = new();
    private readonly StringCompareInfo compareInfo;
    private bool hasEmpty;

    public bool ContainsExact(ReadOnlySpan<char> key)
    {
        if (key.Length == 0)
        {
            return hasEmpty;
        }

        var index = key.Length - 1;
        if (index >= tagListList.Count)
        {
            return false;
        }

        if (tagListList[index] is not { Count: > 0 } list)
        {
            return false;
        }

        foreach (var item in list)
        {
            if (compareInfo.Equals(item, key))
            {
                return true;
            }
        }

        return false;
    }

    public bool ContainsPartial(ReadOnlySpan<char> key)
    {
        if (key.Length == 0)
        {
            return hasEmpty;
        }

        var index = key.Length - 1;
        if (index >= tagListList.Count)
        {
            return false;
        }

        var tagListSpan = CollectionsMarshal.AsSpan(tagListList)[index..];
        foreach (var tagList in tagListSpan)
        {
            if (tagList is not { Count: > 0 })
            {
                continue;
            }

            foreach (var tag in CollectionsMarshal.AsSpan(tagList))
            {
                if (compareInfo.Contains(tag, key))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Add(string key)
    {
        if (key.Length == 0)
        {
            hasEmpty = true;
            return;
        }

        var index = key.Length - 1;
        while (index >= tagListList.Count)
        {
            tagListList.Add(null);
        }

        var tagList = CollectionsMarshal.AsSpan(tagListList)[index] ??= new();
        for (int i = 0; i < tagList.Count; i++)
        {
            if (compareInfo.Equals(tagList[i], key))
            {
                return;
            }
        }

        tagList.Add(key);
    }

    public void Add(ReadOnlySpan<char> key)
    {
        if (key.Length == 0)
        {
            hasEmpty = true;
            return;
        }

        var index = key.Length - 1;
        while (index >= tagListList.Count)
        {
            tagListList.Add(null);
        }

        var tagList = CollectionsMarshal.AsSpan(tagListList)[index] ??= new();
        for (int i = 0; i < tagList.Count; i++)
        {
            if (compareInfo.Equals(tagList[i], key))
            {
                return;
            }
        }

        tagList.Add(new(key));
    }
}
