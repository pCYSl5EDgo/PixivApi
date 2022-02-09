using System.Collections;
using System.Collections.Concurrent;

namespace PixivApi;

public sealed class ArtworkDatabaseInfoEnumerable : IEnumerable<ArtworkDatabaseInfo>
{
    private readonly ArtworkDatabaseInfo[] artworkItems;
    private readonly ArtworkDatabaseInfoFilter filter;
    private readonly ConcurrentBag<int> bag = new();

    private ArtworkDatabaseInfoEnumerable(ArtworkDatabaseInfo[] artworkItems, ArtworkDatabaseInfoFilter filter)
    {
        this.artworkItems = artworkItems;
        this.filter = filter;
    }

    public static async ValueTask<int> CountAsync(ArtworkDatabaseInfo[] artworkItems, ArtworkDatabaseInfoFilter filter, CancellationToken cancellationToken)
    {
        if (filter.Count == 0)
        {
            return 0;
        }

        int count = 0;
        await Parallel.ForEachAsync(artworkItems, cancellationToken, (item, token) =>
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

    public static async ValueTask<IEnumerable<ArtworkDatabaseInfo>> CreateAsync(ArtworkDatabaseInfo[] artworkItems, ArtworkDatabaseInfoFilter filter, CancellationToken cancellationToken)
    {
        ArtworkDatabaseInfoEnumerable enumerable = new(artworkItems, filter);
        await Parallel.ForEachAsync(artworkItems.Select((x, i) => (x, i)), cancellationToken, (pair, token) =>
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

    IEnumerator<ArtworkDatabaseInfo> IEnumerable<ArtworkDatabaseInfo>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<ArtworkDatabaseInfo>
    {
        private ArtworkDatabaseInfo[] artworkItems;
        private IEnumerator<int> enumerator;

        public Enumerator(ArtworkDatabaseInfo[] artworkItems, IEnumerator<int> enumerator)
        {
            this.artworkItems = artworkItems;
            this.enumerator = enumerator;
        }

        public ArtworkDatabaseInfo Current => artworkItems[enumerator.Current];

        object IEnumerator.Current => throw new NotImplementedException();

        public void Dispose() => enumerator.Dispose();

        public bool MoveNext() => enumerator.MoveNext();

        public void Reset() => enumerator.Reset();
    }
}
