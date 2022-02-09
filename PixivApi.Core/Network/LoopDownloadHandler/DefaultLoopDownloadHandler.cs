namespace PixivApi;

public sealed class DefaultLoopDownloadHandler<TContainer, T> : ILoopDownloadHandler<TContainer, T>
    where TContainer : INext, IArrayContainer<T>
{
    public DefaultLoopDownloadHandler()
    {
        list = new();
    }

    private readonly List<T[]> list;

    public IEnumerable<T> Get()
    {
        if (list.Count == 0)
        {
            return Array.Empty<T>();
        }

        if (list.Count == 1)
        {
            return list[0];
        }

        return list.SelectMany(x => x);
    }

    public ValueTask<string?> GetNextUrlAsync(TContainer container, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var array = container.GetContainer();
        if (array.Length != 0)
        {
            list.Add(array);
        }

        return ValueTask.FromResult(array.Length == 0 ? null : container.NextUrl);
    }

    public void Dispose()
    {
        list.Clear();
    }
}
