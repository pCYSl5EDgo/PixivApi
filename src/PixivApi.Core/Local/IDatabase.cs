namespace PixivApi.Core.Local;

public interface IDatabase<T> where T : class
{
    ValueTask<T?> GetAsync(ulong id, CancellationToken token);

    /// <summary>
    /// Get or Add by id.
    /// </summary>
    /// <returns>true: Get, false: Add</returns>
    ValueTask<bool> GetOrAddAsync<TArg>(ulong id, Func<TArg, T> addFunc, TArg arg, CancellationToken token);

    /// <summary>
    /// Add or Update by id.
    /// </summary>
    /// <returns>true: Add, false Update</returns>
    ValueTask<bool> AddOrUpdateAsync<TArg>(ulong id, Func<TArg, T> addFunc, Action<T, TArg> updateFunc, TArg arg, CancellationToken token);

    ValueTask<IEnumerable<T>> EnumerateAsync(IFilter<T> filter, CancellationToken token);
}

public interface IFilter<T>
{
    bool HasSlowFilter { get; }

    bool FastFilter(T value);

    bool SlowFilter(T value);

    bool CompleteFilter(T value) => FastFilter(value) && (!HasSlowFilter || SlowFilter(value));
}
