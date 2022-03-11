using Microsoft.Extensions.DependencyInjection;
using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class ContentArtworkFilterFactory : IArtworkFilterFactory<ReadOnlyMemory<byte>>, IArtworkFilterFactory<ReadOnlyMemory<char>>
{
    private readonly IServiceProvider provider;

    public ContentArtworkFilterFactory(IServiceProvider provider)
    {
        this.provider = provider;
    }

    public async ValueTask<ArtworkFilter?> CreateAsync(IDatabase database, ReadOnlyMemory<byte> source, CancellationToken token)
    {
        if (source.Length == 0)
        {
            return null;
        }

        var filter = IOUtility.JsonDeserialize<ArtworkFilter>(source.Span);
        if (filter is null)
        {
            return null;
        }

        await filter.InitializeAsync(database, provider.GetRequiredService<FinderFacade>, token).ConfigureAwait(false);
        return filter;
    }

    public async ValueTask<ArtworkFilter?> CreateAsync(IDatabase database, ReadOnlyMemory<char> source, CancellationToken token)
    {
        if (source.Length == 0)
        {
            return null;
        }

        var filter = IOUtility.JsonDeserialize<ArtworkFilter>(source.Span);
        if (filter is null)
        {
            return null;
        }

        await filter.InitializeAsync(database, provider.GetRequiredService<FinderFacade>, token).ConfigureAwait(false);
        return filter;
    }
}
