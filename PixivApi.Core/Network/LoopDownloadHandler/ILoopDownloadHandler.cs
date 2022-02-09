namespace PixivApi;

public interface IMergeLoopDownloadHandler<TContainer, T> : ILoopDownloadHandler<TContainer, T>
    where TContainer : INext, IArrayContainer<T>
{
    public void Initialize(IEnumerable<T>? enumerable);
}

public interface ILoopDownloadHandler<TContainer, T> : IDisposable
    where TContainer : INext, IArrayContainer<T>
{
    ValueTask<string?> GetNextUrlAsync(TContainer container, CancellationToken token);
    IEnumerable<T> Get();
}
