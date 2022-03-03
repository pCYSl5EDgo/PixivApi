using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class FileExistanceFilter
{
    [JsonPropertyName("original")] public FileExistanceType Original = FileExistanceType.None;
    [JsonPropertyName("thumbnail")] public FileExistanceType Thumbnail = FileExistanceType.None;
    [JsonPropertyName("ugoira")] public bool? Ugoira = null;

    private FinderFacade finder = null!;

    public void Initialize(FinderFacade finder) => this.finder = finder;

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

        if (artwork.Type == ArtworkType.Ugoira && Ugoira.HasValue && finder.UgoiraZipFinder.Exists(artwork) != Ugoira.Value)
        {
            return false;
        }

        return true;
    }

    private static bool ShouldDismiss(Artwork artwork, uint i) => (artwork.ExtraHideLast && i == artwork.PageCount - 1) || (artwork.ExtraPageHideReasonDictionary is { Count: > 0 } hideDictionary && hideDictionary.TryGetValue(i, out var reason) && reason != HideReason.NotHidden);

    private bool ThumbnailFilter(Artwork artwork, FileExistanceType existanceType)
    {
        var finder = artwork.Type == ArtworkType.Illust ? this.finder.IllustThumbnailFinder : this.finder.MangaThumbnailFinder;
        switch (existanceType)
        {
            case FileExistanceType.ExistAll:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

                    if (!finder.Exists(artwork, i))
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

                    if (finder.Exists(artwork, i))
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

                    if (!finder.Exists(artwork, i))
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

                    if (finder.Exists(artwork, i))
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
        var finder = artwork.Type == ArtworkType.Illust ? this.finder.IllustOriginalFinder : this.finder.MangaOriginalFinder;
        switch (existanceType)
        {
            case FileExistanceType.ExistAll:
                for (uint i = 0; i < artwork.PageCount; i++)
                {
                    if (ShouldDismiss(artwork, i))
                    {
                        continue;
                    }

                    if (!finder.Exists(artwork, i))
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

                    if (finder.Exists(artwork, i))
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

                    if (!finder.Exists(artwork, i))
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

                    if (finder.Exists(artwork, i))
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
