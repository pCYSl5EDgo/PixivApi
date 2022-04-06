namespace PixivApi.Plugin.JpegXl;

public sealed record class UgoiraOriginalFinder(ConfigSettings ConfigSettings) : IFinder
{
    public static Task<IPlugin?> CreateAsync(string _, ConfigSettings configSettings, IServiceProvider provider, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        return Task.FromResult<IPlugin?>(new UgoiraOriginalFinder(configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(ulong id, FileExtensionKind extensionKind)
    {
        var folder = Path.Combine(ConfigSettings.OriginalFolder, IOUtility.GetHashPath(id));
        var file = new FileInfo(Path.Combine(folder, ArtworkNameUtility.GetUgoiraOriginalFileName(id, extensionKind)));
        if (file.Exists)
        {
            return file;
        }

        return new(Path.Combine(folder, $"{id}.jxl"));
    }
}
