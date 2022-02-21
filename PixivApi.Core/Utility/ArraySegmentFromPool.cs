namespace PixivApi.Core;

public struct ArraySegmentFromPool : IDisposable
{
    public byte[] Array;
    public int Length;

    public ArraySegmentFromPool(byte[] array, int length)
    {
        Array = array;
        Length = length;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(Array);
        Length = 0;
        Array = System.Array.Empty<byte>();
    }

    public ReadOnlyMemory<byte> AsReadOnlyMemory() => Array.AsMemory(0, Length);
    public ReadOnlySpan<byte> AsReadOnlySpan() => Array.AsSpan(0, Length);

    public Memory<byte> AsMemory() => Array.AsMemory(0, Length);
    public Span<byte> AsSpan() => Array.AsSpan(0, Length);

    public static ArraySegmentFromPool Rent(int sizeHint) => new(ArrayPool<byte>.Shared.Rent(sizeHint), sizeHint);
}
