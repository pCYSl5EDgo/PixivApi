namespace PixivApi.Core.Local;

public interface IArtworkDatabase
{
    ValueTask<ulong> CountArtworkAsync(CancellationToken token);
    
    ValueTask<ulong> CountArtworkAsync(IFilter<Artwork> filter, CancellationToken token);

    ValueTask<Artwork?> GetArtworkAsync(ulong id, CancellationToken token);

    ValueTask<Artwork> GetOrAddAsync(ulong id, DatabaseAddArtworkFunc add, CancellationToken token);

    ValueTask AddOrUpdateAsync(ulong id, DatabaseAddArtworkFunc add, DatabaseUpdateArtworkFunc update, CancellationToken token);

    /// <summary>
    /// Filter only FastFilter
    /// </summary>
    ValueTask<IEnumerable<Artwork>> FastFilterAsync(IFilter<Artwork> filter, CancellationToken token);
    
    /// <summary>
    /// Filter both SlowFilter and FastFilter
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    IAsyncEnumerable<Artwork> FilterAsync(IFilter<Artwork> filter, CancellationToken token);
    
    /// <summary>
    /// Enumerate everything
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    IAsyncEnumerable<Artwork> EnumerableArtworkAsync(CancellationToken token);
}
