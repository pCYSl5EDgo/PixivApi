namespace PixivApi.Plugin.JpegXl;

public sealed record class UgoiraOriginalFinder(ConfigSettings ConfigSettings) : IFinder
{
    public static Task<IPlugin?> CreateAsync(string _, ConfigSettings configSettings, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IPlugin?>(cancellationToken);
        }

        return Task.FromResult<IPlugin?>(new UgoiraOriginalFinder(configSettings));
    }

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
}
