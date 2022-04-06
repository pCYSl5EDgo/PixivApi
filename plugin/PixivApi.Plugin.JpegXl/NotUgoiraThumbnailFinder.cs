namespace PixivApi.Plugin.JpegXl;

public sealed record class NotUgoiraThumbnailFinder(ConfigSettings ConfigSettings) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string _, ConfigSettings configSettings, IServiceProvider provider, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        return Task.FromResult<IPlugin?>(new NotUgoiraThumbnailFinder(configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(ulong id, FileExtensionKind extensionKind, uint index)
    {
        var folder = Path.Combine(ConfigSettings.ThumbnailFolder, IOUtility.GetHashPath(id));
        var file = new FileInfo(Path.Combine(folder, ArtworkNameUtility.GetNotUgoiraThumbnailFileName(id, index)));
        if (file.Exists)
        {
            return file;
        }

        return new(Path.Combine(folder, $"{id}_{index}.jxl"));
    }
}
