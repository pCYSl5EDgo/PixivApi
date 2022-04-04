namespace PixivApi.Core.Local;

public static class ArtworkNameUtility
{
    private static void AddDateToUrl(this DateTime fileDate, ref DefaultInterpolatedStringHandler handler)
    {
        handler.AppendFormatted(fileDate.Year);
        handler.AppendFormatted('/');
        handler.AppendFormatted(fileDate.Month, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(fileDate.Day, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(fileDate.Hour, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(fileDate.Minute, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(fileDate.Second, format: "D2");
    }

    public static string GetNotUgoiraOriginalUrl(this Artwork artwork, uint pageIndex) => GetNotUgoiraOriginalUrl(artwork.Id, artwork.Extension, artwork.FileDate, pageIndex);

    public static string GetNotUgoiraOriginalUrl(ulong id, FileExtensionKind extensionKind, DateTime fileDate, uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-original/img/";
        fileDate.AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddNotUgoiraOriginalFileName(id, extensionKind, ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public static string GetUgoiraOriginalUrl(this Artwork artwork) => GetUgoiraOriginalUrl(artwork.Id, artwork.Extension, artwork.FileDate);

    public static string GetUgoiraOriginalUrl(ulong id, FileExtensionKind extensionKind, DateTime fileDate)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-original/img/";
        fileDate.AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddUgoiraOriginalFileName(id, extensionKind, ref handler);
        return handler.ToStringAndClear();
    }

    public static string GetNotUgoiraThumbnailUrl(this Artwork artwork, uint pageIndex) => GetNotUgoiraThumbnailUrl(artwork.Id, artwork.FileDate, pageIndex);

    public static string GetNotUgoiraThumbnailUrl(ulong id, DateTime fileDate, uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/c/360x360_70/img-master/img/";
        fileDate.AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddNotUgoiraThumbnailFileName(id, ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public static string GetUgoiraThumbnailUrl(this Artwork artwork) => GetUgoiraThumbnailUrl(artwork.Id, artwork.FileDate);

    public static string GetUgoiraThumbnailUrl(ulong id, DateTime fileDate)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/c/360x360_70/img-master/img/";
        fileDate.AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        handler.AppendFormatted(id);
        handler.AppendLiteral("_square1200.jpg");
        return handler.ToStringAndClear();
    }

    public static void AddNotUgoiraOriginalFileName(this Artwork artwork, ref DefaultInterpolatedStringHandler handler, uint pageIndex) => AddNotUgoiraOriginalFileName(artwork.Id, artwork.Extension, ref handler, pageIndex);

    public static void AddNotUgoiraOriginalFileName(ulong id, FileExtensionKind extensionKind, ref DefaultInterpolatedStringHandler handler, uint pageIndex)
    {
        handler.AppendFormatted(id);
        handler.AppendLiteral("_p");
        handler.AppendFormatted(pageIndex);
        handler.AppendLiteral(extensionKind.GetExtensionText());
    }

    public static string GetNotUgoiraOriginalFileName(this Artwork artwork, uint pageIndex) => GetNotUgoiraOriginalFileName(artwork.Id, artwork.Extension, pageIndex);

    public static string GetNotUgoiraOriginalFileName(ulong id, FileExtensionKind extensionKind, uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = new();
        AddNotUgoiraOriginalFileName(id, extensionKind, ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public static void AddUgoiraOriginalFileName(this Artwork artwork, ref DefaultInterpolatedStringHandler handler) => AddUgoiraOriginalFileName(artwork.Id, artwork.Extension, ref handler);

    public static void AddUgoiraOriginalFileName(ulong id, FileExtensionKind extensionKind, ref DefaultInterpolatedStringHandler handler)
    {
        handler.AppendFormatted(id);
        handler.AppendLiteral("_ugoira0");
        handler.AppendLiteral(extensionKind.GetExtensionText());
    }

    public static string GetUgoiraOriginalFileName(this Artwork artwork) => GetUgoiraOriginalFileName(artwork.Id, artwork.Extension);

    public static string GetUgoiraOriginalFileName(ulong id, FileExtensionKind extensionKind)
    {
        DefaultInterpolatedStringHandler handler = new();
        AddUgoiraOriginalFileName(id, extensionKind, ref handler);
        return handler.ToStringAndClear();
    }

    public static void AddNotUgoiraThumbnailFileName(this Artwork artwork, ref DefaultInterpolatedStringHandler handler, uint pageIndex) => AddNotUgoiraThumbnailFileName(artwork.Id, ref handler, pageIndex);

    public static void AddNotUgoiraThumbnailFileName(ulong id, ref DefaultInterpolatedStringHandler handler, uint pageIndex)
    {
        handler.AppendFormatted(id);
        handler.AppendLiteral("_p");
        handler.AppendFormatted(pageIndex);
        handler.AppendLiteral("_square1200.jpg");
    }

    public static string GetUgoiraThumbnailFileName(this Artwork artwork) => GetUgoiraThumbnailFileName(artwork.Id);

    public static string GetUgoiraThumbnailFileName(ulong id) => $"{id}_square1200.jpg";

    public static string GetNotUgoiraThumbnailFileName(this Artwork artwork, uint pageIndex) => GetNotUgoiraThumbnailFileName(artwork.Id, pageIndex);

    public static string GetNotUgoiraThumbnailFileName(ulong id, uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = new();
        AddNotUgoiraThumbnailFileName(id, ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public static string GetUgoiraZipUrl(this Artwork artwork) => GetUgoiraZipUrl(artwork.Id, artwork.FileDate);

    public static string GetUgoiraZipUrl(ulong id, DateTime fileDate)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-zip-ugoira/img/";
        fileDate.AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        handler.AppendFormatted(id);
        handler.AppendLiteral("_ugoira600x600.zip");
        return handler.ToStringAndClear();
    }

    public static string GetUgoiraZipFileName(this Artwork artwork) => GetUgoiraZipFileName(artwork.Id);

    public static string GetUgoiraZipFileName(ulong id) => $"{id}_ugoira600x600.zip";

    public static string GetExtensionText(this FileExtensionKind extensionKind) => extensionKind switch
    {
        FileExtensionKind.Jpg => ".jpg",
        FileExtensionKind.Png => ".png",
        FileExtensionKind.Zip => ".zip",
        FileExtensionKind.None or _ => "",
    };
}
