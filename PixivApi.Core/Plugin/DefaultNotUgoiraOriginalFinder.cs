using PixivApi.Core.Local;

namespace PixivApi.Core;

public sealed record class DefaultNotUgoiraOriginalFinder(ConfigSettings ConfigSettings) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken) => Task.FromResult<IPlugin?>(new DefaultNotUgoiraOriginalFinder(configSettings));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public bool Find(Artwork artwork, uint index) => File.Exists(Path.Combine(ConfigSettings.ThumbnailFolder, artwork.GetNotUgoiraOriginalFileName(index)));
}

public sealed record class DefaultNotUgoiraThumbnailFinder(ConfigSettings ConfigSettings) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken) => Task.FromResult<IPlugin?>(new DefaultNotUgoiraThumbnailFinder(configSettings));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public bool Find(Artwork artwork, uint index) => File.Exists(Path.Combine(ConfigSettings.ThumbnailFolder, artwork.GetNotUgoiraThumbnailFileName(index)));
}
