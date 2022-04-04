namespace PixivApi.Plugin.JpegXl;

public sealed record class UgoiraThumbnailFinder(ConfigSettings ConfigSettings) : IFinder
{
    public static Task<IPlugin?> CreateAsync(string _, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        return Task.FromResult<IPlugin?>(new UgoiraThumbnailFinder(configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(ulong id, FileExtensionKind extensionKind)
    {
        var folder = Path.Combine(ConfigSettings.ThumbnailFolder, IOUtility.GetHashPath(id));
        var file = new FileInfo(Path.Combine(folder, ArtworkNameUtility.GetUgoiraThumbnailFileName(id)));
        if (file.Exists)
        {
            return file;
        }

        return new(Path.Combine(folder, $"{id}.jxl"));
    }
}
