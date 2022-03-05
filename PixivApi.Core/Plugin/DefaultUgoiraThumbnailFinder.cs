using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public sealed record class DefaultUgoiraThumbnailFinder(string Folder) : IFinder
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
        => Task.FromResult<IPlugin?>(new DefaultUgoiraThumbnailFinder(configSettings.ThumbnailFolder));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(Artwork artwork) => new(Path.Combine(Folder, IOUtility.GetHashPath(artwork.Id), artwork.GetUgoiraThumbnailFileName()));
}
