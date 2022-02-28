using PixivApi.Core.Local;

namespace PixivApi.Core;

public sealed record class DefaultUgoiraZipFinder(ConfigSettings ConfigSettings) : IFinder
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken) => Task.FromResult<IPlugin?>(new DefaultUgoiraZipFinder(configSettings));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public bool Find(Artwork artwork) => File.Exists(Path.Combine(ConfigSettings.UgoiraFolder, artwork.GetUgoiraZipFileName()));
}
