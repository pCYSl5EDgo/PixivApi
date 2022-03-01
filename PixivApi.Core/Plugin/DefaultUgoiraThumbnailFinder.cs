using PixivApi.Core.Local;

namespace PixivApi.Core;

public sealed record class DefaultUgoiraThumbnailFinder(ConfigSettings ConfigSettings) : IFinder
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken) => Task.FromResult<IPlugin?>(new DefaultUgoiraThumbnailFinder(configSettings));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(Artwork artwork) => new(Path.Combine(ConfigSettings.ThumbnailFolder, artwork.GetUgoiraThumbnailFileName()));
}
