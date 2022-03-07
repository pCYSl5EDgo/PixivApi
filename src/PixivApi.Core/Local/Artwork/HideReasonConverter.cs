namespace PixivApi.Core.Local;

public sealed partial class HideReasonConverter : JsonConverter<HideReason>
{
    public static readonly HideReasonConverter Instance = new();

    [StringLiteral.Utf8("not-hidden")] private static partial ReadOnlySpan<byte> LiteralNotHidden();
    [StringLiteral.Utf8("low-quality")] private static partial ReadOnlySpan<byte> LiteralLowQuality();
    [StringLiteral.Utf8("irrelevant")] private static partial ReadOnlySpan<byte> LiteralIrrelevant();
    [StringLiteral.Utf8("external-link")] private static partial ReadOnlySpan<byte> LiteralExternalLink();
    [StringLiteral.Utf8("dislike")] private static partial ReadOnlySpan<byte> LiteralDislike();
    [StringLiteral.Utf8("crop")] private static partial ReadOnlySpan<byte> LiteralCrop();

    public static bool TryParse(ReadOnlySpan<char> text, out HideReason value)
    {
        if (text.SequenceEqual("not-hidden"))
        {
            value = HideReason.NotHidden;
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
        if (reader.ValueTextEquals(LiteralNotHidden()))
        {
            return HideReason.NotHidden;
        }
        else if (reader.ValueTextEquals(LiteralLowQuality()))
        {
            return HideReason.LowQuality;
        }
        else if (reader.ValueTextEquals(LiteralIrrelevant()))
        {
            return HideReason.Irrelevant;
        }
        else if (reader.ValueTextEquals(LiteralExternalLink()))
        {
            return HideReason.ExternalLink;
        }
        else if (reader.ValueTextEquals(LiteralDislike()))
        {
            return HideReason.Dislike;
        }
        else if (reader.ValueTextEquals(LiteralCrop()))
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
        HideReason.NotHidden => LiteralNotHidden(),
        HideReason.LowQuality => LiteralLowQuality(),
        HideReason.Irrelevant => LiteralIrrelevant(),
        HideReason.ExternalLink => LiteralExternalLink(),
        HideReason.Dislike => LiteralDislike(),
        HideReason.Crop => LiteralCrop(),
        _ => throw new JsonException(),
    });
}
