namespace PixivApi;

public sealed class MergeLoopDownloadHandler<TContainer, T> : IMergeLoopDownloadHandler<TContainer, T>
    where TContainer : INext, IArrayContainer<T>
    where T : IOverwrite<T>
{
    private HashSet<T>? set;

    public void Initialize(IEnumerable<T>? enumerable)
    {
        set = new(enumerable ?? Array.Empty<T>());
    }

    public IEnumerable<T> Get() => set ?? Enumerable.Empty<T>();

    public ValueTask<string?> GetNextUrlAsync(TContainer container, CancellationToken token)
    {
        Debug.Assert(set is not null);
        token.ThrowIfCancellationRequested();
        bool allContained = true;
        foreach (var item in container.GetContainer())
        {
            if (set.TryGetValue(item, out var actual))
            {
                actual.Overwrite(item);
            }
            else
            {
                allContained = false;
                set.Add(item);
            }
        }

        return ValueTask.FromResult(allContained ? null : container.NextUrl);
    }

    public void Dispose()
    {
        set = null;
    }
}
