namespace PixivApi.Core.Local;

public interface IArtworkFilterFactory<T>
{
    ValueTask<ArtworkFilter?> CreateAsync(T source, CancellationToken token);
}
