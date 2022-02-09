namespace PixivApi;

[MessagePackFormatter(typeof(Formatter))]
public struct FilePageExtraInfo
{
    [JsonPropertyName("hide")] public HideReason HideReason;

    public FilePageExtraInfo(HideReason hideReason)
    {
        HideReason = hideReason;
    }

    public sealed class Formatter : IMessagePackFormatter<FilePageExtraInfo>
    {
        public static readonly IMessagePackFormatter<FilePageExtraInfo> Instance = new Formatter();

        public FilePageExtraInfo Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => new((HideReason)reader.ReadInt32());

        public void Serialize(ref MessagePackWriter writer, FilePageExtraInfo value, MessagePackSerializerOptions options) => writer.Write((int)value.HideReason);
    }
}
