namespace PixivApi;

[MessagePackFormatter(typeof(Formatter))]
public struct MetaSinglePage
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonPropertyName("original_image_url")]
    public string? OriginalImageUrl;

    public MetaSinglePage(string? originalImageUrl)
    {
        OriginalImageUrl = originalImageUrl;
    }

    public sealed class Formatter : IMessagePackFormatter<MetaSinglePage>
    {
        public MetaSinglePage Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => new(reader.ReadString());

        public void Serialize(ref MessagePackWriter writer, MetaSinglePage value, MessagePackSerializerOptions options) => writer.Write(value.OriginalImageUrl);
    }
}
