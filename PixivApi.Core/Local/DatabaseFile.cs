namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
public sealed class DatabaseFile
{
    [Key(0x00)] public uint MajorVersion;
    [Key(0x01)] public uint MinorVersion;
    [Key(0x02)] public Artwork[] Artworks;
    [Key(0x03)] public ConcurrentDictionary<ulong, User> UserDictionary;
    [Key(0x04)] public StringSet TagSet;
    [Key(0x05)] public StringSet ToolSet;

    public DatabaseFile()
    {
        MajorVersion = 0;
        MinorVersion = 0;
        Artworks = Array.Empty<Artwork>();
        UserDictionary = new();
        TagSet = new(4096);
        ToolSet = new(256);
    }

    public DatabaseFile(uint majorVersion, uint minorVersion, Artwork[] artworks, ConcurrentDictionary<ulong, User> userDictionary, StringSet tagSet, StringSet toolSet)
    {
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        Artworks = artworks;
        UserDictionary = userDictionary;
        TagSet = tagSet;
        ToolSet = toolSet;
    }

    public async ValueTask OptimizeAsync(ParallelOptions parallelOptions)
    {
        var lackedTag = TagSet.Optimize();
        var lackedTool = ToolSet.Optimize();
        await Parallel.ForEachAsync(Artworks, parallelOptions, (artwork, token) =>
        {
            foreach (var (lacked, value) in lackedTag)
            {
                foreach (ref var item in artwork.Tags.AsSpan())
                {
                    if (item == value)
                    {
                        item = lacked;
                    }
                }

                foreach (ref var item in artwork.ExtraTags.AsSpan())
                {
                    if (item == value)
                    {
                        item = lacked;
                    }
                }

                foreach (ref var item in artwork.ExtraFakeTags.AsSpan())
                {
                    if (item == value)
                    {
                        item = lacked;
                    }
                }
            }

            foreach (var (lacked, value) in lackedTool)
            {
                foreach (ref var item in artwork.Tools.AsSpan())
                {
                    if (item == value)
                    {
                        item = lacked;
                    }
                }
            }
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
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

            writer.WriteArrayHeader(6);
            writer.Write(value.MajorVersion);
            writer.Write(value.MinorVersion);
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
            uint major = 0, minor = 0;
            var artworks = Array.Empty<Artwork>();
            ConcurrentDictionary<ulong, User>? users = null;
            StringSet? tags = null;
            StringSet? tools = null;
            for (var memberIndex = 0; memberIndex < header; memberIndex++)
            {
                switch (memberIndex)
                {
                    case 0:
                        major = reader.ReadUInt32();
                        break;
                    case 1:
                        minor = reader.ReadUInt32();
                        break;
                    case 2:
                        if (!reader.TryReadArrayHeader(out var artworkHeader) || artworkHeader == 0)
                        {
                            break;
                        }

                        artworks = new Artwork[artworkHeader];
                        for (var i = 0; i < artworks.Length; i++)
                        {
                            artworks[i] = Artwork.Formatter.DeserializeStatic(ref reader);
                        }
                        break;
                    case 3:
                        if (!reader.TryReadArrayHeader(out var userHeader) || userHeader == 0)
                        {
                            break;
                        }

                        var userFormatter = options.Resolver.GetFormatterWithVerify<User>();
                        users = new(Environment.ProcessorCount, userHeader);
                        for (var i = 0; i < userHeader; i++)
                        {
                            var user = userFormatter.Deserialize(ref reader, options);
                            users.TryAdd(user.Id, user);
                        }
                        break;
                    case 4:
                        tags = StringSet.Formatter.DeserializeStatic(ref reader);
                        break;
                    case 5:
                        tools = StringSet.Formatter.DeserializeStatic(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new(major, minor, artworks, users ?? new(), tags ?? new(0), tools ?? new(0));
        }
    }
}
