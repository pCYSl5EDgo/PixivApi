namespace PixivApi.Core.Local;

public sealed class FileExistanceFilter
{
    [JsonPropertyName("original")] public FileExistanceType Original = FileExistanceType.None;
    [JsonPropertyName("thumbnail")] public FileExistanceType Thumbnail = FileExistanceType.None;
    [JsonPropertyName("ugoira")] public UgoiraFileExistanceFilter? Ugoira = null;

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

        if (artwork.Type == ArtworkType.Ugoira && Ugoira is not null && !Ugoira.Filter(artwork, ugoiraFolder))
        {
            return false;
        }

        return true;
    }

    private static bool ShouldDismiss(Artwork artwork, uint i) => (artwork.ExtraHideLast && i == artwork.PageCount - 1) || (artwork.ExtraPageHideReasonDictionary is { Count: > 0 } hideDictionary && hideDictionary.TryGetValue(i, out var reason) && reason != HideReason.NotHidden);

    private bool ThumbnailFilter(Artwork artwork, FileExistanceType existanceType)
    {
        var folder = $"{thumbnailFolder}{IOUtility.GetHashPath(artwork.Id)}";

        switch (existanceType)
        {
            case FileExistanceType.ExistAll:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

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

public sealed class UgoiraFileExistanceFilter
{
    [JsonPropertyName("zip")] public bool? ExistZip;
    [JsonPropertyName("codecs")] public Dictionary<string, bool>? CodecExistDictionary;

    public bool Filter(Artwork artwork, string folder)
    {
        if (ExistZip.HasValue && ExistZip.Value != File.Exists(Path.Combine(folder, $"{IOUtility.GetHashPath(artwork.Id)}{artwork.GetZipFileName()}")))
        {
            return false;
        }

        if (CodecExistDictionary is { Count: > 0 })
        {
            var withoutExtension = Path.Combine(folder, $"{IOUtility.GetHashPath(artwork.Id)}{artwork.GetZipFileNameWithoutExtension()}");
            foreach (var (key, exist) in CodecExistDictionary)
            {
                if (!UgoiraCodecExtensions.TryParse(key, out var codec))
                {
                    continue;
                }

                if (File.Exists(withoutExtension + codec.GetExtension()) != exist)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
