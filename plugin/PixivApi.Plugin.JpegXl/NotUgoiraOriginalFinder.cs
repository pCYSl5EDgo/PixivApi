[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254")]

namespace PixivApi.Plugin.JpegXl;

public sealed record class NotUgoiraOriginalFinder(ConfigSettings ConfigSettings) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string _, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        return Task.FromResult<IPlugin?>(new NotUgoiraOriginalFinder(configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public FileInfo Find(ulong id, FileExtensionKind extensionKind, uint index)
    {
        var folder = Path.Combine(ConfigSettings.OriginalFolder, IOUtility.GetHashPath(id));
        var file = new FileInfo(Path.Combine(folder, ArtworkNameUtility.GetNotUgoiraOriginalFileName(id, extensionKind, index)));
        if (file.Exists)
        {
            return file;
        }

        return new(Path.Combine(folder, $"{id}_{index}.jxl"));
    }
}
