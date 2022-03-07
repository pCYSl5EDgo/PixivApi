﻿using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class FileExistanceFilter
{
    [JsonPropertyName("original"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public InnerFilter? Original;
    [JsonPropertyName("thumbnail"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public InnerFilter? Thumbnail;
    [JsonPropertyName("ugoira")] public bool? Ugoira;

    private FinderFacade finder = null!;

    public void Initialize(FinderFacade finder) => this.finder = finder;

    public bool Filter(Artwork artwork)
    {
        if (Original is not null && !PrivateFilter(artwork, Original, finder.IllustOriginalFinder, finder.MangaOriginalFinder, finder.UgoiraOriginalFinder))
        {
            return false;
        }

        if (Thumbnail is not null && !PrivateFilter(artwork, Thumbnail, finder.IllustThumbnailFinder, finder.MangaThumbnailFinder, finder.UgoiraThumbnailFinder))
        {
            return false;
        }

        if (artwork.Type == ArtworkType.Ugoira && Ugoira.HasValue && finder.UgoiraZipFinder.Exists(artwork) != Ugoira.Value)
        {
            return false;
        }

        return true;
    }

    private static bool PrivateFilter(Artwork artwork, InnerFilter filter, IFinderWithIndex illustFinder, IFinderWithIndex mangaFinder, IFinder ugoiraFinder) => artwork.Type switch
    {
        ArtworkType.Illust => filter.Filter(artwork, illustFinder),
        ArtworkType.Manga => filter.Filter(artwork, mangaFinder),
        ArtworkType.Ugoira => filter.Filter(artwork, ugoiraFinder),
        _ => false,
    };

    public sealed record class InnerFilter(int? Max, bool IsAllMin, int Min)
    {
        private bool Filter(uint count, uint pageCount)
        {
            if (IsAllMin)
            {
                return count == pageCount;
            }

            if (count < (Min < 0 ? pageCount : 0) + Min)
            {
                return false;
            }

            if (Max is { } max)
            {
                return count <= (max < 0 ? pageCount : 0) + max;
            }

            return true;
        }

        public bool Filter(Artwork artwork, IFinder finder)
        {
            var enumerator = artwork.GetEnumerator();
            if (enumerator.MoveNext())
            {
                return Filter(finder.Exists(artwork) ? 1U : 0U, 1);
            }

            return Filter(0, 0);
        }

        public bool Filter(Artwork artwork, IFinderWithIndex finder)
        {
            uint count = 0, pageCount = 0;
            foreach (var pageIndex in artwork)
            {
                ++pageCount;
                if (finder.Exists(artwork, pageIndex))
                {
                    count++;
                }
            }

            return Filter(count, pageCount);
        }
    }
}

public sealed partial class FileExistanceInnerFilterConverter : JsonConverter<FileExistanceFilter.InnerFilter?>
{
    public static readonly FileExistanceInnerFilterConverter Instance = new();

    [StringLiteral.Utf8("max")] private static partial ReadOnlySpan<byte> LiteralMax();
    [StringLiteral.Utf8("min")] private static partial ReadOnlySpan<byte> LiteralMin();
    [StringLiteral.Utf8("all")] private static partial ReadOnlySpan<byte> LiteralAll();

    public override FileExistanceFilter.InnerFilter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
    LOOP:
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartObject:
                break;
            case JsonTokenType.Comment:
                reader.Read();
                goto LOOP;
            default:
                throw new JsonException();
        }

        var isAllMin = false;
        var maxSelected = false;
        int max = 0, min = 0;
        var any = false;

        while (reader.Read())
        {
            var tokenType = reader.TokenType;
            if (tokenType == JsonTokenType.EndObject)
            {
                return any ? new(maxSelected ? max : null, isAllMin, min) : null;
            }

            if (tokenType == JsonTokenType.Comment)
            {
                continue;
            }

            if (tokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            if (reader.ValueTextEquals(LiteralMax()))
            {
                any = true;
                maxSelected = true;
                if (!reader.Read() || !reader.TryGetInt32(out max))
                {
                    throw new JsonException();
                }
            }
            else if (reader.ValueTextEquals(LiteralMin()))
            {
                any = true;
                if (!reader.Read())
                {
                    throw new JsonException();
                }

                if (reader.TokenType == JsonTokenType.String && reader.ValueTextEquals(LiteralAll()))
                {
                    isAllMin = true;
                }
                else if (reader.TryGetInt32(out min))
                {
                    isAllMin = false;
                }
                else
                {
                    throw new JsonException();
                }
            }
            else
            {
                reader.Skip();
                reader.Skip();
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, FileExistanceFilter.InnerFilter? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        if (value.Max.HasValue)
        {
            writer.WriteNumber(LiteralMax(), value.Max.Value);
        }

        if (value.IsAllMin)
        {
            writer.WriteString(LiteralMin(), LiteralAll());
        }
        else if (value.Min != 0)
        {
            writer.WriteNumber(LiteralMin(), value.Min);
        }

        writer.WriteEndObject();
    }
}