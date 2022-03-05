using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public sealed record class DefaultNotUgoiraThumbnailFinder(string Folder) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
        => Task.FromResult<IPlugin?>(new DefaultNotUgoiraThumbnailFinder(configSettings.ThumbnailFolder));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(Artwork artwork, uint index) => new(Path.Combine(Folder, IOUtility.GetHashPath(artwork.Id), artwork.GetNotUgoiraThumbnailFileName(index)));
}
