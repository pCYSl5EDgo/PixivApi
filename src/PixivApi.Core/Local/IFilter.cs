namespace PixivApi.Core.Local;

public interface IFilter<T>
{
    bool HasSlowFilter { get; }

    bool FastFilter(IDatabase database, T value);

    ValueTask<bool> SlowFilter(IDatabase database, T value, CancellationToken token);
}
