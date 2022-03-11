using PixivApi.Core.Local;

namespace PixivApi.Core.Network;

public interface IExtenededDatabase : IDatabase
{
    /// <returns>True: Add, False: Update</returns>
    ValueTask<bool> AddOrUpdateAsync(ArtworkResponseContent source, CancellationToken token);

    async ValueTask<(ulong, ulong)> AddOrUpdateAsync(IEnumerable<ArtworkResponseContent> sources, CancellationToken token)
    {
        var pair = (0UL, 0UL);
        foreach (var source in sources)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (await AddOrUpdateAsync(source, token).ConfigureAwait(false))
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
    ValueTask<bool> AddOrUpdateAsync(UserDetailResponseData source, CancellationToken token);

    /// <returns>True: Add, False: Update</returns>
    ValueTask<bool> AddOrUpdateAsync(UserPreviewResponseContent source, CancellationToken token);

    async ValueTask<(ulong, ulong)> AddOrUpdateAsync(IEnumerable<UserPreviewResponseContent> sources, CancellationToken token)
    {
        var pair = (0UL, 0UL);
        foreach (var source in sources)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (await AddOrUpdateAsync(source, token).ConfigureAwait(false))
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
}
