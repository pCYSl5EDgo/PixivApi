using System.Buffers;

namespace PixivApi.Core.SqliteDatabase;

public struct ArtworkArrayKey : IEquatable<ArtworkArrayKey>, IComparable<ArtworkArrayKey>, IDisposable
{
    public int ArtworkCount;
    public (int TagCount, int ToolCount)[] Collection;

    public ArtworkArrayKey(int artworkCount)
    {
        ArtworkCount = artworkCount;
        Collection = ArrayPool<(int, int)>.Shared.Rent(ArtworkCount);
    }

    public void Sort<T>(Span<T> array) => Collection.AsSpan(0, ArtworkCount).Sort(array[0..ArtworkCount]);

    public void Dispose()
    {
        ArtworkCount = 0;
        if (Collection is not null)
        {
            ArrayPool<(int, int)>.Shared.Return(Collection);
            Collection = null!;
        }
    }

    public readonly bool Equals(ArtworkArrayKey other) => Collection.AsSpan(0, ArtworkCount).SequenceEqual(other.Collection.AsSpan(0, other.ArtworkCount));

    public readonly int CompareTo(ArtworkArrayKey other)
    {
        var c = ArtworkCount.CompareTo(other.ArtworkCount);
        if (c != 0)
        {
            return c;
        }

        return Collection.AsSpan(0, ArtworkCount).SequenceCompareTo(other.Collection.AsSpan(0, other.ArtworkCount));
    }

    public static bool operator ==(ArtworkArrayKey left, ArtworkArrayKey right) => left.Equals(right);
    public static bool operator !=(ArtworkArrayKey left, ArtworkArrayKey right) => !(left == right);
    public static bool operator <(ArtworkArrayKey left, ArtworkArrayKey right) => left.CompareTo(right) < 0;
    public static bool operator <=(ArtworkArrayKey left, ArtworkArrayKey right) => left.CompareTo(right) <= 0;
    public static bool operator >(ArtworkArrayKey left, ArtworkArrayKey right) => left.CompareTo(right) > 0;
    public static bool operator >=(ArtworkArrayKey left, ArtworkArrayKey right) => left.CompareTo(right) >= 0;

    public override readonly bool Equals(object? obj) => obj is ArtworkArrayKey other && Equals(other);

    public override readonly int GetHashCode() => ArtworkCount;
}
