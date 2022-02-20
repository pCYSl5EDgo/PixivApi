namespace PixivApi.Core.Local.Filter;

public sealed class FileExistanceFilter : IFilter<Artwork>
{
    public sealed class OriginalFilter : IFilter<Artwork>
    {
        [JsonPropertyName("folder")]
        public string Folder = "Original";

        [JsonPropertyName("exist")]
        public bool Exist;

        public bool Filter(Artwork artwork)
        {
            var folder = $"{Folder}/{IOUtility.GetHashPath(artwork.Id)}/";
            if (Exist)
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

    public sealed class ThumbnailFilter : IFilter<Artwork>
    {
        [JsonPropertyName("folder")]
        public string Folder = "Thumbnail";

        [JsonPropertyName("exist")]
        public bool Exist;

        public bool Filter(Artwork artwork)
        {
            DefaultInterpolatedStringHandler handler = $"{Folder}/";
            IOUtility.AppendHashPath(ref handler, artwork.Id);
            handler.AppendLiteral("/");
            artwork.AddThumbnailFileName(ref handler);
            return File.Exists(handler.ToStringAndClear()) == Exist;
        }
    }

    public sealed class UgoiraFilter : IFilter<Artwork>
    {
        [JsonPropertyName("folder")]
        public string Folder = "Ugoira";

        [JsonPropertyName("exist")]
        public bool Exist;

        public bool Filter(Artwork artwork)
        {
            DefaultInterpolatedStringHandler handler = $"{Folder}/";
            IOUtility.AppendHashPath(ref handler, artwork.Id);
            handler.AppendLiteral("/");
            handler.AppendFormatted(artwork.GetZipFileName());
            return File.Exists(handler.ToStringAndClear()) == Exist;
        }
    }

    [JsonPropertyName("original")]
    public OriginalFilter? Original;

    [JsonPropertyName("thumbnail")]
    public ThumbnailFilter? Thumbnail;

    [JsonPropertyName("ugoira")]
    public UgoiraFilter? Ugoira;

    public bool Filter(Artwork artwork)
    {
        if (Original is not null && !Original.Filter(artwork))
        {
            return false;
        }

        if (Thumbnail is not null && !Thumbnail.Filter(artwork))
        {
            return false;
        }

        if (artwork.Type == ArtworkType.Ugoira && Ugoira is not null && !Ugoira.Filter(artwork))
        {
            return false;
        }

        return true;
    }
}
