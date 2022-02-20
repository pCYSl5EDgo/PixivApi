namespace PixivApi.Core.Local.Filter;

public interface IFilter<T>
{
    bool Filter(T item);
}
