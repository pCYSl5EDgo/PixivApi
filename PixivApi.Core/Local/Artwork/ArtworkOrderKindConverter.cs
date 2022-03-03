namespace PixivApi.Core.Local;

public sealed partial class ArtworkOrderKindConverter : JsonConverter<ArtworkOrderKind>
{
    public static readonly ArtworkOrderKindConverter Instance = new();

    public override ArtworkOrderKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArtworkOrderKind kind;
        if (reader.ValueTextEquals(LiteralNone()[1..^1])) { kind = ArtworkOrderKind.None; }
        else if (reader.ValueTextEquals(LiteralId()[1..^1])) { kind = ArtworkOrderKind.Id; }
        else if (reader.ValueTextEquals(LiteralReverseId()[1..^1])) { kind = ArtworkOrderKind.ReverseId; }
        else if (reader.ValueTextEquals(LiteralView()[1..^1])) { kind = ArtworkOrderKind.View; }
        else if (reader.ValueTextEquals(LiteralReverseView()[1..^1])) { kind = ArtworkOrderKind.ReverseView; }
        else if (reader.ValueTextEquals(LiteralBookmarks()[1..^1])) { kind = ArtworkOrderKind.Bookmarks; }
        else if (reader.ValueTextEquals(LiteralReverseBookmarks()[1..^1])) { kind = ArtworkOrderKind.ReverseBookmarks; }
        else if (reader.ValueTextEquals(LiteralUserId()[1..^1])) { kind = ArtworkOrderKind.UserId; }
        else if (reader.ValueTextEquals(LiteralReverseUserId()[1..^1])) { kind = ArtworkOrderKind.ReverseUserId; }
        else { throw new JsonException(nameof(ArtworkOrderKind)); }
        reader.Skip();
        return kind;
    }

    [StringLiteral.Utf8("\"none\"")] private static partial ReadOnlySpan<byte> LiteralNone();
    [StringLiteral.Utf8("\"id\"")] private static partial ReadOnlySpan<byte> LiteralId();
    [StringLiteral.Utf8("\"reverse-id\"")] private static partial ReadOnlySpan<byte> LiteralReverseId();
    [StringLiteral.Utf8("\"view\"")] private static partial ReadOnlySpan<byte> LiteralView();
    [StringLiteral.Utf8("\"reverse-view\"")] private static partial ReadOnlySpan<byte> LiteralReverseView();
    [StringLiteral.Utf8("\"bookmarks\"")] private static partial ReadOnlySpan<byte> LiteralBookmarks();
    [StringLiteral.Utf8("\"reverse-bookmarks\"")] private static partial ReadOnlySpan<byte> LiteralReverseBookmarks();
    [StringLiteral.Utf8("\"user\"")] private static partial ReadOnlySpan<byte> LiteralUserId();
    [StringLiteral.Utf8("\"reverse-user\"")] private static partial ReadOnlySpan<byte> LiteralReverseUserId();

    public override void Write(Utf8JsonWriter writer, ArtworkOrderKind value, JsonSerializerOptions options) 
    {
        switch (value)
        {
            case ArtworkOrderKind.Id: writer.WriteRawValue(LiteralId(), false); break;
            case ArtworkOrderKind.ReverseId: writer.WriteRawValue(LiteralReverseId(), false); break;
            case ArtworkOrderKind.View: writer.WriteRawValue(LiteralView(), false); break;
            case ArtworkOrderKind.ReverseView: writer.WriteRawValue(LiteralReverseView(), false); break;
            case ArtworkOrderKind.Bookmarks: writer.WriteRawValue(LiteralBookmarks(), false); break;
            case ArtworkOrderKind.ReverseBookmarks: writer.WriteRawValue(LiteralReverseBookmarks(), false); break;
            case ArtworkOrderKind.UserId: writer.WriteRawValue(LiteralUserId(), false); break;
            case ArtworkOrderKind.ReverseUserId: writer.WriteRawValue(LiteralReverseUserId(), false); break;
            case ArtworkOrderKind.None:
            default:
                writer.WriteRawValue(LiteralNone(), false);
                break;
        }
    }
}