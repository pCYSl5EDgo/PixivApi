namespace PixivApi.Core.Local;

public sealed class FileExistanceFilter : IFilter<Artwork>
{
    [JsonPropertyName("original")]
    public bool? Original;

    [JsonPropertyName("thumbnail")]
    public bool? Thumbnail;

    [JsonPropertyName("ugoira")]
    public bool? Ugoira;

    private string originalFolder = "Original";

    private string thumbnailFolder = "Thumbnail";

    private string ugoiraFolder = "Ugoira";

    public void Initialize(string originalFolder, string thumbnailFolder, string ugoiraFolder)
    {
        static string WithSeparator(string path)
        {
            if (path.Length > 0 && path[^1] != Path.DirectorySeparatorChar && path[^1] != Path.AltDirectorySeparatorChar)
            {
                return path + '/';
            }

            return path;
        }

        this.originalFolder = WithSeparator(originalFolder);
        this.thumbnailFolder = WithSeparator(thumbnailFolder);
        this.ugoiraFolder = WithSeparator(ugoiraFolder);
    }

    public bool Filter(Artwork artwork)
    {
        if (Original.HasValue && !OriginalFilter(artwork, Original.Value))
        {
            return false;
        }

        if (Thumbnail.HasValue && !ThumbnailFilter(artwork, Thumbnail.Value))
        {
            return false;
        }

        if (artwork.Type == ArtworkType.Ugoira && Ugoira.HasValue && !UgoiraFilter(artwork, Ugoira.Value))
        {
            return false;
        }

        return true;
    }

    private bool UgoiraFilter(Artwork artwork, bool exist)
    {
        DefaultInterpolatedStringHandler handler = $"{ugoiraFolder}";
        IOUtility.AppendHashPath(ref handler, artwork.Id);
        handler.AppendFormatted(artwork.GetZipFileName());
        return File.Exists(handler.ToStringAndClear()) == exist;
    }

    private bool ThumbnailFilter(Artwork artwork, bool exist)
    {
        DefaultInterpolatedStringHandler handler = $"{thumbnailFolder}";
        IOUtility.AppendHashPath(ref handler, artwork.Id);
        artwork.AddThumbnailFileName(ref handler);
        return File.Exists(handler.ToStringAndClear()) == exist;
    }

    public bool OriginalFilter(Artwork artwork, bool exist)
    {
        var folder = $"{originalFolder}{IOUtility.GetHashPath(artwork.Id)}";
        if (exist)
        {
            for (uint i = 0; i < artwork.PageCount; i++)
            {
                DefaultInterpolatedStringHandler handler = $"{folder}";
                artwork.AddOriginalFileName(i, ref handler);
                if (!File.Exists(handler.ToStringAndClear()))
                {
                    return false;
                }
            }
        }
        else
        {
            for (uint i = 0; i < artwork.PageCount; i++)
            {
                DefaultInterpolatedStringHandler handler = $"{folder}";
                artwork.AddOriginalFileName(i, ref handler);
                if (File.Exists(handler.ToStringAndClear()))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
