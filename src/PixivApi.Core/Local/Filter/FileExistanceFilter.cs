using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class FileExistanceFilter
{
    [JsonPropertyName("original"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public InnerFilter? Original;
    [JsonPropertyName("thumbnail"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public InnerFilter? Thumbnail;
    [JsonPropertyName("ugoira")] public bool? Ugoira;
    [JsonPropertyName("relation")] public Relation Relationship = new();

    private FinderFacade finder = null!;

    public void Initialize(FinderFacade finder) => this.finder = finder;

    public bool Filter(Artwork artwork)
    {
        if (Original is null)
        {
            if (Thumbnail is null)
            {
                if (artwork.Type != ArtworkType.Ugoira || Ugoira is null)
                {
                    return true;
                }
                else
                {
                    var ugoiraValue = Ugoira.HasValue && finder.UgoiraZipFinder.Exists(artwork) == Ugoira.Value;
                    return ugoiraValue;
                }
            }
            else
            {
                var thumbnailValue = PrivateFilter(artwork, Thumbnail, finder.IllustThumbnailFinder, finder.MangaThumbnailFinder, finder.UgoiraThumbnailFinder);
                if (artwork.Type != ArtworkType.Ugoira || Ugoira is null)
                {
                    return thumbnailValue;
                }
                else
                {
                    var ugoiraValue = Ugoira.HasValue && finder.UgoiraZipFinder.Exists(artwork) == Ugoira.Value;
                    return Relationship.Calc_Thumbnail_Ugoira(thumbnailValue, ugoiraValue);
                }
            }
        }
        else
        {
            var originalValue = PrivateFilter(artwork, Original, finder.IllustOriginalFinder, finder.MangaOriginalFinder, finder.UgoiraOriginalFinder);
            if (Thumbnail is null)
            {
                if (artwork.Type != ArtworkType.Ugoira || Ugoira is null)
                {
                    return originalValue;
                }
                else
                {
                    var ugoiraValue = Ugoira.HasValue && finder.UgoiraZipFinder.Exists(artwork) == Ugoira.Value;
                    return Relationship.Calc_Ogirinal_Ugoira(originalValue, ugoiraValue);
                }
            }
            else
            {
                var thumbnailValue = PrivateFilter(artwork, Thumbnail, finder.IllustThumbnailFinder, finder.MangaThumbnailFinder, finder.UgoiraThumbnailFinder);
                if (artwork.Type != ArtworkType.Ugoira || Ugoira is null)
                {
                    return Relationship.Calc_Original_Thumbnail(originalValue, thumbnailValue);
                }
                else
                {
                    var ugoiraValue = Ugoira.HasValue && finder.UgoiraZipFinder.Exists(artwork) == Ugoira.Value;
                    return Relationship.Calc_Original_Thumbnail_Ugoira(originalValue, thumbnailValue, ugoiraValue);
                }
            }
        }
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

    public readonly struct Relation
    {
        public Relation()
        {
            IsFirstOperatorAnd = false;
            IsSecondOperatorAnd = false;
            Order = 0;
        }

        public Relation(bool isFirstOperatorAnd, bool isSecondOperatorAnd, int order)
        {
            IsFirstOperatorAnd = isFirstOperatorAnd;
            IsSecondOperatorAnd = isSecondOperatorAnd;
            Order = order;
        }

        public readonly bool IsFirstOperatorAnd;
        public readonly bool IsSecondOperatorAnd;
        public readonly int Order;

        public bool Calc_Original_Thumbnail(bool original, bool thumbnail) => IsFirstOperatorAnd ? original & thumbnail : original | thumbnail;

        public bool Calc_Ogirinal_Ugoira(bool original, bool ugoira) => IsFirstOperatorAnd ? original & ugoira : original | ugoira;

        public bool Calc_Thumbnail_Ugoira(bool thumbnail, bool ugoira) => IsFirstOperatorAnd ? thumbnail & ugoira : thumbnail | ugoira;

        public bool Calc_Original_Thumbnail_Ugoira(bool original, bool thumbnail, bool ugoira)
        {
            bool first_second, third;
            switch (Order)
            {
                case 0:
                    (first_second, third) = (IsFirstOperatorAnd ? original & thumbnail : original | thumbnail, ugoira);
                    break;
                case 1:
                    (first_second, third) = (IsFirstOperatorAnd ? original & ugoira : original | ugoira, thumbnail);
                    break;
                default:
                    (first_second, third) = (IsFirstOperatorAnd ? thumbnail & ugoira : thumbnail | ugoira, original);
                    break;
            }

            return IsSecondOperatorAnd ? first_second & third : first_second | third;
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

public sealed partial class FileExistanceRelationConverter : JsonConverter<FileExistanceFilter.Relation>
{
    public static readonly FileExistanceRelationConverter Instance = new();

    [StringLiteral.Utf8("and")] private static partial ReadOnlySpan<byte> Literal_AndText();
    [StringLiteral.Utf8("or")] private static partial ReadOnlySpan<byte> Literal_OrText();
    [StringLiteral.Utf8("&")] private static partial ReadOnlySpan<byte> Literal_And();
    [StringLiteral.Utf8("|")] private static partial ReadOnlySpan<byte> Literal_Or();
    [StringLiteral.Utf8("o&t&u")] private static partial ReadOnlySpan<byte> Literal_OaTaU();
    [StringLiteral.Utf8("o|t|u")] private static partial ReadOnlySpan<byte> Literal_OoToU();
    [StringLiteral.Utf8("o&t|u")] private static partial ReadOnlySpan<byte> Literal_OaToU();
    [StringLiteral.Utf8("o|t&u")] private static partial ReadOnlySpan<byte> Literal_OoTaU();
    [StringLiteral.Utf8("o|u&t")] private static partial ReadOnlySpan<byte> Literal_OoUaT();
    [StringLiteral.Utf8("o&u|t")] private static partial ReadOnlySpan<byte> Literal_OaUoT();
    [StringLiteral.Utf8("t&u|o")] private static partial ReadOnlySpan<byte> Literal_TaUoO();
    [StringLiteral.Utf8("t|u&o")] private static partial ReadOnlySpan<byte> Literal_ToUaO();

    public override FileExistanceFilter.Relation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        while (reader.TokenType == JsonTokenType.Comment)
        {
            reader.Skip();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.ValueTextEquals(Literal_And()) || reader.ValueTextEquals(Literal_AndText()) || reader.ValueTextEquals(Literal_OaTaU()))
            {
                return new(true, true, 0);
            }

            if (reader.ValueTextEquals(Literal_Or()) || reader.ValueTextEquals(Literal_OrText()) || reader.ValueTextEquals(Literal_OoToU()))
            {
                return new();
            }

            if (reader.ValueTextEquals(Literal_OaToU()))
            {
                return new(true, false, 0);
            }

            if (reader.ValueTextEquals(Literal_OoTaU()))
            {
                return new(false, true, 0);
            }

            if (reader.ValueTextEquals(Literal_OoUaT()))
            {
                return new(false, true, 1);
            }

            if (reader.ValueTextEquals(Literal_OaUoT()))
            {
                return new(true, false, 1);
            }

            if (reader.ValueTextEquals(Literal_TaUoO()))
            {
                return new(true, false, 2);
            }

            if (reader.ValueTextEquals(Literal_ToUaO()))
            {
                return new(false, true, 2);
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, FileExistanceFilter.Relation value, JsonSerializerOptions options)
    {
        switch (value.Order)
        {
            case 0:
                if (value.IsFirstOperatorAnd)
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        goto All_And;
                    }
                    else
                    {
                        writer.WriteStringValue(Literal_OaToU());
                    }
                }
                else
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        writer.WriteStringValue(Literal_OoTaU());
                    }
                    else
                    {
                        goto All_Or;
                    }
                }
                break;
            case 1:
                if (value.IsFirstOperatorAnd)
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        goto All_And;
                    }
                    else
                    {
                        writer.WriteStringValue(Literal_OaUoT());
                    }
                }
                else
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        writer.WriteStringValue(Literal_OoUaT());
                    }
                    else
                    {
                        goto All_Or;
                    }
                }
                break;
            default:
                if (value.IsFirstOperatorAnd)
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        goto All_And;
                    }
                    else
                    {
                        writer.WriteStringValue(Literal_TaUoO());
                    }
                }
                else
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        writer.WriteStringValue(Literal_ToUaO());
                    }
                    else
                    {
                        goto All_Or;
                    }
                }
                break;
        }

        return;
    All_And:
        writer.WriteStringValue(Literal_AndText());
        return;
    All_Or:
        writer.WriteStringValue(Literal_OrText());
        return;
    }
}
