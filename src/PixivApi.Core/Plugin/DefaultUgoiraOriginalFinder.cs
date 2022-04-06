using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public sealed record class DefaultUgoiraOriginalFinder(string Folder) : IFinder
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, IServiceProvider provider, CancellationToken cancellationToken) 
        => Task.FromResult<IPlugin?>(new DefaultUgoiraOriginalFinder(configSettings.OriginalFolder));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(ulong id, FileExtensionKind extensionKind) => new(Path.Combine(Folder, IOUtility.GetHashPath(id), ArtworkNameUtility.GetUgoiraOriginalFileName(id, extensionKind)));
}
