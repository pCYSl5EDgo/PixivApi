using DatabaseAddArtworkFunc = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<PixivApi.Core.Local.Artwork>>;
using DatabaseAddUserFunc = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<PixivApi.Core.Local.User>>;
using DatabaseUpdateArtworkFunc = System.Func<PixivApi.Core.Local.Artwork, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask>;
using DatabaseUpdateUserFunc = System.Func<PixivApi.Core.Local.User, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask>;
using ArtworkDictionary = System.Collections.Concurrent.ConcurrentDictionary<ulong, PixivApi.Core.Local.Artwork>;
using UserDictionary = System.Collections.Concurrent.ConcurrentDictionary<ulong, PixivApi.Core.Local.User>;
using MessagePack;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MessagePack.Formatters;

namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
internal sealed class DatabaseFile : IDatabase
{
    [Key(0x00)] private readonly uint MajorVersion;
    [Key(0x01)] private readonly uint MinorVersion;
    [Key(0x02)] private readonly ArtworkDictionary ArtworkDictionary;
    [Key(0x03)] private readonly UserDictionary UserDictionary;
    [Key(0x04)] private readonly StringSet TagSet;
    [Key(0x05)] private readonly StringSet ToolSet;
    [Key(0x06)] private readonly RankingSet RankingSet;

    internal int IsChanged = 0;

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

    public DatabaseFile(uint majorVersion, uint minorVersion, ArtworkDictionary artworkDictionary, UserDictionary userDictionary, StringSet tagSet, StringSet toolSet, RankingSet rankingSet)
    {
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        ArtworkDictionary = artworkDictionary;
        UserDictionary = userDictionary;
        TagSet = tagSet;
        ToolSet = toolSet;
        RankingSet = rankingSet;
    }

    public Version Version => new((int)MajorVersion, (int)MinorVersion);

    public ValueTask<bool> AddOrUpdateAsync(ulong id, DatabaseAddArtworkFunc add, DatabaseUpdateArtworkFunc update, CancellationToken token)
    {
        _ = Interlocked.Exchange(ref IsChanged, 1);
        var isAdd = false;
        ArtworkDictionary.AddOrUpdate(id, (_, pair) =>
        {
            var (add, _, token) = pair;
            isAdd = true;
            return add(token).AsTask().Result;
        }, (_, value, pair) =>
        {
            var (_, update, token) = pair;
            update(value, token).AsTask().Wait();
            return value;
        }, (add, update, token));
        return ValueTask.FromResult(isAdd);
    }

    public ValueTask<bool> AddOrUpdateAsync(Artwork artwork, CancellationToken token)
    {
        _ = Interlocked.Exchange(ref IsChanged, 1);
        var isAdd = true;
        ArtworkDictionary.AddOrUpdate(artwork.Id, artwork, (_, _) =>
        {
            isAdd = false;
            return artwork;
        });
        return ValueTask.FromResult(isAdd);
    }

#pragma warning disable CS1998
    public async IAsyncEnumerable<bool> AddOrUpdateAsync(IEnumerable<Artwork> collection, [EnumeratorCancellation] CancellationToken token)
#pragma warning restore CS1998
    {
        foreach (var source in collection)
        {
            if (token.IsCancellationRequested)
            {
                yield break;
            }

            var add = true;
            ArtworkDictionary.AddOrUpdate(source.Id, source, (_, _) =>
            {
                add = false;
                return source;
            });

            yield return add;
        }
    }

    public ValueTask<bool> AddOrUpdateAsync(ulong id, DatabaseAddUserFunc add, DatabaseUpdateUserFunc update, CancellationToken token)
    {
        _ = Interlocked.Exchange(ref IsChanged, 1);
        var isAdd = false;
        UserDictionary.AddOrUpdate(id, (_, pair) =>
        {
            var (add, _, token) = pair;
            isAdd = true;
            return add(token).AsTask().Result;
        }, (_, value, pair) =>
        {
            var (_, update, token) = pair;
            update(value, token).AsTask().Wait();
            return value;
        }, (add, update, token));
        return ValueTask.FromResult(isAdd);
    }

    public ValueTask AddOrUpdateRankingAsync(DateOnly date, RankingKind kind, ulong[] values, CancellationToken token)
    {
        _ = Interlocked.Exchange(ref IsChanged, 1);
        RankingSet.AddOrUpdate(new RankingSet.Pair(date, kind), values, (_, _) => values);
        return ValueTask.CompletedTask;
    }

    public ValueTask<ulong> CountArtworkAsync(CancellationToken token) => ValueTask.FromResult((ulong)ArtworkDictionary.Count);

    public async ValueTask<ulong> CountArtworkAsync(ArtworkFilter filter, CancellationToken token)
    {
        if (filter.Count == 0)
        {
            return 0;
        }

        var answer = 0UL;
        async ValueTask CountAsync(Artwork item, CancellationToken token)
        {
            if (!filter.FastFilter(item))
            {
                return;
            }

            if (filter.HasSlowFilter && !await filter.SlowFilter(item, token).ConfigureAwait(false))
            {
                return;
            }

            _ = Interlocked.Increment(ref answer);
        }

        if (filter.IdFilter is { Ids: { Length: > 0 } ids })
        {
            await Parallel.ForEachAsync(ids.ToAsyncEnumerable().SelectAwaitWithCancellation(GetArtworkAsync).Where(x => x is not null), token, CountAsync!).ConfigureAwait(false);
        }
        else
        {
            await Parallel.ForEachAsync(ArtworkDictionary.Values, token, CountAsync).ConfigureAwait(false);
        }

        if (answer <= (ulong)filter.Offset)
        {
            return 0;
        }

        answer -= (ulong)filter.Offset;
        if (filter.Count.HasValue && (ulong)filter.Count.Value < answer)
        {
            answer = (ulong)filter.Count.Value;
        }

        return answer;
    }

    public ValueTask<ulong> CountRankingAsync(CancellationToken token) => ValueTask.FromResult((ulong)RankingSet.Count);

    public ValueTask<ulong> CountTagAsync(CancellationToken token) => ValueTask.FromResult((ulong)TagSet.Reverses.Count);

    public ValueTask<ulong> CountToolAsync(CancellationToken token) => ValueTask.FromResult((ulong)ToolSet.Reverses.Count);

    public ValueTask<ulong> CountUserAsync(CancellationToken token) => ValueTask.FromResult((ulong)UserDictionary.Count);

    public IAsyncEnumerable<Artwork> EnumerateArtworkAsync(CancellationToken token) => ArtworkDictionary.Values.ToAsyncEnumerable();

    public IAsyncEnumerable<User> EnumerateUserAsync(CancellationToken token) => UserDictionary.Values.ToAsyncEnumerable();

    public IAsyncEnumerable<(string, uint)> EnumerateTagAsync(CancellationToken _) => TagSet.Reverses.Select(static pair => (pair.Key, pair.Value)).ToAsyncEnumerable();

    public IAsyncEnumerable<(string, uint)> EnumerateToolAsync(CancellationToken _) => ToolSet.Reverses.Select(static pair => (pair.Key, pair.Value)).ToAsyncEnumerable();

    public async IAsyncEnumerable<uint> EnumeratePartialMatchTagAsync(string key, [EnumeratorCancellation] CancellationToken token)
    {
        ConcurrentBag<uint> bag = new();
        await Parallel.ForEachAsync(TagSet.Reverses, token, (pair, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            var (text, number) = pair;
            if (text.Contains(key))
            {
                bag.Add(number);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        foreach (var item in bag)
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<Artwork> FilterAsync(ArtworkFilter filter, [EnumeratorCancellation] CancellationToken token)
    {
        var bag = new ConcurrentBag<Artwork>();
        ValueTask AddToBag(Artwork item, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (filter.FastFilter(item))
            {
                bag.Add(item);
            }

            return ValueTask.CompletedTask;
        }

        if (filter.IdFilter is { Ids: { Length: > 0 } ids })
        {
            await Parallel.ForEachAsync(ids.ToAsyncEnumerable().SelectAwaitWithCancellation(GetArtworkAsync).Where(x => x is not null), AddToBag!).ConfigureAwait(false);
        }
        else
        {
            await Parallel.ForEachAsync(ArtworkDictionary.Values, token, AddToBag).ConfigureAwait(false);
        }

        var artworks = bag.ToAsyncEnumerable();
        if (filter.Order != ArtworkOrderKind.None)
        {
            artworks = artworks.OrderBy(filter.GetKey);
        }

        if (filter.HasSlowFilter)
        {
            artworks = artworks.WhereAwaitWithCancellation(filter.SlowFilter);
        }

        if (filter.Offset > 0)
        {
            artworks = artworks.Skip(filter.Offset);
        }

        if (filter.Count.HasValue)
        {
            artworks = artworks.Take(filter.Count.Value);
        }

        await foreach (var artwork in artworks)
        {
            if (token.IsCancellationRequested)
            {
                yield break;
            }

            yield return artwork;
        }
    }

    public async IAsyncEnumerable<User> FilterAsync(UserFilter filter, [EnumeratorCancellation] CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            yield break;
        }

        var bag = new ConcurrentBag<User>();
        await Parallel.ForEachAsync(UserDictionary.Values, token, (item, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (filter.FastFilter(item))
            {
                bag.Add(item);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        if (filter.HasSlowFilter)
        {
            foreach (var item in bag)
            {
                if (token.IsCancellationRequested)
                {
                    yield break;
                }

                if (await filter.SlowFilter(item, token).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }
        else
        {
            foreach (var item in bag)
            {
                if (token.IsCancellationRequested)
                {
                    yield break;
                }

                yield return item;
            }
        }
    }

    public ValueTask<uint?> FindTagAsync(string key, CancellationToken token) => ValueTask.FromResult(TagSet.Reverses.TryGetValue(key, out var tag) ? tag : default(uint?));

    public ValueTask<uint?> FindToolAsync(string key, CancellationToken token) => ValueTask.FromResult(ToolSet.Reverses.TryGetValue(key, out var tool) ? tool : default(uint?));

    public ValueTask<Artwork?> GetArtworkAsync(ulong id, CancellationToken token) => ValueTask.FromResult(ArtworkDictionary.TryGetValue(id, out var answer) ? answer : null);

    public ValueTask<ulong[]?> GetRankingAsync(DateOnly date, RankingKind kind, CancellationToken token) => ValueTask.FromResult(RankingSet.TryGetValue(new(date, kind), out var answer) ? answer : null);

    public ValueTask<string?> GetTagAsync(uint id, CancellationToken token) => ValueTask.FromResult(TagSet.Values.TryGetValue(id, out var answer) ? answer : null);

    public ValueTask<string?> GetToolAsync(uint id, CancellationToken token) => ValueTask.FromResult(ToolSet.Values.TryGetValue(id, out var answer) ? answer : null);

    public ValueTask<User?> GetUserAsync(ulong id, CancellationToken token) => ValueTask.FromResult(UserDictionary.TryGetValue(id, out var answer) ? answer : null);

    public ValueTask<uint> RegisterTagAsync(string value, CancellationToken token)
    {
        _ = Interlocked.Exchange(ref IsChanged, 1);
        return ValueTask.FromResult(TagSet.Register(value));
    }

    public ValueTask<uint> RegisterToolAsync(string value, CancellationToken token)
    {
        _ = Interlocked.Exchange(ref IsChanged, 1);
        return ValueTask.FromResult(ToolSet.Register(value));
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
            ArtworkDictionary? artworks = null;
            UserDictionary? users = null;
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
