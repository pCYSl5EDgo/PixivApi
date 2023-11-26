using PixivApi.Core.Plugin;

namespace PixivApi.Core.Local;

public sealed class FileExistanceFilter
{
    [JsonPropertyName("original"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public InnerFilter? Original;
    [JsonPropertyName("ugoira")] public bool? Ugoira;
    [JsonPropertyName("relation")] public Relation Relationship = new();

    [JsonIgnore] public FinderFacade finder = null!;

    public void Initialize(FinderFacade finder) => this.finder = finder;

    public bool Filter(Artwork artwork)
    {
        if (Original is null)
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
            var originalValue = PrivateFilter(artwork, Original, finder.IllustOriginalFinder, finder.MangaOriginalFinder, finder.UgoiraOriginalFinder);
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
        
        public bool Calc_Ogirinal_Ugoira(bool original, bool ugoira) => IsFirstOperatorAnd ? original & ugoira : original | ugoira;
    }
}

public sealed partial class FileExistanceInnerFilterConverter : JsonConverter<FileExistanceFilter.InnerFilter?>
{
    public static readonly FileExistanceInnerFilterConverter Instance = new();

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

            if (reader.ValueTextEquals("max"u8))
            {
                any = true;
                maxSelected = true;
                if (!reader.Read() || !reader.TryGetInt32(out max))
                {
                    throw new JsonException();
                }
            }
            else if (reader.ValueTextEquals("min"u8))
            {
                any = true;
                if (!reader.Read())
                {
                    throw new JsonException();
                }

                if (reader.TokenType == JsonTokenType.String && reader.ValueTextEquals("all"u8))
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
            writer.WriteNumber("max"u8, value.Max.Value);
        }

        if (value.IsAllMin)
        {
            writer.WriteString("min"u8, "all"u8);
        }
        else if (value.Min != 0)
        {
            writer.WriteNumber("min"u8, value.Min);
        }

        writer.WriteEndObject();
    }
}

public sealed partial class FileExistanceRelationConverter : JsonConverter<FileExistanceFilter.Relation>
{
    public static readonly FileExistanceRelationConverter Instance = new();

    public override FileExistanceFilter.Relation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        while (reader.TokenType == JsonTokenType.Comment)
        {
            reader.Skip();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.ValueTextEquals("&"u8) || reader.ValueTextEquals("and"u8) || reader.ValueTextEquals("o&t&u"u8))
            {
                return new(true, true, 0);
            }

            if (reader.ValueTextEquals("|"u8) || reader.ValueTextEquals("or"u8) || reader.ValueTextEquals("o|t|u"u8))
            {
                return new();
            }

            if (reader.ValueTextEquals("o&t|u"u8))
            {
                return new(true, false, 0);
            }

            if (reader.ValueTextEquals("o|t&u"u8))
            {
                return new(false, true, 0);
            }

            if (reader.ValueTextEquals("o|u&t"u8))
            {
                return new(false, true, 1);
            }

            if (reader.ValueTextEquals("o&u|t"u8))
            {
                return new(true, false, 1);
            }

            if (reader.ValueTextEquals("t&u|o"u8))
            {
                return new(true, false, 2);
            }

            if (reader.ValueTextEquals("t|u&o"u8))
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
                        writer.WriteStringValue("o&t|u"u8);
                    }
                }
                else
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        writer.WriteStringValue("o|t&u"u8);
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
                        writer.WriteStringValue("o&u|t"u8);
                    }
                }
                else
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        writer.WriteStringValue("o|u&t"u8);
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
                        writer.WriteStringValue("t&u|o"u8);
                    }
                }
                else
                {
                    if (value.IsSecondOperatorAnd)
                    {
                        writer.WriteStringValue("t|u&o"u8);
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
        writer.WriteStringValue("and"u8);
        return;
    All_Or:
        writer.WriteStringValue("or"u8);
        return;
    }
}
