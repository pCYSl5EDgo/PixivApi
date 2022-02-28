
namespace PixivApi.Plugin.JpegXl;

public sealed record class OriginalConverter(string ExePath, ConfigSettings ConfigSettings) : IConverter
{
    public static Task<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        var exePath = PluginUtility.Find(dllPath, "cjxl");
        return Task.FromResult<IPlugin?>(exePath is null ? null : new OriginalConverter(exePath, configSettings));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string GetJxlName(Artwork artwork) => $"{artwork.Id}.jxl";

    private static string GetJxlName(Artwork artwork, uint index) => $"{artwork.Id}_{index}.jxl";
    
    public async ValueTask<bool> TryConvertAsync(Artwork artwork, ILogger? logger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var folder = Path.Combine(ConfigSettings.OriginalFolder, IOUtility.GetHashPath(artwork.Id));

        if (artwork.Type == ArtworkType.Ugoira)
        {
            var file = Path.Combine(folder, artwork.GetUgoiraOriginalFileName());
            if (!File.Exists(file))
            {
                return false;
            }

            var jxlFile = Path.Combine(folder, GetJxlName(artwork));
            if (File.Exists(jxlFile))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await Utility.ExecuteAsync(logger, ExePath, file, jxlFile).ConfigureAwait(false);
        }
        else
        {
            for (uint i = 0; i < artwork.PageCount; i++)
            {
                var file = Path.Combine(folder, artwork.GetNotUgoiraOriginalFileName(i));
                if (!File.Exists(file))
                {
                    continue;
                }

                var jxlFile = Path.Combine(folder, GetJxlName(artwork, i));
                if (File.Exists(jxlFile))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await Utility.ExecuteAsync(logger, ExePath, file, jxlFile).ConfigureAwait(false);
            }
        }

        return true;
    }
}
