using System.Buffers.Binary;

namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
public sealed partial class Artwork : IEquatable<Artwork>, IEnumerable<uint>
{
    // 8 * 4
    public ulong Id;
    public ulong UserId;
    public ulong TotalView;
    public ulong TotalBookmarks;

    // 4 * 3
    public uint PageCount;
    public uint Width;
    public uint Height;

    // 1 * 3
    public ArtworkType Type;
    public FileExtensionKind Extension;
    public HideReason ExtraHideReason;

    // 6bit
    public bool IsOfficiallyRemoved;
    public bool IsXRestricted;
    public bool IsBookmarked;
    public bool IsVisible;
    public bool IsMuted;
    public bool ExtraHideLast;

    public DateTime CreateDate;
    public DateTime FileDate;
    public uint[] Tags = Array.Empty<uint>();
    public uint[]? ExtraTags;
    public uint[]? ExtraFakeTags;
    public uint[] Tools = Array.Empty<uint>();
    public string Title = string.Empty;
    public string Caption = string.Empty;
    public string? ExtraMemo;
    public Dictionary<uint, HideReason>? ExtraPageHideReasonDictionary;
    public ushort[]? UgoiraFrames;

    private void AddDateToUrl(ref DefaultInterpolatedStringHandler handler)
    {
        handler.AppendFormatted(FileDate.Year);
        handler.AppendFormatted('/');
        handler.AppendFormatted(FileDate.Month, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(FileDate.Day, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(FileDate.Hour, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(FileDate.Minute, format: "D2");
        handler.AppendFormatted('/');
        handler.AppendFormatted(FileDate.Second, format: "D2");
    }

    public string GetNotUgoiraOriginalUrl(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-original/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddNotUgoiraOriginalFileName(ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public string GetUgoiraOriginalUrl()
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-original/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddUgoiraOriginalFileName(ref handler);
        return handler.ToStringAndClear();
    }

    public string GetNotUgoiraThumbnailUrl(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/c/360x360_70/img-master/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddNotUgoiraThumbnailFileName(ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public string GetUgoiraThumbnailUrl()
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/c/360x360_70/img-master/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        handler.AppendFormatted(Id);
        handler.AppendLiteral("_square1200.jpg");
        return handler.ToStringAndClear();
    }

    public void AddNotUgoiraOriginalFileName(ref DefaultInterpolatedStringHandler handler, uint pageIndex)
    {
        handler.AppendFormatted(Id);
        handler.AppendLiteral("_p");
        handler.AppendFormatted(pageIndex);
        handler.AppendLiteral(GetExtensionText());
    }

    public string GetNotUgoiraOriginalFileName(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = new();
        AddNotUgoiraOriginalFileName(ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public void AddUgoiraOriginalFileName(ref DefaultInterpolatedStringHandler handler)
    {
        handler.AppendFormatted(Id);
        handler.AppendLiteral("_ugoira0");
        handler.AppendLiteral(GetExtensionText());
    }

    public string GetUgoiraOriginalFileName()
    {
        DefaultInterpolatedStringHandler handler = new();
        AddUgoiraOriginalFileName(ref handler);
        return handler.ToStringAndClear();
    }

    public void AddNotUgoiraThumbnailFileName(ref DefaultInterpolatedStringHandler handler, uint pageIndex)
    {
        handler.AppendFormatted(Id);
        handler.AppendLiteral("_p");
        handler.AppendFormatted(pageIndex);
        handler.AppendLiteral("_square1200.jpg");
    }

    public string GetUgoiraThumbnailFileName() => $"{Id}_square1200.jpg";

    public string GetNotUgoiraThumbnailFileName(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = new();
        AddNotUgoiraThumbnailFileName(ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public string GetUgoiraZipUrl()
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-zip-ugoira/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        handler.AppendFormatted(Id);
        handler.AppendLiteral("_ugoira600x600.zip");
        return handler.ToStringAndClear();
    }

    public string GetUgoiraZipFileNameWithoutExtension() => $"{Id}_ugoira600x600";
    public string GetUgoiraZipFileName() => $"{Id}_ugoira600x600.zip";

    public string GetExtensionText() => Extension switch
    {
        FileExtensionKind.Jpg => ".jpg",
        FileExtensionKind.Png => ".png",
        FileExtensionKind.Zip => ".zip",
        FileExtensionKind.None or _ => "",
    };

    public override string ToString() => Id.ToString();

    public override int GetHashCode() => Id.GetHashCode();

    public bool Equals(Artwork? other) => ReferenceEquals(this, other) || (other is not null && Id == other.Id && UserId == other.UserId);

    public override bool Equals(object? obj) => Equals(obj as Artwork);

    private bool stringify;

    public bool IsStringified
    {
        get => stringify;
        set
        {
            if (value)
            {
                return;
            }

            stringify = false;
        }
    }

    public IEnumerable<string>? StringifiedTags { get; private set; }

    public IEnumerable<string>? StringifiedTools { get; private set; }

    public string? UserName { get; private set; }

    public void Stringify(ConcurrentDictionary<ulong, User> userDictionary, StringSet tagSet, StringSet toolSet)
    {
        HashSet<uint> set = new(Tags);
        if (ExtraTags is { Length: > 0 })
        {
            foreach (var item in ExtraTags)
            {
                set.Add(item);
            }
        }

        if (ExtraFakeTags is { Length: > 0 })
        {
            foreach (var item in ExtraFakeTags)
            {
                set.Remove(item);
            }
        }

        StringifiedTags = set.Count == 0 ? Array.Empty<string>() : set.Select(x => tagSet.Values[x]);
        StringifiedTools = Tools.Select(x => toolSet.Values[x]);
        UserName = userDictionary[UserId].Name;
        stringify = true;
    }

    public sealed class Formatter : IMessagePackFormatter<Artwork>
    {
        private const int BinLength = sizeof(ulong) * 4 + sizeof(uint) * 3 + sizeof(ArtworkType) + sizeof(FileExtensionKind) + sizeof(HideReason) + 1;

        public void Serialize(ref MessagePackWriter writer, Artwork value, MessagePackSerializerOptions options) => SerializeStatic(ref writer, value);

        public static void SerializeStatic(ref MessagePackWriter writer, Artwork value)
        {
            writer.WriteArrayHeader(12);
            // 0
            {
                writer.WriteBinHeader(BinLength);
                var span = writer.GetSpan(BinLength);
                BinaryPrimitives.WriteUInt64LittleEndian(span, value.Id);
                span = span[sizeof(ulong)..];
                BinaryPrimitives.WriteUInt64LittleEndian(span, value.UserId);
                span = span[sizeof(ulong)..];
                BinaryPrimitives.WriteUInt64LittleEndian(span, value.TotalView);
                span = span[sizeof(ulong)..];
                BinaryPrimitives.WriteUInt64LittleEndian(span, value.TotalBookmarks);
                span = span[sizeof(ulong)..];
                BinaryPrimitives.WriteUInt32LittleEndian(span, value.PageCount);
                span = span[sizeof(uint)..];
                BinaryPrimitives.WriteUInt32LittleEndian(span, value.Width);
                span = span[sizeof(uint)..];
                BinaryPrimitives.WriteUInt32LittleEndian(span, value.Height);
                span = span[sizeof(uint)..];
                span[0] = (byte)value.Type;
                span[1] = (byte)value.Extension;
                span[2] = (byte)value.ExtraHideReason;
                span[3] = (byte)
                    (
                    ((value.IsOfficiallyRemoved ? 1U : 0) << 5)
                    | ((value.IsXRestricted ? 1U : 0) << 4)
                    | ((value.IsBookmarked ? 1U : 0) << 3)
                    | ((value.IsVisible ? 1U : 0) << 2)
                    | ((value.IsMuted ? 1U : 0) << 1)
                    | (value.ExtraHideLast ? 1U : 0)
                    );
                writer.Advance(BinLength);
            }

            writer.Write(value.CreateDate); // 1
            writer.Write(value.FileDate);
            WriteArray(ref writer, value.Tags);
            WriteArray(ref writer, value.ExtraTags);
            WriteArray(ref writer, value.ExtraFakeTags);
            WriteArray(ref writer, value.Tools);
            writer.Write(value.Title);
            writer.Write(value.Caption);
            writer.Write(value.ExtraMemo);
            WriteDictionary(ref writer, value.ExtraPageHideReasonDictionary);
            WriteArray(ref writer, value.UgoiraFrames);
        }

        public Artwork Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => DeserializeStatic(ref reader);

        public static Artwork DeserializeStatic(ref MessagePackReader reader)
        {
            if (!reader.TryReadArrayHeader(out var header))
            {
                return new();
            }

            var answer = new Artwork();
            Span<byte> segmentSpan = stackalloc byte[BinLength];
            for (var i = 0; i < header; i++)
            {
                switch (i)
                {
                    case 0x00:
                        {
                            var bytes = reader.ReadBytes();
                            if (!bytes.HasValue)
                            {
                                break;
                            }

                            bytes.Value.CopyTo(segmentSpan);
                            answer.Id = MemoryMarshal.Read<ulong>(segmentSpan);
                            answer.UserId = MemoryMarshal.Read<ulong>(segmentSpan[8..]);
                            answer.TotalView = MemoryMarshal.Read<ulong>(segmentSpan[16..]);
                            answer.TotalBookmarks = MemoryMarshal.Read<ulong>(segmentSpan[24..]);
                            answer.PageCount = MemoryMarshal.Read<uint>(segmentSpan[32..]);
                            answer.Width = MemoryMarshal.Read<uint>(segmentSpan[36..]);
                            answer.Height = MemoryMarshal.Read<uint>(segmentSpan[40..]);
                            answer.Type = (ArtworkType)segmentSpan[44];
                            answer.Extension = (FileExtensionKind)segmentSpan[45];
                            answer.ExtraHideReason = (HideReason)segmentSpan[46];
                            var flags = segmentSpan[47];
                            if ((flags & 0b100000) != 0)
                            {
                                answer.IsOfficiallyRemoved = true;
                            }
                            if ((flags & 0b10000) != 0)
                            {
                                answer.IsXRestricted = true;
                            }
                            if ((flags & 0b1000) != 0)
                            {
                                answer.IsBookmarked = true;
                            }
                            if ((flags & 0b100) != 0)
                            {
                                answer.IsVisible = true;
                            }
                            if ((flags & 0b10) != 0)
                            {
                                answer.IsMuted = true;
                            }
                            if ((flags & 0b1) != 0)
                            {
                                answer.ExtraHideLast = true;
                            }
                        }
                        break;
                    case 0x01:
                        answer.CreateDate = reader.ReadDateTime();
                        break;
                    case 0x02:
                        answer.FileDate = reader.ReadDateTime();
                        break;
                    case 0x03:
                        answer.Tags = ReadUInt32Array(ref reader) ?? Array.Empty<uint>();
                        break;
                    case 0x04:
                        answer.ExtraTags = ReadUInt32Array(ref reader);
                        break;
                    case 0x05:
                        answer.ExtraFakeTags = ReadUInt32Array(ref reader);
                        break;
                    case 0x06:
                        answer.Tools = ReadUInt32Array(ref reader) ?? Array.Empty<uint>();
                        break;
                    case 0x07:
                        answer.Title = reader.ReadString() ?? string.Empty;
                        break;
                    case 0x08:
                        answer.Caption = reader.ReadString() ?? string.Empty;
                        break;
                    case 0x09:
                        answer.ExtraMemo = reader.ReadString();
                        break;
                    case 0x0a:
                        answer.ExtraPageHideReasonDictionary = ReadDictionary(ref reader);
                        break;
                    case 0x0b:
                        answer.UgoiraFrames = ReadUInt16Array(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return answer;
        }

        private static void WriteDictionary(ref MessagePackWriter writer, Dictionary<uint, HideReason>? value)
        {
            if (value is not { Count: > 0 })
            {
                writer.WriteNil();
                return;
            }

            var binLength = value.Count * 5;
            writer.WriteBinHeader(binLength);
            ref var first = ref writer.GetSpan(binLength)[0];
            foreach (var (k, v) in value)
            {
                Unsafe.WriteUnaligned(ref first, k);
                first = ref Unsafe.Add(ref first, sizeof(uint));
                first = (byte)v;
                first = ref Unsafe.Add(ref first, sizeof(byte));
            }
            writer.Advance(binLength);
        }

        private static Dictionary<uint, HideReason>? ReadDictionary(ref MessagePackReader reader)
        {
            var bytes = reader.ReadBytes();
            if (!bytes.HasValue)
            {
                return null;
            }

            var answer = new Dictionary<uint, HideReason>();
            var sequence = bytes.Value;
            if (sequence.IsSingleSegment)
            {
                var span = sequence.FirstSpan;
                var length = span.Length;
                if (length == 0)
                {
                    goto END;
                }

                ref var first = ref Unsafe.AsRef(in span[0]);
                for (var i = 0; i < length; i += 5)
                {
                    var key = Unsafe.ReadUnaligned<uint>(ref first);
                    first = ref Unsafe.Add(ref first, 4);
                    answer.Add(key, (HideReason)first);
                    first = ref Unsafe.Add(ref first, 1);
                }
            }
            else
            {
                Span<byte> tmp = stackalloc byte[5];
                var tmpIndex = 0;
                var enumerator = sequence.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var span = enumerator.Current.Span;
                    if (tmpIndex != 0)
                    {
                        if (5 - tmpIndex <= span.Length)
                        {
                            span[0..(5 - tmpIndex)].CopyTo(tmp[tmpIndex..]);
                            span = span[(5 - tmpIndex)..];
                            answer.Add(MemoryMarshal.Read<uint>(tmp), (HideReason)tmp[4]);
                            tmpIndex = 0;
                        }
                        else
                        {
                            span.CopyTo(tmp[tmpIndex..]);
                            tmpIndex += span.Length;
                            continue;
                        }
                    }

                    while (span.Length >= 5)
                    {
                        answer.Add(MemoryMarshal.Read<uint>(span), (HideReason)span[4]);
                        span = span[5..];
                    }

                    if (!span.IsEmpty)
                    {
                        span.CopyTo(tmp[tmpIndex..]);
                        tmpIndex += span.Length;
                    }
                }
            }

        END:
            return answer;
        }

        private static void WriteArray(ref MessagePackWriter writer, ushort[]? value)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            if (value.Length == 0)
            {
                writer.WriteBinHeader(0);
                return;
            }

            var first = value[0];
            for (var i = 1; i < value.Length; i++)
            {
                if (value[i] != first)
                {
                    goto ARRAY;
                }
            }

            writer.WriteMapHeader(1);
            writer.Write(value.Length);
            writer.Write(first);
            return;

        ARRAY:
            writer.WriteBinHeader(value.Length << 1);
            var span = writer.GetSpan(value.Length << 1);
            MemoryMarshal.AsBytes(value.AsSpan()).CopyTo(span);
            writer.Advance(value.Length << 1);
        }

        private static void WriteArray(ref MessagePackWriter writer, uint[]? value)
        {
            writer.WriteBinHeader(value is null ? 0 : value.Length << 2);
            if (value is not { Length: > 0 })
            {
                return;
            }

            var span = writer.GetSpan(value.Length << 2);
            MemoryMarshal.AsBytes(value.AsSpan()).CopyTo(span);
            writer.Advance(value.Length << 2);
        }

        private static uint[]? ReadUInt32Array(ref MessagePackReader reader)
        {
            var bytes = reader.ReadBytes();
            if (!bytes.HasValue)
            {
                return null;
            }

            var sequence = bytes.Value;
            var length = sequence.Length >> 2;
            if (length == 0)
            {
                return Array.Empty<uint>();
            }

            var answer = new uint[length];
            sequence.CopyTo(MemoryMarshal.AsBytes(answer.AsSpan()));
            return answer;
        }

        private static ushort[]? ReadUInt16Array(ref MessagePackReader reader)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            ushort[] answer;
            if (reader.NextMessagePackType == MessagePackType.Map)
            {
                var mapHeader = reader.ReadMapHeader();
                if (mapHeader == 0)
                {
                    return Array.Empty<ushort>();
                }

                if (mapHeader == 1)
                {
                    var count = reader.ReadInt32();
                    if (count == 0)
                    {
                        return Array.Empty<ushort>();
                    }

                    answer = new ushort[count];
                    Array.Fill(answer, reader.ReadUInt16());
                }
                else
                {
                    Span<(int, ushort)> pairs = stackalloc (int, ushort)[mapHeader];
                    var sum = 0;
                    for (var i = 0; i < mapHeader; i++)
                    {
                        pairs[i] = (reader.ReadInt32(), reader.ReadUInt16());
                        sum += pairs[i].Item1;
                    }

                    if (sum == 0)
                    {
                        return Array.Empty<ushort>();
                    }

                    answer = new ushort[sum];
                    var index = 0;
                    foreach (var (count, value) in pairs)
                    {
                        answer.AsSpan(index, count).Fill(value);
                        index += count;
                    }
                }
            }
            else
            {
                var bytes = reader.ReadBytes();
                if (!bytes.HasValue)
                {
                    return null;
                }

                var sequence = bytes.Value;
                var mapHeader = sequence.Length >> 1;
                if (mapHeader == 0)
                {
                    return Array.Empty<ushort>();
                }

                answer = new ushort[mapHeader];
                sequence.CopyTo(MemoryMarshal.AsBytes(answer.AsSpan()));
            }

            return answer;
        }
    }

    #region Literals
    [StringLiteral.Utf8("id")] private static partial ReadOnlySpan<byte> LiteralId();
    [StringLiteral.Utf8("title")] private static partial ReadOnlySpan<byte> LiteralTitle();
    [StringLiteral.Utf8("caption")] private static partial ReadOnlySpan<byte> LiteralCaption();
    [StringLiteral.Utf8("total-view")] private static partial ReadOnlySpan<byte> LiteralTotalView();
    [StringLiteral.Utf8("total-bookmarks")] private static partial ReadOnlySpan<byte> LiteralTotalBookmarks();

    [StringLiteral.Utf8("tags")] private static partial ReadOnlySpan<byte> LiteralTags();
    [StringLiteral.Utf8("fake-tags")] private static partial ReadOnlySpan<byte> LiteralFakeTags();
    [StringLiteral.Utf8("extra-tags")] private static partial ReadOnlySpan<byte> LiteralExtraTags();

    [StringLiteral.Utf8("tools")] private static partial ReadOnlySpan<byte> LiteralTools();
    [StringLiteral.Utf8("user-id")] private static partial ReadOnlySpan<byte> LiteralUserId();
    [StringLiteral.Utf8("user-name")] private static partial ReadOnlySpan<byte> LiteralUserName();

    [StringLiteral.Utf8("page-count")] private static partial ReadOnlySpan<byte> LiteralPageCount();
    [StringLiteral.Utf8("width")] private static partial ReadOnlySpan<byte> LiteralWidth();
    [StringLiteral.Utf8("height")] private static partial ReadOnlySpan<byte> LiteralHeight();

    [StringLiteral.Utf8("create-date")] private static partial ReadOnlySpan<byte> LiteralCreateDate();
    [StringLiteral.Utf8("file-date")] private static partial ReadOnlySpan<byte> LiteralFileDate();

    [StringLiteral.Utf8("type")] private static partial ReadOnlySpan<byte> LiteralType();
    [StringLiteral.Utf8("none")] private static partial ReadOnlySpan<byte> LiteralNone();
    [StringLiteral.Utf8("illust")] private static partial ReadOnlySpan<byte> LiteralIllust();
    [StringLiteral.Utf8("manga")] private static partial ReadOnlySpan<byte> LiteralManga();
    [StringLiteral.Utf8("ugoira")] private static partial ReadOnlySpan<byte> LiteralUgoira();

    [StringLiteral.Utf8("extension")] private static partial ReadOnlySpan<byte> LiteralExtension();
    [StringLiteral.Utf8("jpg")] private static partial ReadOnlySpan<byte> LiteralJpg();
    [StringLiteral.Utf8("png")] private static partial ReadOnlySpan<byte> LiteralPng();
    [StringLiteral.Utf8("zip")] private static partial ReadOnlySpan<byte> LiteralZip();

    [StringLiteral.Utf8("hide-reason")] private static partial ReadOnlySpan<byte> LiteralHideReason();
    [StringLiteral.Utf8("not-hidden")] private static partial ReadOnlySpan<byte> LiteralNotHidden();
    [StringLiteral.Utf8("low-quality")] private static partial ReadOnlySpan<byte> LiteralLowQuality();
    [StringLiteral.Utf8("not-much")] private static partial ReadOnlySpan<byte> LiteralNotMuch();
    [StringLiteral.Utf8("irrelevant")] private static partial ReadOnlySpan<byte> LiteralIrrelevant();
    [StringLiteral.Utf8("external-link")] private static partial ReadOnlySpan<byte> LiteralExternalLink();
    [StringLiteral.Utf8("dislike")] private static partial ReadOnlySpan<byte> LiteralDislike();
    [StringLiteral.Utf8("unfollow")] private static partial ReadOnlySpan<byte> LiteralUnfollow();

    [StringLiteral.Utf8("officially-removed")] private static partial ReadOnlySpan<byte> LiteralIsOfficiallyRemoved();
    [StringLiteral.Utf8("r18")] private static partial ReadOnlySpan<byte> LiteralIsXRestricted();
    [StringLiteral.Utf8("bookmarked")] private static partial ReadOnlySpan<byte> LiteralIsBookmarked();
    [StringLiteral.Utf8("visible")] private static partial ReadOnlySpan<byte> LiteralIsVisible();
    [StringLiteral.Utf8("muted")] private static partial ReadOnlySpan<byte> LiteralIsMuted();
    [StringLiteral.Utf8("hide-last")] private static partial ReadOnlySpan<byte> LiteralExtraHideLast();

    [StringLiteral.Utf8("memo")] private static partial ReadOnlySpan<byte> LiteralExtraMemo();
    [StringLiteral.Utf8("ugoira-frames")] private static partial ReadOnlySpan<byte> LiteralUgoiraFrames();

    [StringLiteral.Utf8("page-hide-reason-dictionary")] private static partial ReadOnlySpan<byte> LiteralExtraPageHideReasonDictionary();
    #endregion

    public struct PageIndexEnumerator : IEnumerator<uint>
    {
        private readonly int maxExclusive;
        private readonly Dictionary<uint, HideReason>? dictionary;
        private int index;

        public PageIndexEnumerator(Artwork artwork)
        {
            maxExclusive = (int)artwork.PageCount;
            if (artwork.ExtraHideLast)
            {
                --maxExclusive;
            }

            index = artwork.ExtraHideReason != HideReason.NotHidden ? maxExclusive : -1;
            if (artwork.ExtraPageHideReasonDictionary is { Count: > 0 } dictionary)
            {
                this.dictionary = dictionary;
                return;
            }
            else
            {
                this.dictionary = null;
            }
        }

        public uint Current => (uint)index;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            do
            {
                if (++index >= maxExclusive)
                {
                    return false;
                }

                if (dictionary is not null && dictionary.TryGetValue((uint)index, out var reason) && reason != HideReason.NotHidden)
                {
                    continue;
                }

                return true;
            } while (true);
        }

        public void Reset() => index = -1;
    }

    public PageIndexEnumerator GetEnumerator() => new(this);

    IEnumerator<uint> IEnumerable<uint>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public sealed class Converter : JsonConverter<Artwork>
    {
        public static readonly Converter Instance = new();

        public override Artwork? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        private static ReadOnlySpan<byte> GetLiteral(HideReason hideReason) => hideReason switch
        {
            HideReason.NotHidden => LiteralNotHidden(),
            HideReason.LowQuality => LiteralLowQuality(),
            HideReason.NotMuch => LiteralNotMuch(),
            HideReason.Irrelevant => LiteralIrrelevant(),
            HideReason.ExternalLink => LiteralExternalLink(),
            HideReason.Dislike => LiteralDislike(),
            HideReason.Unfollow => LiteralUnfollow(),
            _ => throw new InvalidDataException(hideReason.ToString()),
        };

        public override void Write(Utf8JsonWriter writer, Artwork value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(LiteralId(), value.Id);
            writer.WriteString(LiteralTitle(), value.Title);

            writer.WriteNumber(LiteralUserId(), value.UserId);
            if (value.IsStringified)
            {
                writer.WriteString(LiteralUserName(), value.UserName);
            }

            writer.WriteNumber(LiteralTotalView(), value.TotalView);
            writer.WriteNumber(LiteralTotalBookmarks(), value.TotalBookmarks);

            writer.WriteNumber(LiteralPageCount(), value.PageCount);
            writer.WriteNumber(LiteralWidth(), value.Width);
            writer.WriteNumber(LiteralHeight(), value.Height);

            writer.WritePropertyName(LiteralCreateDate());
            writer.WriteStringValue(value.CreateDate);
            if (value.FileDate != value.CreateDate.ToLocalTime())
            {
                writer.WritePropertyName(LiteralFileDate());
                writer.WriteStringValue(value.FileDate);
            }

            writer.WriteString(LiteralType(), value.Type switch
            {
                ArtworkType.Illust => LiteralIllust(),
                ArtworkType.Manga => LiteralManga(),
                ArtworkType.Ugoira => LiteralUgoira(),
                ArtworkType.None or _ => LiteralNone(),
            });

            writer.WriteString(LiteralExtension(), value.Extension switch
            {
                FileExtensionKind.Jpg => LiteralJpg(),
                FileExtensionKind.Png => LiteralPng(),
                FileExtensionKind.Zip => LiteralZip(),
                FileExtensionKind.None or _ => LiteralNone(),
            });

            writer.WriteBoolean(LiteralIsOfficiallyRemoved(), value.IsOfficiallyRemoved);
            writer.WriteBoolean(LiteralIsXRestricted(), value.IsXRestricted);
            writer.WriteBoolean(LiteralIsBookmarked(), value.IsBookmarked);
            writer.WriteBoolean(LiteralIsVisible(), value.IsVisible);
            writer.WriteBoolean(LiteralIsMuted(), value.IsMuted);

            if (value.ExtraHideReason != HideReason.NotHidden)
            {
                writer.WriteString(LiteralHideReason(), GetLiteral(value.ExtraHideReason));
            }

            if (value.ExtraHideLast)
            {
                writer.WriteBoolean(LiteralExtraHideLast(), true);
            }

            if (value.ExtraPageHideReasonDictionary is { Count: > 0 } dictionary)
            {
                writer.WriteStartObject(LiteralExtraPageHideReasonDictionary());
                using var builder = ZString.CreateUtf8StringBuilder();
                foreach (var (page, reason) in dictionary)
                {
                    if (reason == HideReason.NotHidden)
                    {
                        continue;
                    }

                    builder.Clear();
                    builder.Append(page);
                    writer.WriteString(builder.AsSpan(), GetLiteral(reason));
                }
                writer.WriteEndObject();
            }

            if (value.Type == ArtworkType.Ugoira)
            {
                if (value.UgoiraFrames is null)
                {
                    writer.WriteNull(LiteralUgoiraFrames());
                }
                else
                {
                    writer.WriteStartArray(LiteralUgoiraFrames());
                    foreach (var frame in value.UgoiraFrames)
                    {
                        writer.WriteNumberValue(frame);
                    }
                    writer.WriteEndArray();
                }
            }

            if (value.IsStringified)
            {
                if (value.StringifiedTags is { } tags)
                {
                    writer.WriteStartArray(LiteralTags());
                    foreach (var tag in tags)
                    {
                        writer.WriteStringValue(tag);
                    }
                    writer.WriteEndArray();
                }

                if (value.StringifiedTools is { } tools)
                {
                    writer.WriteStartArray(LiteralTools());
                    foreach (var tool in tools)
                    {
                        writer.WriteStringValue(tool);
                    }
                    writer.WriteEndArray();
                }
            }
            else
            {
                if (value.Tags is { Length: > 0 } tags)
                {
                    writer.WriteStartArray(LiteralTags());
                    foreach (var tag in tags)
                    {
                        writer.WriteNumberValue(tag);
                    }
                    writer.WriteEndArray();
                }

                if (value.ExtraTags is { Length: > 0 } extraTags)
                {
                    writer.WriteStartArray(LiteralExtraTags());
                    foreach (var tag in extraTags)
                    {
                        writer.WriteNumberValue(tag);
                    }
                    writer.WriteEndArray();
                }

                if (value.ExtraFakeTags is { Length: > 0 } extraFakeTags)
                {
                    writer.WriteStartArray(LiteralFakeTags());
                    foreach (var tag in extraFakeTags)
                    {
                        writer.WriteNumberValue(tag);
                    }
                    writer.WriteEndArray();
                }

                if (value.Tools is { Length: > 0 } tools)
                {
                    writer.WriteStartArray(LiteralTools());
                    foreach (var tool in tools)
                    {
                        writer.WriteNumberValue(tool);
                    }
                    writer.WriteEndArray();
                }
            }

            writer.WriteString(LiteralCaption(), value.Caption);

            if (!string.IsNullOrWhiteSpace(value.ExtraMemo))
            {
                writer.WriteString(LiteralExtraMemo(), value.ExtraMemo);
            }

            writer.WriteEndObject();
        }
    }
}
