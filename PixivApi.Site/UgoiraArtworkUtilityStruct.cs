using Cysharp.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixivApi.Site;

public static class FileUriUtility
{
    public static void Convert(ref Utf8ValueStringBuilder builder, ReadOnlySpan<char> path)
    {
        builder.Clear();
        builder.AppendLiteral(LiteralUtility.LiteralQuoteFile());
        var enumerator = path.EnumerateRunes();
        while (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            switch (c.Value)
            {
                case '\\':
                    builder.GetSpan(1)[0] = (byte)'/';
                    builder.Advance(1);
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.AppendLiteral(LiteralUtility.LiteralQuote());
    }
}

public readonly struct UgoiraArtworkUtilityStruct
{
    private readonly Artwork artwork;
    private readonly IFinder? thumbnailFinder;
    private readonly IFinder? originalFinder;
    private readonly IFinder? ugoiraFinder;

    public UgoiraArtworkUtilityStruct(Artwork artwork, IFinder? ugoiraFinder, IFinder? thumbnailFinder, IFinder? originalFinder)
    {
        this.artwork = artwork;
        this.thumbnailFinder = thumbnailFinder;
        this.originalFinder = originalFinder;
        this.ugoiraFinder = ugoiraFinder;
    }

    public sealed class Converter : JsonConverter<UgoiraArtworkUtilityStruct>
    {
        public static readonly Converter Instance = new();

        public override UgoiraArtworkUtilityStruct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, UgoiraArtworkUtilityStruct value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(LiteralUtility.LiteralArtwork());
            var artwork = value.artwork;
            Artwork.Converter.Instance.Write(writer, artwork, options);
            var utf8builder = ZString.CreateUtf8StringBuilder();
            try
            {
                if (value.ugoiraFinder is not null)
                {
                    writer.WriteStartArray(LiteralUtility.LiteralUgoira());
                    PrivateWrite(writer, ref utf8builder, artwork, value.ugoiraFinder);
                    writer.WriteEndArray();
                }

                if (value.thumbnailFinder is not null)
                {
                    writer.WriteStartArray(LiteralUtility.LiteralThumbnail());
                    PrivateWrite(writer, ref utf8builder, artwork, value.thumbnailFinder);
                    writer.WriteEndArray();
                }

                if (value.originalFinder is not null)
                {
                    writer.WriteStartArray(LiteralUtility.LiteralOriginal());
                    PrivateWrite(writer, ref utf8builder, artwork, value.originalFinder);
                    writer.WriteEndArray();
                }
            }
            finally
            {
                utf8builder.Dispose();
            }

            writer.WriteEndObject();
        }

        private static void PrivateWrite(Utf8JsonWriter writer, ref Utf8ValueStringBuilder utf8builder, Artwork artwork, IFinder finder)
        {
            if (!artwork.IsNotHided(0))
            {
                goto NULL;
            }

            var info = finder.Find(artwork);
            if (!info.Exists)
            {
                goto NULL;
            }

            FileUriUtility.Convert(ref utf8builder, info.FullName);
            writer.WriteRawValue(utf8builder.AsSpan(), true);
            return;

        NULL:
            writer.WriteNullValue();
        }
    }
}
