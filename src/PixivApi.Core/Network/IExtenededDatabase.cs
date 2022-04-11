using PixivApi.Core.Local;

namespace PixivApi.Core.Network;

public interface IExtenededDatabase : IDatabase
{
    ValueTask OfficiallyRemoveArtwork(ulong id, CancellationToken token);
    
    ValueTask OfficiallyRemoveUser(ulong id, CancellationToken token);

    /// <returns>True: Add, False: Update</returns>
    ValueTask<bool> ArtworkAddOrUpdateAsync(ArtworkResponseContent source, CancellationToken token);

    async ValueTask<(ulong Add, ulong Update)> ArtworkAddOrUpdateAsync(IEnumerable<ArtworkResponseContent> sources, CancellationToken token)
    {
        var pair = (0UL, 0UL);
        foreach (var source in sources)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (await ArtworkAddOrUpdateAsync(source, token).ConfigureAwait(false))
            {
                pair.Item1++;
            }
            else
            {
                pair.Item2++;
            }
        }

        return pair;
    }

    /// <returns>True: Add, False: Update</returns>
    ValueTask<bool> UserAddOrUpdateAsync(UserDetailResponseData source, CancellationToken token);

    /// <returns>True: Add, False: Update</returns>
    ValueTask<bool> UserAddOrUpdateAsync(UserPreviewResponseContent source, CancellationToken token);

    async ValueTask<(ulong Add, ulong Update)> UserPreviewAddOrUpdateAsync(IEnumerable<UserPreviewResponseContent> sources, CancellationToken token)
    {
        var pair = (0UL, 0UL);
        foreach (var source in sources)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (await UserAddOrUpdateAsync(source, token).ConfigureAwait(false))
            {
                pair.Item1++;
            }
            else
            {
                pair.Item2++;
            }
        }

        return pair;
    }

    ValueTask AddTagToUser(ulong id, uint tagId, CancellationToken token);
}
