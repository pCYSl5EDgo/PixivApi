using Microsoft.Extensions.DependencyInjection;
using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class FileArtworkFilterFactory : IArtworkFilterFactory<FileInfo>
{
    private readonly IDatabaseFactory factory;
    private readonly IServiceProvider provider;

    public FileArtworkFilterFactory(IDatabaseFactory factory, IServiceProvider provider)
    {
        this.factory = factory;
        this.provider = provider;
    }

    public async ValueTask<ArtworkFilter?> CreateAsync(FileInfo source, CancellationToken token)
    {
        if (!source.Exists || source.Length == 0)
        {
            return null;
        }

        var filter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(source.FullName, token).ConfigureAwait(false);
        if (filter is null)
        {
            return null;
        }

        var database = await factory.CreateAsync(token).ConfigureAwait(false);
        await filter.InitializeAsync(database, provider.GetRequiredService<FinderFacade>, token).ConfigureAwait(false);
        return filter;
    }
}
