using PixivApi.Core.Local;

namespace PixivApi.Core.Plugin;

public sealed record class DefaultUgoiraZipFinder(string Folder) : IFinder
{
  public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, IServiceProvider provider, CancellationToken cancellationToken)
      => Task.FromResult<IPlugin?>(new DefaultUgoiraZipFinder(configSettings.UgoiraFolder));

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  public FileInfo Find(ulong id, FileExtensionKind extensionKind) => new(Path.Combine(Folder, IOUtility.GetHashPath(id), ArtworkNameUtility.GetUgoiraZipFileName(id)));
}
