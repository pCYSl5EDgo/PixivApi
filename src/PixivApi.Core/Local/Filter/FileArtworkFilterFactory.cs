using Microsoft.Extensions.DependencyInjection;
using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class FileArtworkFilterFactory : IArtworkFilterFactory<FileInfo>
{
  private readonly IServiceProvider provider;

  public FileArtworkFilterFactory(IServiceProvider provider)
  {
    this.provider = provider;
  }

  public async ValueTask<ArtworkFilter?> CreateAsync(IDatabase database, FileInfo source, CancellationToken token)
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

    filter.Initialize(database, provider.GetRequiredService<FinderFacade>);
    return filter;
  }
}
