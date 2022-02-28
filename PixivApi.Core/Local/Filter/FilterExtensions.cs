namespace PixivApi.Core.Local;

public static class FilterExtensions
{
    public static async ValueTask<IEnumerable<Artwork>> CreateEnumerableAsync(FinderFacade finderFacade, DatabaseFile database, ArtworkFilter filter, CancellationToken cancellationToken)
    {
        var answer = await PrivateSelectAsync(finderFacade, database, filter, cancellationToken).ConfigureAwait(false);
        if (answer.TryGetNonEnumeratedCount(out var count) && count == 0)
        {
            return Array.Empty<Artwork>();
        }

        if (filter.FileExistanceFilter is { } fileFilter)
        {
            answer = answer.Where(fileFilter.Filter);
        }

        if (filter.Offset > 0)
        {
            answer = answer.Skip(filter.Offset);
        }

        if (filter.Count.HasValue)
        {
            answer = answer.Take(filter.Count.Value);
        }

        return answer;
    }

    public static async IAsyncEnumerable<Artwork> CreateAsyncEnumerable(FinderFacade finderFacade, DatabaseFile database, ArtworkFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var artworks = await PrivateSelectAsync(finderFacade, database, filter, cancellationToken).ConfigureAwait(false);
        if (artworks.TryGetNonEnumeratedCount(out var count) && count == 0)
        {
            yield break;
        }

        var index = 0;
        foreach (var artwork in artworks)
        {
            if (filter.FileExistanceFilter is not null && !filter.FileExistanceFilter.Filter(artwork))
            {
                continue;
            }

            if (++index > filter.Offset)
            {
                yield return artwork;
                if (filter.Count.HasValue && index >= filter.Count.Value + filter.Offset)
                {
                    yield break;
                }
            }
        }
    }

    private static async ValueTask<IEnumerable<Artwork>> PrivateSelectAsync(FinderFacade finderFacade, DatabaseFile database, ArtworkFilter filter, CancellationToken cancellationToken)
    {
        if (filter.Count == 0 || filter.Offset >= database.ArtworkDictionary.Count)
        {
            return Array.Empty<Artwork>();
        }

        await filter.InitializeAsync(finderFacade, database.UserDictionary, database.TagSet, cancellationToken).ConfigureAwait(false);
        ConcurrentBag<Artwork> bag = new();
        await Parallel.ForEachAsync(database.ArtworkDictionary.Values, cancellationToken, (artwork, token) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (filter.FilterWithoutFileExistance(artwork))
            {
                bag.Add(artwork);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        if (filter.Order == ArtworkOrderKind.None)
        {
            return bag;
        }
        else
        {
            return bag.OrderBy(filter.GetKey);
        }
    }
}
