using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public sealed record class DefaultNotUgoiraOriginalFinder(string Folder) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
        => Task.FromResult<IPlugin?>(new DefaultNotUgoiraOriginalFinder(configSettings.OriginalFolder));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(Artwork artwork, uint index) => new(Path.Combine(Folder, IOUtility.GetHashPath(artwork.Id), artwork.GetNotUgoiraOriginalFileName(index)));
}
