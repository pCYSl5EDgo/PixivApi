namespace PixivApi.Core.Local;

public static class FilterExtensions
{
    public static IAsyncEnumerable<Artwork> ArtworkFilterAsync(this IDatabase database, ArtworkFilter filter, CancellationToken token)
    {
        var collection = filter.IdFilter is { Ids: { Length: > 0 } ids } ? EnumerateByIdAsync(database, ids, token) : database.FilterAsync(filter, token);
        return collection.Reorder(filter);
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
