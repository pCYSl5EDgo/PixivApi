using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public sealed record class DefaultNotUgoiraThumbnailFinder(ConfigSettings ConfigSettings) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken) => Task.FromResult<IPlugin?>(new DefaultNotUgoiraThumbnailFinder(configSettings));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(Artwork artwork, uint index) => new(Path.Combine(ConfigSettings.ThumbnailFolder, artwork.GetNotUgoiraThumbnailFileName(index)));
}
