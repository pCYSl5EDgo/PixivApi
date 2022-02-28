[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254")]

namespace PixivApi.Plugin.JpegXl;

public sealed record class ImplementationOriginalNotUgoira(string ExePath, ConfigSettings ConfigSettings) : IFinderWithIndex, IConverter
{
    public static ValueTask<IPlugin?> CreateAsync(string dllPath, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<IPlugin?>(cancellationToken);
        }

        var exePath = PluginFindExecutableUtility.Find(dllPath, "cjxl");
        return ValueTask.FromResult<IPlugin?>(exePath is null ? null : new ImplementationOriginalNotUgoira(exePath, configSettings));
    }

    public static bool SupportsMultithread() => false;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string GetJxlName(Artwork artwork, uint index) => $"{artwork.Id}_{index}.jxl";

    public bool Find(Artwork artwork, uint index)
    {
        var folder = Path.Combine(ConfigSettings.OriginalFolder, IOUtility.GetHashPath(artwork.Id));
        var file = Path.Combine(folder, artwork.GetNotUgoiraOriginalFileName(index));
        if (File.Exists(file))
        {
            return true;
        }

        var jxlFile = Path.Combine(folder, GetJxlName(artwork, index));
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

        return true;
    }
}
