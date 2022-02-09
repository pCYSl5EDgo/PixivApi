namespace PixivApi;

public interface IMapper<T>
{
    object? Map(IEnumerable<T> enumerable);
}
