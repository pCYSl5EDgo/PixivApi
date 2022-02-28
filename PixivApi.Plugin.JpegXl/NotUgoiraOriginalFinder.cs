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
}
