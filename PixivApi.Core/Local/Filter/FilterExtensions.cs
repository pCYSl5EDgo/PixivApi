namespace PixivApi.Core.Local;

public static class ArtworkEnumerableHelper
{
    public static async ValueTask<IEnumerable<Artwork>> CreateAsync(ConfigSettings configSettings, DatabaseFile database, ArtworkFilter filter, ParallelOptions parallelOptions)
    {
        if (filter.Count == 0 || filter.Offset >= database.Artworks.Length)
        {
            return Array.Empty<Artwork>();
        }

        await filter.InitializeAsync(configSettings, database.UserDictionary, database.TagSet, parallelOptions).ConfigureAwait(false);
        ConcurrentBag<Artwork> bag = new();
        await Parallel.ForEachAsync(database.Artworks, parallelOptions, (artwork, token) =>
        {
            if (filter.FilterWithoutFileExistance(artwork))
            {
                bag.Add(artwork);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        IEnumerable<Artwork> answer = bag;
        if (filter.Order != ArtworkOrderKind.None)
        {
            answer = answer.OrderBy(filter.GetKey);
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
}
