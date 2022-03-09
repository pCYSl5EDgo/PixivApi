namespace PixivApi.Core.Local;

public interface IArtworkDatabase
{
    ValueTask<ulong> CountArtworkAsync(CancellationToken token);
    
    ValueTask<ulong> CountArtworkAsync(IFilter<Artwork> filter, CancellationToken token);

    ValueTask<Artwork?> GetArtworkAsync(ulong id, CancellationToken token);

    ValueTask<Artwork> GetOrAddAsync(ulong id, DatabaseAddArtworkFunc add, CancellationToken token);

    ValueTask AddOrUpdateAsync(ulong id, DatabaseAddArtworkFunc add, DatabaseUpdateArtworkFunc update, CancellationToken token);

    IAsyncEnumerable<Artwork> FilterAsync(IFilter<Artwork> filter, CancellationToken token);

    IAsyncEnumerable<Artwork> EnumerableArtworkAsync(CancellationToken token);
}
