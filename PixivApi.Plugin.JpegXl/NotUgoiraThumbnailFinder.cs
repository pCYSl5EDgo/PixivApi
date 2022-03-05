namespace PixivApi.Plugin.JpegXl;

public sealed record class NotUgoiraThumbnailFinder(ConfigSettings ConfigSettings) : IFinderWithIndex
{
    public static Task<IPlugin?> CreateAsync(string _, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        return Task.FromResult<IPlugin?>(new NotUgoiraThumbnailFinder(configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string GetJxlName(Artwork artwork, uint index) => $"{artwork.Id}_{index}.jxl";

    public FileInfo Find(Artwork artwork, uint index)
    {
        var folder = Path.Combine(ConfigSettings.ThumbnailFolder, IOUtility.GetHashPath(artwork.Id));
        var file = new FileInfo(Path.Combine(folder, artwork.GetNotUgoiraThumbnailFileName(index)));
        if (file.Exists)
        {
            return file;
        }

        return new(Path.Combine(folder, GetJxlName(artwork, index)));
    }
}
