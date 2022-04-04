using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public sealed record class DefaultNotUgoiraThumbnailFinder(string Folder) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
        => Task.FromResult<IPlugin?>(new DefaultNotUgoiraThumbnailFinder(configSettings.ThumbnailFolder));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(ulong id, FileExtensionKind extensionKind, uint index) => new(Path.Combine(Folder, IOUtility.GetHashPath(id), ArtworkNameUtility.GetNotUgoiraThumbnailFileName(id, index)));
}
