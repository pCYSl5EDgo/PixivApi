namespace PixivApi.Core.Local;

public sealed class FileExistanceFilter
{
    [JsonPropertyName("original")] public FileExistanceType Original = FileExistanceType.None;
    [JsonPropertyName("thumbnail")] public FileExistanceType Thumbnail = FileExistanceType.None;
    [JsonPropertyName("ugoira")] public FileExistanceType Ugoira = FileExistanceType.None;

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
        if (Original != FileExistanceType.None && !OriginalFilter(artwork, Original))
        {
            return false;
        }

        if (Thumbnail != FileExistanceType.None && !ThumbnailFilter(artwork, Thumbnail))
        {
            return false;
        }

        if (artwork.Type == ArtworkType.Ugoira && Ugoira != FileExistanceType.None && !UgoiraFilter(artwork, Ugoira))
        {
            return false;
        }

        return true;
    }

    private bool UgoiraFilter(Artwork artwork, FileExistanceType existanceType)
    {
        var zip = $"{ugoiraFolder}{IOUtility.GetHashPath(artwork.Id)}{artwork.GetZipFileName()}";
        var webm = $"{ugoiraFolder}{IOUtility.GetHashPath(artwork.Id)}{artwork.GetZipFileNameWithoutExtension()}.webm";
        switch (existanceType)
        {
            case FileExistanceType.ExistAll:
                return File.Exists(zip) && File.Exists(webm);
            case FileExistanceType.ExistAny:
                return File.Exists(zip) || File.Exists(webm);
            case FileExistanceType.NotExistAll:
                return !File.Exists(zip) || !File.Exists(webm);
            case FileExistanceType.NotExistAny:
                return !File.Exists(zip) && !File.Exists(webm);
            default:
                throw new ArgumentOutOfRangeException(nameof(existanceType));
        }
    }

    private bool ThumbnailFilter(Artwork artwork, FileExistanceType existanceType)
    {
        var folder = $"{thumbnailFolder}{IOUtility.GetHashPath(artwork.Id)}";
        switch (existanceType)
        {
            case FileExistanceType.ExistAll:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddThumbnailFileName(ref handler, i);
                    if (!File.Exists(handler.ToStringAndClear()))
                    {
                        return false;
                    }
                }

                return true;
            case FileExistanceType.ExistAny:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddThumbnailFileName(ref handler, i);
                    if (File.Exists(handler.ToStringAndClear()))
                    {
                        return true;
                    }
                }

                return false;
            case FileExistanceType.NotExistAll:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddThumbnailFileName(ref handler, i);
                    if (!File.Exists(handler.ToStringAndClear()))
                    {
                        return true;
                    }
                }

                return false;
            case FileExistanceType.NotExistAny:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddThumbnailFileName(ref handler, i);
                    if (File.Exists(handler.ToStringAndClear()))
                    {
                        return false;
                    }
                }

                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(existanceType));
        }

    }

    public bool OriginalFilter(Artwork artwork, FileExistanceType existanceType)
    {
        var folder = $"{originalFolder}{IOUtility.GetHashPath(artwork.Id)}";
        switch (existanceType)
        {
            case FileExistanceType.ExistAll:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddOriginalFileName(ref handler, i);
                    if (!File.Exists(handler.ToStringAndClear()))
                    {
                        return false;
                    }
                }

                return true;
            case FileExistanceType.ExistAny:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddOriginalFileName(ref handler, i);
                    if (File.Exists(handler.ToStringAndClear()))
                    {
                        return true;
                    }
                }

                return false;
            case FileExistanceType.NotExistAll:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddOriginalFileName(ref handler, i);
                    if (!File.Exists(handler.ToStringAndClear()))
                    {
                        return true;
                    }
                }

                return false;
            case FileExistanceType.NotExistAny:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    DefaultInterpolatedStringHandler handler = $"{folder}";
                    artwork.AddOriginalFileName(ref handler, i);
                    if (File.Exists(handler.ToStringAndClear()))
                    {
                        return false;
                    }
                }

                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(existanceType));
        }
    }
}

public enum FileExistanceType
{
    None,
    ExistAll,
    ExistAny,
    NotExistAll,
    NotExistAny,
}

public sealed partial class FileExistanceTypeFormatter : JsonConverter<FileExistanceType>
{
    public static readonly FileExistanceTypeFormatter Instance = new();

    [StringLiteral.Utf8("exist-all")] private static partial ReadOnlySpan<byte> LiteralExistAll();
    [StringLiteral.Utf8("exist-any")] private static partial ReadOnlySpan<byte> LiteralExistAny();
    [StringLiteral.Utf8("not-exist-all")] private static partial ReadOnlySpan<byte> LiteralNotExistAll();
    [StringLiteral.Utf8("not-exist-any")] private static partial ReadOnlySpan<byte> LiteralNotExistAny();

    public override FileExistanceType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.ValueTextEquals(LiteralExistAll()))
        {
            return FileExistanceType.ExistAll;
        }
        else if (reader.ValueTextEquals(LiteralExistAny()))
        {
            return FileExistanceType.ExistAny;
        }
        else if (reader.ValueTextEquals(LiteralNotExistAll()))
        {
            return FileExistanceType.NotExistAll;
        }
        else if (reader.ValueTextEquals(LiteralNotExistAny()))
        {
            return FileExistanceType.NotExistAny;
        }
        else
        {
            return FileExistanceType.None;
        }
    }

    public override void Write(Utf8JsonWriter writer, FileExistanceType value, JsonSerializerOptions options) => throw new NotSupportedException();
}