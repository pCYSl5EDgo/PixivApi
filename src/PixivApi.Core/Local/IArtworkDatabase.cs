namespace PixivApi.Core.Local;

public interface IArtworkDatabase
{
  ValueTask<ulong> CountArtworkAsync(CancellationToken token);

  ValueTask<ulong> CountArtworkAsync(ArtworkFilter filter, CancellationToken token);

  ValueTask<Artwork?> GetArtworkAsync(ulong id, CancellationToken token);

  /// <returns>True: Add, False: Update</returns>
  ValueTask<bool> AddOrUpdateAsync(ulong id, DatabaseAddArtworkFunc add, DatabaseUpdateArtworkFunc update, CancellationToken token);

  /// <returns>True: Add, False: Update</returns>
  ValueTask<bool> AddOrUpdateAsync(Artwork artwork, CancellationToken token);

  IAsyncEnumerable<Artwork> FilterAsync(ArtworkFilter filter, CancellationToken token);

  /// <summary>
  /// Enumerate everything
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  IAsyncEnumerable<Artwork> EnumerateArtworkAsync(CancellationToken token);

  IAsyncEnumerable<bool> AddOrUpdateAsync(IEnumerable<Artwork> collection, CancellationToken token);
}
