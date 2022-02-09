namespace PixivApi;

[MessagePackFormatter(typeof(Formatter))]
public struct MetaPage
{
    [JsonPropertyName("image_urls")]
    public ImageUrls ImageUrls;

    public MetaPage(ImageUrls imageUrls)
    {
        ImageUrls = imageUrls;
    }

    public sealed class Formatter : IMessagePackFormatter<MetaPage>
    {
        public MetaPage Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var formatter = options.Resolver.GetFormatterWithVerify<ImageUrls>();
            return new(formatter.Deserialize(ref reader, options));
        }

        public void Serialize(ref MessagePackWriter writer, MetaPage value, MessagePackSerializerOptions options)
        {
            var formatter = options.Resolver.GetFormatterWithVerify<ImageUrls>();
            formatter.Serialize(ref writer, value.ImageUrls, options);
        }
    }
}
