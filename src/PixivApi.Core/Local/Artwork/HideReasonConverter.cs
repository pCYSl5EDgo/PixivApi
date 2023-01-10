namespace PixivApi.Core.Local;

public sealed partial class HideReasonConverter : JsonConverter<HideReason>
{
    public static readonly HideReasonConverter Instance = new();

    public static bool TryParse(ReadOnlySpan<char> text, out HideReason value)
    {
        if (text.SequenceEqual("not-hidden"))
        {
            value = HideReason.NotHidden;
            return true;
        }

        if (text.SequenceEqual("temporary-hidden"))
        {
            value = HideReason.TemporaryHidden;
            return true;
        }

        if (text.SequenceEqual("low-quality"))
        {
            value = HideReason.LowQuality;
            return true;
        }

        if (text.SequenceEqual("irrelevant"))
        {
            value = HideReason.Irrelevant;
            return true;
        }

        if (text.SequenceEqual("external-link"))
        {
            value = HideReason.ExternalLink;
            return true;
        }

        if (text.SequenceEqual("dislike"))
        {
            value = HideReason.Dislike;
            return true;
        }

        if (text.SequenceEqual("crop"))
        {
            value = HideReason.Crop;
            return true;
        }

        Unsafe.SkipInit(out value);
        return false;
    }

    public override HideReason Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.ValueTextEquals("not-hidden"u8))
        {
            return HideReason.NotHidden;
        }
        else if (reader.ValueTextEquals("temporary-hidden"u8))
        {
            return HideReason.TemporaryHidden;
        }
        else if (reader.ValueTextEquals("low-quality"u8))
        {
            return HideReason.LowQuality;
        }
        else if (reader.ValueTextEquals("irrelevant"u8))
        {
            return HideReason.Irrelevant;
        }
        else if (reader.ValueTextEquals("external-link"u8))
        {
            return HideReason.ExternalLink;
        }
        else if (reader.ValueTextEquals("dislike"u8))
        {
            return HideReason.Dislike;
        }
        else if (reader.ValueTextEquals("crop"u8))
        {
            return HideReason.Crop;
        }
        else
        {
            throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, HideReason value, JsonSerializerOptions options) => writer.WriteStringValue(value switch
    {
        HideReason.NotHidden => "not-hidden"u8,
        HideReason.TemporaryHidden => "temporary-hidden"u8,
        HideReason.LowQuality => "low-quality"u8,
        HideReason.Irrelevant => "irrelevant"u8,
        HideReason.ExternalLink => "external-link"u8,
        HideReason.Dislike => "dislike"u8,
        HideReason.Crop => "crop"u8,
        _ => throw new JsonException(),
    });
}
