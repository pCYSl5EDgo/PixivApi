namespace PixivApi;

public sealed class FileExistanceFilter : IFilter<ArtworkDatabaseInfo>
{
    public sealed class OriginalFilter
    {
        [JsonPropertyName("folder")]
        public string Folder = "Original";

        [JsonPropertyName("exist")]
        public bool Exist;

        public bool Filter(string url)
        {
            var name = IOUtility.GetFileNameFromUri(url);
            return !string.IsNullOrEmpty(name) && (File.Exists(Path.Combine(Folder, name)) ? Exist : !Exist);
        }

        public bool Filter(ArtworkDatabaseInfo artwork)
        {
            if (Exist)
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
                    return Exist;
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
                            return Exist;
                        }

                        goto RETURN;
                    }
                }
            }

        RETURN:
            return !Exist;
        }

        public bool Filter(MetaPage[] metaPages)
        {
            foreach (var metaPage in metaPages)
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
                        return Exist;
                    }

                    goto RETURN;
                }
            }

        RETURN:
            return !Exist;
        }
    }

    public sealed class ThumbnailFilter
    {
        [JsonPropertyName("folder")]
        public string Folder = "Thumbnail";

        [JsonPropertyName("exist")]
        public bool Exist;

        public bool Filter(string? url)
        {
            if (url is null)
            {
                return !Exist;
            }

            var name = IOUtility.GetFileNameFromUri(url);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return File.Exists(Path.Combine(Folder, name)) ? Exist : !Exist;
        }

        public bool Filter(ArtworkDatabaseInfo artwork) => Filter(artwork.ImageUrls.SquareMedium);
    }

    [JsonPropertyName("original")]
    public OriginalFilter? Original;

    [JsonPropertyName("thumbnail")]
    public ThumbnailFilter? Thumbnail;

    private static bool Filterable([NotNullWhen(true)] OriginalFilter? filter) => filter is not null;
    private static bool Filterable([NotNullWhen(true)] ThumbnailFilter? filter) => filter is not null;

    public bool Filter(ArtworkDatabaseInfo artwork)
    {
        if (Filterable(Original))
        {
            if (!Filterable(Thumbnail))
            {
                return Original.Filter(artwork);
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
