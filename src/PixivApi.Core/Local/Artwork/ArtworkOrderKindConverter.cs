namespace PixivApi.Core.Local;

public sealed partial class ArtworkOrderKindConverter : JsonConverter<ArtworkOrderKind>
{
    public static readonly ArtworkOrderKindConverter Instance = new();

    public override ArtworkOrderKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArtworkOrderKind kind;
        if (reader.ValueTextEquals("none"u8)) { kind = ArtworkOrderKind.None; }
        else if (reader.ValueTextEquals("id"u8)) { kind = ArtworkOrderKind.Id; }
        else if (reader.ValueTextEquals("reverse-id"u8)) { kind = ArtworkOrderKind.ReverseId; }
        else if (reader.ValueTextEquals("view"u8)) { kind = ArtworkOrderKind.View; }
        else if (reader.ValueTextEquals("reverse-view"u8)) { kind = ArtworkOrderKind.ReverseView; }
        else if (reader.ValueTextEquals("bookmarks"u8)) { kind = ArtworkOrderKind.Bookmarks; }
        else if (reader.ValueTextEquals("reverse-bookmarks"u8)) { kind = ArtworkOrderKind.ReverseBookmarks; }
        else if (reader.ValueTextEquals("user"u8)) { kind = ArtworkOrderKind.UserId; }
        else if (reader.ValueTextEquals("reverse-user"u8)) { kind = ArtworkOrderKind.ReverseUserId; }
        else { throw new JsonException(nameof(ArtworkOrderKind)); }
        reader.Skip();
        return kind;
    }

    public override void Write(Utf8JsonWriter writer, ArtworkOrderKind value, JsonSerializerOptions options) 
    {
        switch (value)
        {
            case ArtworkOrderKind.Id: writer.WriteRawValue("\"id\""u8, false); break;
            case ArtworkOrderKind.ReverseId: writer.WriteRawValue("\"reverse-id\""u8, false); break;
            case ArtworkOrderKind.View: writer.WriteRawValue("\"view\""u8, false); break;
            case ArtworkOrderKind.ReverseView: writer.WriteRawValue("\"reverse-view\""u8, false); break;
            case ArtworkOrderKind.Bookmarks: writer.WriteRawValue("\"bookmarks\""u8, false); break;
            case ArtworkOrderKind.ReverseBookmarks: writer.WriteRawValue("\"reverse-bookmarks\""u8, false); break;
            case ArtworkOrderKind.UserId: writer.WriteRawValue("\"user\""u8, false); break;
            case ArtworkOrderKind.ReverseUserId: writer.WriteRawValue("\"reverse-user\""u8, false); break;
            case ArtworkOrderKind.None:
            default:
                writer.WriteRawValue("\"none\""u8, false);
                break;
        }
    }
}