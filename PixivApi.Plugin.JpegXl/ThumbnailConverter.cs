namespace PixivApi.Plugin.JpegXl;

public sealed record class ThumbnailConverter(string ExePath, ConfigSettings ConfigSettings) : IConverter
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        var exePath = PluginUtility.Find(dllPath, "cjxl");
        return Task.FromResult<IPlugin?>(exePath is null ? null : new ThumbnailConverter(exePath, configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string GetJxlName(Artwork artwork) => $"{artwork.Id}.jxl";

    private static string GetJxlName(Artwork artwork, uint index) => $"{artwork.Id}_{index}.jxl";

    public async ValueTask<bool> TryConvertAsync(Artwork artwork, ILogger? logger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var folder = Path.Combine(ConfigSettings.ThumbnailFolder, IOUtility.GetHashPath(artwork.Id));
        var anyProcess = false;
        if (artwork.Type == ArtworkType.Ugoira)
        {
            var fileName = artwork.GetUgoiraThumbnailFileName();
            var fileInfo = new FileInfo(Path.Combine(folder, fileName));
            if (!fileInfo.Exists)
            {
                return false;
            }

            var jxlName = GetJxlName(artwork);
            if (File.Exists(Path.Combine(folder, jxlName)))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            anyProcess = await ConverterUtility.ExecuteAsync(logger, ExePath, fileName, fileInfo.Length, jxlName, folder).ConfigureAwait(false);
        }
        else
        {
            for (uint i = 0; i < artwork.PageCount; i++)
            {
                var fileName = artwork.GetNotUgoiraThumbnailFileName(i);
                var fileInfo = new FileInfo(Path.Combine(folder, fileName));
                if (!fileInfo.Exists)
                {
                    continue;
                }

                var jxlName = GetJxlName(artwork, i);
                if (File.Exists(Path.Combine(folder, jxlName)))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                anyProcess |= await ConverterUtility.ExecuteAsync(logger, ExePath, fileName, fileInfo.Length, jxlName, folder).ConfigureAwait(false);
            }
        }

        return anyProcess;
    }

    public void DeleteUnneccessaryOriginal(Artwork artwork, ILogger? logger)
    {
        var folder = Path.Combine(ConfigSettings.ThumbnailFolder, IOUtility.GetHashPath(artwork.Id));
        if (artwork.Type == ArtworkType.Ugoira)
        {
            var fileName = artwork.GetUgoiraThumbnailFileName();
            var fileInfo = new FileInfo(Path.Combine(folder, fileName));
            if (!fileInfo.Exists)
            {
                return;
            }

            var jxlName = GetJxlName(artwork);
            if (!File.Exists(Path.Combine(folder, jxlName)))
            {
                return;
            }

            logger?.LogInformation($"Delete: {fileName}");
            fileInfo.Delete();
        }
        else
        {
            for (uint i = 0; i < artwork.PageCount; i++)
            {
                var fileName = artwork.GetNotUgoiraThumbnailFileName(i);
                var fileInfo = new FileInfo(Path.Combine(folder, fileName));
                if (!fileInfo.Exists)
                {
                    continue;
                }

                var jxlName = GetJxlName(artwork, i);
                if (!File.Exists(Path.Combine(folder, jxlName)))
                {
                    continue;
                }

                logger?.LogInformation($"Delete: {fileName}");
                fileInfo.Delete();
            }
        }
    }
}
