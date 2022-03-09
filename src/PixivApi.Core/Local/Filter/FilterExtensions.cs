namespace PixivApi.Core.Local;

public static class FilterExtensions
{
    public static IAsyncEnumerable<Artwork> ArtworkFilterAsync(this IDatabase database, ArtworkFilter filter, CancellationToken token)
    {
        if (filter.Count == 0)
        {
            return AsyncEnumerable.Empty<Artwork>();
        }

        IAsyncEnumerable<Artwork> answer;
        if (filter.IdFilter is { Ids: { Length: > 0 } ids })
        {
            answer = EnumerateByIdAsync(database, ids, token).WhereAwaitWithCancellation(async (v, token) =>
            {
                if (!filter.FastFilter(database, v))
                {
                    return false;
                }

                if (filter.HasSlowFilter && !await filter.SlowFilter(database, v, token).ConfigureAwait(false))
                {
                    return false;
                }

                return true;
            });
        }
        else
        {
            answer = database.FilterAsync(filter, token);
        }

        return answer.Reorder(filter);
    }

    /// <summary>
    /// This returns the same result of what <see cref="ArtworkFilterAsync"/> returns.
    /// This consumes more memory than that.
    /// This is more appropriate for the command line tool because user don't have to wait all of the slow filter.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static IAsyncEnumerable<Artwork> FastArtworkFilterAsync(this IDatabase database, ArtworkFilter filter, CancellationToken token) => filter.Count == 0 ? AsyncEnumerable.Empty<Artwork>() : database.PrivateFastArtworkFilterAsync(filter, token).SkipTake(filter.Offset, filter.Count);

    private static async IAsyncEnumerable<Artwork> PrivateFastArtworkFilterAsync(this IDatabase database, ArtworkFilter filter, [EnumeratorCancellation] CancellationToken token)
    {
        IEnumerable<Artwork> collection;
        if (filter.IdFilter is { Ids: { Length: > 0 } ids })
        {
            var bag = new ConcurrentBag<Artwork>();
            await Parallel.ForEachAsync(ids, token, async (id, token) =>
            {
                var artwork = await database.GetArtworkAsync(id, token).ConfigureAwait(false);
                if (artwork is not null && filter.FastFilter(database, artwork))
                {
                    bag.Add(artwork);
                }
            }).ConfigureAwait(false);
            collection = bag;
        }
        else
        {
            collection = await database.FastFilterAsync(filter, token).ConfigureAwait(false);
        }

        token.ThrowIfCancellationRequested();
        if (filter.Order != ArtworkOrderKind.None)
        {
            collection = collection.OrderBy(filter.GetKey);
        }

        if (filter.HasSlowFilter)
        {
            foreach (var artwork in collection)
            {
                if (token.IsCancellationRequested)
                {
                    yield break;
                }

                if (await filter.SlowFilter(database, artwork, token).ConfigureAwait(false))
                {
                    yield return artwork;
                }
            }
        }
        else
        {
            foreach (var artwork in collection)
            {
                if (token.IsCancellationRequested)
                {
                    yield break;
                }

                yield return artwork;
            }
        }
    }

    private static IAsyncEnumerable<Artwork> SkipTake(this IAsyncEnumerable<Artwork> collection, int offset, int? count)
    {
        if (offset > 0)
        {
            collection = collection.Skip(offset);
        }

        if (count > 0)
        {
            collection = collection.Take(count.Value);
        }

        return collection;
    }

    private static IAsyncEnumerable<Artwork> Reorder(this IAsyncEnumerable<Artwork> collection, ArtworkFilter filter)
    {
        if (filter.Order != ArtworkOrderKind.None)
        {
            collection = collection.OrderBy(filter.GetKey);
        }

        if (filter.Offset > 0)
        {
            collection = collection.Skip(filter.Offset);
        }

        if (filter.Count > 0)
        {
            collection = collection.Take(filter.Count.Value);
        }

        return collection;
    }

    private static async IAsyncEnumerable<Artwork> EnumerateByIdAsync(IArtworkDatabase database, ulong[] ids, [EnumeratorCancellation] CancellationToken token)
    {
        foreach (var id in ids)
        {
            if (token.IsCancellationRequested)
            {
                yield break;
            }

            var artwork = await database.GetArtworkAsync(id, token).ConfigureAwait(false);
            if (artwork is not null)
            {
                yield return artwork;
            }
        }
    }
}
