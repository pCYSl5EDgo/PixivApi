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

#pragma warning disable CS1998
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

        filter.Initialize(database, provider.GetRequiredService<FinderFacade>);
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

        filter.Initialize(database, provider.GetRequiredService<FinderFacade>);
        return filter;
    }
#pragma warning restore CS1998
}
