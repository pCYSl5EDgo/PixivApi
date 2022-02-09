namespace PixivApi;

public sealed class FileExistanceFilter : IFilter<ArtworkDatabaseInfo>
{
    public sealed class OriginalFilter
    {
        [JsonPropertyName("folder")]
        public string Folder = "Original";

        [JsonPropertyName("exist")]
        public bool? Exist;

        public bool Filter(ArtworkDatabaseInfo artwork)
        {
            if (!Exist.HasValue)
            {
                return true;
            }

            bool exist = Exist.Value;
            if (exist)
            {
                if (artwork.PageCount == 0)
                {
                    return false;
                }
            }

            if (artwork.MetaSinglePage.OriginalImageUrl is string single)
            {
                var name = IOUtility.GetFileNameFromUri(single);
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }

                if (File.Exists(Path.Combine(Folder, name)))
                {
                    return exist;
                }
            }
            else
            {
                foreach (var metaPage in artwork.MetaPages)
                {
                    if (metaPage.ImageUrls.Original is string page)
                    {
                        var name = IOUtility.GetFileNameFromUri(page);
                        if (string.IsNullOrEmpty(name))
                        {
                            return false;
                        }

                        if (File.Exists(Path.Combine(Folder, name)))
                        {
                            return exist;
                        }

                        goto RETURN;
                    }
                }
            }

        RETURN:
            return !exist;
        }
    }

    public sealed class ThumbnailFilter
    {
        [JsonPropertyName("folder")]
        public string Folder = "Thumbnail";

        [JsonPropertyName("exist")]
        public bool? Exist;

        public bool Filter(ArtworkDatabaseInfo artwork)
        {
            if (!Exist.HasValue)
            {
                return true;
            }

            bool exist = Exist.Value;
            if (artwork.ImageUrls.SquareMedium is not string single)
            {
                return !exist;
            }
            
            var name = IOUtility.GetFileNameFromUri(single);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (File.Exists(Path.Combine(Folder, name)))
            {
                return exist;
            }

            return !exist;
        }
    }

    [JsonPropertyName("original")]
    public OriginalFilter? Original;

    [JsonPropertyName("thumbnail")]
    public ThumbnailFilter? Thumbnail;

    [JsonPropertyName("or")]
    public bool Or = true;

    private bool Filterable([NotNullWhen(true)] OriginalFilter? filter) => filter is not null && filter.Exist.HasValue;
    private bool Filterable([NotNullWhen(true)] ThumbnailFilter? filter) => filter is not null && filter.Exist.HasValue;

    public bool Filter(ArtworkDatabaseInfo artwork)
    {
        if (Filterable(Original))
        {
            if (!Filterable(Thumbnail))
            {
                return Original.Filter(artwork);
            }

            if (Or)
            {
                return Original.Filter(artwork) || Thumbnail.Filter(artwork);
            }

            return Original.Filter(artwork) && Thumbnail.Filter(artwork);
        }

        if (Filterable(Thumbnail))
        {
            return Thumbnail.Filter(artwork);
        }
        
        return true;
    }
}
