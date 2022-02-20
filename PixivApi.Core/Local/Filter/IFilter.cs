namespace PixivApi.Core.Local;

public interface IFilter<T>
{
    bool Filter(T item);
}
