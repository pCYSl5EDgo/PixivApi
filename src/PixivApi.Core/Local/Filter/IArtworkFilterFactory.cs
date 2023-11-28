namespace PixivApi.Core.Local;

public interface IArtworkFilterFactory<T>
{
  ValueTask<ArtworkFilter?> CreateAsync(IDatabase database, T source, CancellationToken token);
}
