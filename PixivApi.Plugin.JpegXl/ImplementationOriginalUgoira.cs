namespace PixivApi.Plugin.JpegXl;

public sealed record class ImplementationOriginalUgoira(string ExePath, ConfigSettings ConfigSettings) : IFinder, IConverter
{
    public static ValueTask<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<IPlugin?>(cancellationToken);
        }

        var exePath = PluginFindExecutableUtility.Find(dllPath, "cjxl");
        return ValueTask.FromResult<IPlugin?>(exePath is null ? null : new ImplementationOriginalUgoira(exePath, configSettings));
    }

    public static bool SupportsMultithread() => false;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string GetJxlName(Artwork artwork) => $"{artwork.Id}.jxl";

    public bool Find(Artwork artwork)
    {
        var folder = Path.Combine(ConfigSettings.OriginalFolder, IOUtility.GetHashPath(artwork.Id));
        var file = Path.Combine(folder, artwork.GetUgoiraOriginalFileName());
        if (File.Exists(file))
        {
            return true;
        }

        var jxlFile = Path.Combine(folder, GetJxlName(artwork));
        if (File.Exists(jxlFile))
        {
            return true;
        }

        return false;
    }

    public async ValueTask<bool> TryConvertAsync(Artwork artwork, ILogger? logger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var folder = Path.Combine(ConfigSettings.OriginalFolder, IOUtility.GetHashPath(artwork.Id));
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
        return true;
    }
}
