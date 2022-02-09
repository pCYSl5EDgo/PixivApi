namespace PixivApi;

public interface IFilter<T>
{
    bool Filter(T item);
}
