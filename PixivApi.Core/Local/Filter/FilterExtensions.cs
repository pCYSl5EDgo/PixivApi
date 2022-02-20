namespace PixivApi.Core.Local;

public sealed class ArtworkEnumerable : IEnumerable<Artwork>
{
    private readonly Artwork[] artworkItems;
    private readonly ConcurrentBag<int> bag = new();

    private ArtworkEnumerable(Artwork[] artworkItems)
    {
        this.artworkItems = artworkItems;
    }

    public static async ValueTask<int> CountAsync(ConfigSettings configSettings, DatabaseFile database, ArtworkFilter filter, ParallelOptions parallelOptions)
    {
        if (filter.Count == 0)
        {
            return 0;
        }

        await filter.InitializeAsync(configSettings, database.UserDictionary, database.TagSet, parallelOptions).ConfigureAwait(false);
        var count = 0;
        await Parallel.ForEachAsync(database.Artworks, parallelOptions, (item, token) =>
        {
            if (filter.Filter(item))
            {
                Interlocked.Increment(ref count);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        if (filter.Offset != 0)
        {
            if (count < filter.Offset)
            {
                return 0;
            }

            count -= filter.Offset;
        }

        if (!filter.Count.HasValue)
        {
            return count;
        }

        if (count <= filter.Count.Value)
        {
            return count;
        }
        else
        {
            return filter.Count.Value;
        }
    }

    public static async ValueTask<IEnumerable<Artwork>> CreateAsync(ConfigSettings configSettings, DatabaseFile database, ArtworkFilter filter, ParallelOptions parallelOptions)
    {
        await filter.InitializeAsync(configSettings, database.UserDictionary, database.TagSet, parallelOptions).ConfigureAwait(false);
        ArtworkEnumerable enumerable = new(database.Artworks);
        await Parallel.ForEachAsync(database.Artworks.Select((x, i) => (x, i)), parallelOptions, (pair, token) =>
        {
            var (item, index) = pair;
            if (filter.Filter(item))
            {
                enumerable.bag.Add(index);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        if (filter.IsOrder)
        {
            if (filter.IsLimit)
            {
                return filter.Limit(enumerable.OrderBy(x => x, filter));
            }
            else
            {
                return enumerable.OrderBy(x => x, filter);
            }
        }

        if (filter.IsLimit)
        {
            return filter.Limit(enumerable);
        }
        else
        {
            return enumerable;
        }
    }

    public int Count => bag.Count;

    public Enumerator GetEnumerator() => new(artworkItems, bag.GetEnumerator());

    IEnumerator<Artwork> IEnumerable<Artwork>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public readonly struct Enumerator : IEnumerator<Artwork>
    {
        private readonly Artwork[] artworkItems;
        private readonly IEnumerator<int> enumerator;

        public Enumerator(Artwork[] artworkItems, IEnumerator<int> enumerator)
        {
            this.artworkItems = artworkItems;
            this.enumerator = enumerator;
        }

        public Artwork Current => artworkItems[enumerator.Current];

        object IEnumerator.Current => Current;

        public void Dispose() => enumerator.Dispose();

        public bool MoveNext() => enumerator.MoveNext();

        public void Reset() => enumerator.Reset();
    }
}
