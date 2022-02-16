namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
public sealed class DatabaseFile
{
    [Key(0x00)] public Artwork[] Artworks;
    [Key(0x01)] public ConcurrentDictionary<ulong, User> UserDictionary;
    [Key(0x02)] public StringSet TagSet;
    [Key(0x03)] public StringSet ToolSet;

    public DatabaseFile()
    {
        Artworks = Array.Empty<Artwork>();
        UserDictionary = new();
        TagSet = new(4096);
        ToolSet = new(256);
    }

    public DatabaseFile(Artwork[] artworks, ConcurrentDictionary<ulong, User> userDictionary, StringSet tagSet, StringSet toolSet)
    {
        Artworks = artworks;
        UserDictionary = userDictionary;
        TagSet = tagSet;
        ToolSet = toolSet;
    }

    public sealed class Formatter : IMessagePackFormatter<DatabaseFile?>
    {
        public void Serialize(ref MessagePackWriter writer, DatabaseFile? value, MessagePackSerializerOptions options) => SerializeStatic(ref writer, value, options);

        public static void SerializeStatic(ref MessagePackWriter writer, DatabaseFile? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(4);
            writer.WriteArrayHeader(value.Artworks.Length);
            foreach (var item in value.Artworks)
            {
                Artwork.Formatter.SerializeStatic(ref writer, item);
            }

            var userFormatter = options.Resolver.GetFormatterWithVerify<User>();
            writer.WriteArrayHeader(value.UserDictionary.Count);
            foreach (var item in value.UserDictionary.Values)
            {
                userFormatter.Serialize(ref writer, item, options);
            }
            StringSet.Formatter.SerializeStatic(ref writer, value.TagSet);
            StringSet.Formatter.SerializeStatic(ref writer, value.ToolSet);
        }

        public DatabaseFile? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => DeserializeStatic(ref reader, options);

        public static DatabaseFile? DeserializeStatic(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var header = reader.ReadArrayHeader();
            var artworks = Array.Empty<Artwork>();
            ConcurrentDictionary<ulong, User>? users = null;
            StringSet? tags = null;
            StringSet? tools = null;
            for (int memberIndex = 0; memberIndex < header; memberIndex++)
            {
                switch (memberIndex)
                {
                    case 0:
                        if (!reader.TryReadArrayHeader(out var artworkHeader) || artworkHeader == 0)
                        {
                            break;
                        }

                        artworks = new Artwork[artworkHeader];
                        for (int i = 0; i < artworks.Length; i++)
                        {
                            artworks[i] = Artwork.Formatter.DeserializeStatic(ref reader, options);
                        }
                        break;
                    case 1:
                        if (!reader.TryReadArrayHeader(out var userHeader) || userHeader == 0)
                        {
                            break;
                        }

                        var userFormatter = options.Resolver.GetFormatterWithVerify<User>();
                        users = new(Environment.ProcessorCount, userHeader);
                        for (int i = 0; i < userHeader; i++)
                        {
                            var user = userFormatter.Deserialize(ref reader, options);
                            users.TryAdd(user.Id, user);
                        }
                        break;
                    case 2:
                        tags = StringSet.Formatter.DeserializeStatic(ref reader);
                        break;
                    case 3:
                        tools = StringSet.Formatter.DeserializeStatic(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new(artworks, users ?? new(), tags ?? new(0), tools ?? new(0));
        }
    }
}
