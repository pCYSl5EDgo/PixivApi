using Cysharp.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixivApi.Site;

public readonly partial struct NotUgoiraArtworkUtilityStruct
{
    private readonly Artwork artwork;
    private readonly IFinderWithIndex? thumbnailFinder;
    private readonly IFinderWithIndex? originalFinder;

    public NotUgoiraArtworkUtilityStruct(Artwork artwork, IFinderWithIndex? thumbnailFinder, IFinderWithIndex? originalFinder)
    {
        this.artwork = artwork;
        this.thumbnailFinder = thumbnailFinder;
        this.originalFinder = originalFinder;
    }

    public sealed class Converter : JsonConverter<NotUgoiraArtworkUtilityStruct>
    {
        public static readonly Converter Instance = new();

        public override NotUgoiraArtworkUtilityStruct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, NotUgoiraArtworkUtilityStruct value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(LiteralUtility.LiteralArtwork());
            var artwork = value.artwork;
            Artwork.Converter.Instance.Write(writer, artwork, options);
            var utf8builder = ZString.CreateUtf8StringBuilder();
            try
            {
                if (value.thumbnailFinder is not null)
                {
                    writer.WriteStartArray(LiteralUtility.LiteralThumbnail());
                    for (uint pageIndex = 0, pageCount = artwork.PageCount; pageIndex < pageCount; pageIndex++)
                    {
                        PrivateWrite(writer, ref utf8builder, artwork, pageIndex, value.thumbnailFinder);
                    }

                    writer.WriteEndArray();
                }

                if (value.originalFinder is not null)
                {
                    writer.WriteStartArray(LiteralUtility.LiteralOriginal());
                    for (uint pageIndex = 0, pageCount = artwork.PageCount; pageIndex < pageCount; pageIndex++)
                    {
                        PrivateWrite(writer, ref utf8builder, artwork, pageIndex, value.originalFinder);
                    }

                    writer.WriteEndArray();
                }
            }
            finally
            {
                utf8builder.Dispose();
            }

            writer.WriteEndObject();
        }

        private static void PrivateWrite(Utf8JsonWriter writer, ref Utf8ValueStringBuilder builder, Artwork artwork, uint pageIndex, IFinderWithIndex finder)
        {
            if (!artwork.IsNotHided(pageIndex))
            {
                goto NULL;
            }

            var info = finder.Find(artwork, pageIndex);
            if (!info.Exists)
            {
                goto NULL;
            }

            FileUriUtility.Convert(ref builder, info.Name);
            writer.WriteRawValue(builder.AsSpan(), true);
            return;

        NULL:
            writer.WriteNullValue();
        }
    }
}
