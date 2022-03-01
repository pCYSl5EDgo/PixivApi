using PixivApi.Core.Local;

namespace PixivApi.Core;

public sealed record class DefaultNotUgoiraOriginalFinder(ConfigSettings ConfigSettings) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken) => Task.FromResult<IPlugin?>(new DefaultNotUgoiraOriginalFinder(configSettings));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(Artwork artwork, uint index) => new(Path.Combine(ConfigSettings.ThumbnailFolder, artwork.GetNotUgoiraOriginalFileName(index)));
}
