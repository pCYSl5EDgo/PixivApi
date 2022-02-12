using System.Collections;

namespace PixivApi;

public sealed class SequenceFromPool : IDisposable, IReadOnlyList<Memory<byte>>
{
    public SequenceFromPool(long length, int slicePower2)
    {
        var eachLength = 1 << slicePower2;
        var count = (int)(length >> slicePower2);
        var rest = (int)(length - (((long)count) << slicePower2));
        if (rest == 0)
        {
            segments = ArraySegmentFromPool<SegmentFromPool>.Rent(count);
            var span = segments.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = new(eachLength, ((long)i) << slicePower2);
            }

            for (int i = 1; i < span.Length; i++)
            {
                span[i - 1].SetNext(span[i]);
            }
        }
        else
        {
            segments = ArraySegmentFromPool<SegmentFromPool>.Rent(count + 1);
            var span = segments.AsSpan()[..count];
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = new(eachLength, ((long)i) << slicePower2);
            }

            for (int i = 1; i < span.Length; i++)
            {
                span[i - 1].SetNext(span[i]);
            }

            span[^1].SetNext(segments.AsSpan()[^1] = new(rest, length - rest));
        }

        this.slicePower2 = slicePower2;
    }

    private readonly ArraySegmentFromPool<SegmentFromPool> segments;
    private readonly int slicePower2;

    public Memory<byte> this[int index] => segments.Array[index].Segment.AsMemory();

    public int Count => segments.Length;

    public ReadOnlySequence<byte> AsSequence()
    {
        var span = segments.AsSpan();
        var last = span[^1];
        return new(span[0], 0, last, endIndex: last.Segment.Length);
    }

    public void Dispose()
    {
        foreach (ref var segment in segments.AsSpan())
        {
            segment.Dispose();
            segment = null!;
        }

        segments.Dispose();
    }

    public struct Enumerator : IEnumerator<Memory<byte>>
    {
        private readonly SegmentFromPool[] segments;
        private readonly int count;
        private int index;

        public Enumerator(ArraySegmentFromPool<SegmentFromPool> segments)
        {
            this.segments = segments.Array;
            count = segments.Length;
            index = -1;
        }

        public Memory<byte> Current => segments[index].Segment.AsMemory();

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext() => ++index < count;

        public void Reset() => index = -1;
    }

    public Enumerator GetEnumerator() => new(segments);

    IEnumerator<Memory<byte>> IEnumerable<Memory<byte>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public sealed class SegmentFromPool : ReadOnlySequenceSegment<byte>, IDisposable
    {
        public readonly ArraySegmentFromPool Segment;

        public SegmentFromPool(int length, long runningIndex)
        {
            Segment = ArraySegmentFromPool.Rent(length);
            RunningIndex = runningIndex;
            Memory = Segment.AsMemory();
            Next = null;
        }

        internal void SetNext(SegmentFromPool next)
        {
            Next = next;
        }

        public void Dispose()
        {
            Segment.Dispose();
            Next = null;
            Memory = ReadOnlyMemory<byte>.Empty;
        }
    }
}
