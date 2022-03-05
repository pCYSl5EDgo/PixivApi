namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
public sealed class DatabaseFile : IDatabase<Artwork>
{
    [Key(0x00)] public uint MajorVersion;
    [Key(0x01)] public uint MinorVersion;
    [Key(0x02)] public ConcurrentDictionary<ulong, Artwork> ArtworkDictionary;
    [Key(0x03)] public ConcurrentDictionary<ulong, User> UserDictionary;
    [Key(0x04)] public StringSet TagSet;
    [Key(0x05)] public StringSet ToolSet;
    [Key(0x06)] public RankingSet RankingSet;

    public DatabaseFile()
    {
        MajorVersion = 0;
        MinorVersion = 0;
        ArtworkDictionary = new();
        UserDictionary = new();
        TagSet = new(4096);
        ToolSet = new(256);
        RankingSet = new();
    }

    public DatabaseFile(uint majorVersion, uint minorVersion, ConcurrentDictionary<ulong, Artwork> artworkDictionary, ConcurrentDictionary<ulong, User> userDictionary, StringSet tagSet, StringSet toolSet, RankingSet rankingSet)
    {
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        ArtworkDictionary = artworkDictionary;
        UserDictionary = userDictionary;
        TagSet = tagSet;
        ToolSet = toolSet;
        RankingSet = rankingSet;
    }

    ValueTask<bool> IDatabase<Artwork>.AddOrUpdateAsync<TArg>(ulong id, Func<TArg, Artwork> addFunc, Action<Artwork, TArg> updateFunc, TArg arg, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<bool>(token);
        }

        var add = true;
        ArtworkDictionary.AddOrUpdate(id, (_, arg) => addFunc(arg), (_, artwork, arg) =>
        {
            add = false;
            updateFunc(artwork, arg);
            return artwork;
        }, arg);
        return ValueTask.FromResult(add);
    }

    async ValueTask<IEnumerable<Artwork>> IDatabase<Artwork>.EnumerateAsync(IFilter<Artwork> filter, CancellationToken token)
    {
        var bag = new ConcurrentBag<Artwork>();
        await Parallel.ForEachAsync(ArtworkDictionary.Values, token, (artwork, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (filter.FastFilter(artwork))
            {
                bag.Add(artwork);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        return filter.HasSlowFilter ? bag.Where(filter.SlowFilter) : (IEnumerable<Artwork>)bag;
    }

    ValueTask<Artwork?> IDatabase<Artwork>.GetAsync(ulong id, CancellationToken token) => token.IsCancellationRequested
        ? ValueTask.FromCanceled<Artwork?>(token)
        : ValueTask.FromResult(ArtworkDictionary.TryGetValue(id, out var artwork) ? artwork : null);

    ValueTask<bool> IDatabase<Artwork>.GetOrAddAsync<TArg>(ulong id, Func<TArg, Artwork> addFunc, TArg arg, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<bool>(token);
        }

        var get = true;
        _ = ArtworkDictionary.GetOrAdd(id, (_, arg) =>
        {
            get = false;
            return addFunc(arg);
        }, arg);
        return ValueTask.FromResult(get);
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

            writer.WriteArrayHeader(7);
            writer.Write(value.MajorVersion);
            writer.Write(value.MinorVersion);
            writer.WriteArrayHeader(value.ArtworkDictionary.Count);
            foreach (var item in value.ArtworkDictionary.Values)
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
            RankingSet.Formatter.SerializeStatic(ref writer, value.RankingSet);
        }

        public DatabaseFile? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => DeserializeStatic(ref reader, options);

        public static DatabaseFile? DeserializeStatic(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var token = reader.CancellationToken;
            token.ThrowIfCancellationRequested();
            if (reader.TryReadNil())
            {
                return null;
            }

            var header = reader.ReadArrayHeader();
            uint major = 0, minor = 0;
            ConcurrentDictionary<ulong, Artwork>? artworks = null;
            ConcurrentDictionary<ulong, User>? users = null;
            StringSet? tags = null;
            StringSet? tools = null;
            RankingSet? rankings = null;
            for (var memberIndex = 0; memberIndex < header; memberIndex++)
            {
                token.ThrowIfCancellationRequested();
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

                        artworks = new(Environment.ProcessorCount, artworkHeader);
                        for (var i = 0; i < artworkHeader; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            var artwork = Artwork.Formatter.DeserializeStatic(ref reader);
                            artworks.TryAdd(artwork.Id, artwork);
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
                            token.ThrowIfCancellationRequested();
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
                    case 6:
                        rankings = RankingSet.Formatter.DeserializeStatic(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new(major, minor, artworks ?? new(), users ?? new(), tags ?? new(0), tools ?? new(0), rankings ?? new());
        }
    }
}
