using System.Buffers.Binary;

namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
public sealed partial class Artwork : IOverwrite<Artwork>, IEquatable<Artwork>
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

    public void Overwrite(Artwork source)
    {
        if (Id != source.Id)
        {
            return;
        }

        if (UserId != source.UserId || source.UserId == 0)
        {
            IsOfficiallyRemoved = true;
        }

        if (TotalView < source.TotalView)
        {
            TotalView = source.TotalView;
        }

        TotalBookmarks = source.TotalBookmarks;
        PageCount = source.PageCount;
        Width = source.Width;
        Height = source.Height;
        Type = source.Type;
        Extension = source.Extension;
        ExtraHideReason = source.ExtraHideReason;
        IsOfficiallyRemoved = source.IsOfficiallyRemoved;
        IsXRestricted = source.IsXRestricted;
        IsBookmarked = source.IsBookmarked;
        IsVisible = source.IsVisible;
        IsMuted = source.IsMuted;
        ExtraHideLast = source.ExtraHideLast;
        CreateDate = source.CreateDate;
        FileDate = source.FileDate;
        Tags = source.Tags;
        OverwriteExtensions.Overwrite(ref ExtraTags, source.ExtraTags);
        OverwriteExtensions.Overwrite(ref ExtraFakeTags, source.ExtraFakeTags);
        Tools = source.Tools;
        Title = source.Title;
        Caption = source.Caption;
        OverwriteExtensions.Overwrite(ref ExtraMemo, source.ExtraMemo);
        OverwriteExtensions.Overwrite(ref ExtraPageHideReasonDictionary, source.ExtraPageHideReasonDictionary);
        OverwriteExtensions.Overwrite(ref UgoiraFrames, source.UgoiraFrames);
    }

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

    public string GetOriginalUrl(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-original/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddOriginalFileName(ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public string GetThumbnailUrl(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/c/360x360_70/img-master/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddThumbnailFileName(ref handler, pageIndex);
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

    public void AddOriginalFileName(ref DefaultInterpolatedStringHandler handler, uint pageIndex)
    {
        handler.AppendFormatted(Id);
        if (Type == ArtworkType.Ugoira)
        {
            handler.AppendLiteral("_ugoira0");
        }
        else
        {
            handler.AppendLiteral("_p");
            handler.AppendFormatted(pageIndex);
        }

        handler.AppendLiteral(GetExtensionText());
    }

    public string GetOriginalFileName(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = new();
        AddOriginalFileName(ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public void AddThumbnailFileName(ref DefaultInterpolatedStringHandler handler, uint pageIndex)
    {
        handler.AppendFormatted(Id);
        if (Type == ArtworkType.Ugoira)
        {
            handler.AppendLiteral("_square1200.jpg");
        }
        else
        {
            handler.AppendLiteral("_p");
            handler.AppendFormatted(pageIndex);
            handler.AppendLiteral("_square1200.jpg");
        }
    }

    public string GetUgoiraThumbnailFileName() => $"{Id}_square1200.jpg";

    public string GetThumbnailFileName(uint pageIndex)
    {
        DefaultInterpolatedStringHandler handler = new();
        AddThumbnailFileName(ref handler, pageIndex);
        return handler.ToStringAndClear();
    }

    public string GetZipUrl()
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/img-zip-ugoira/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        handler.AppendFormatted(Id);
        handler.AppendLiteral("_ugoira600x600.zip");
        return handler.ToStringAndClear();
    }

    public string GetZipFileNameWithoutExtension() => $"{Id}_ugoira600x600";
    public string GetZipFileName() => $"{Id}_ugoira600x600.zip";

    public string GetExtensionText() => Extension switch
    {
        FileExtensionKind.Jpg => ".jpg",
        FileExtensionKind.Png => ".png",
        FileExtensionKind.Gif => ".gif",
        FileExtensionKind.Zip => ".zip",
        FileExtensionKind.Bmp => ".bmp",
        FileExtensionKind.None or _ => "",
    };

    private static FileExtensionKind ConvertFromReadOnlySpanToFileExtensionKind(ReadOnlySpan<char> ext)
    {
        if (ext.SequenceEqual(".jpg") || ext.SequenceEqual(".jpeg"))
        {
            return FileExtensionKind.Jpg;
        }
        else if (ext.SequenceEqual(".png"))
        {
            return FileExtensionKind.Png;
        }
        else if (ext.SequenceEqual(".gif"))
        {
            return FileExtensionKind.Gif;
        }
        else if (ext.SequenceEqual(".zip"))
        {
            return FileExtensionKind.Zip;
        }
        else if (ext.SequenceEqual(".bmp"))
        {
            return FileExtensionKind.Bmp;
        }
        else
        {
            return FileExtensionKind.None;
        }
    }

    // Local Save Format is seems to be unstable.
    // This type should rely on relatively stable Network.Artwork
    public static Artwork ConvertFromNetwrok(Network.Artwork artwork, StringSet tagSet, StringSet toolSet, ConcurrentDictionary<ulong, User> userDictionary)
    {
        Artwork answer = new()
        {
            Id = artwork.Id,
            UserId = artwork.User.Id,
            TotalView = artwork.TotalView,
            TotalBookmarks = artwork.TotalBookmarks,
            PageCount = artwork.PageCount,
            Width = artwork.Width,
            Height = artwork.Height,
            Type = artwork.Type,
            Extension = ConvertFromReadOnlySpanToFileExtensionKind(artwork.MetaSinglePage.OriginalImageUrl is string url ? url.AsSpan(url.LastIndexOf('.')) : artwork.MetaPages[0].ImageUrls.Original is string original ? original.AsSpan(original.LastIndexOf('.')) : throw new NullReferenceException()),
            CreateDate = artwork.CreateDate,
            Title = artwork.Title,
            Caption = artwork.Caption,
            IsXRestricted = artwork.XRestrict != 0,
            IsBookmarked = artwork.IsBookmarked,
            IsMuted = artwork.IsMuted,
            IsVisible = artwork.Visible,
        };

        userDictionary.TryAdd(artwork.User.Id, artwork.User);

        if (artwork.Tags.Length > 0)
        {
            answer.Tags = new uint[artwork.Tags.Length];
            for (var i = 0; i < answer.Tags.Length; i++)
            {
                answer.Tags[i] = tagSet.Register(artwork.Tags[i].Name);
            }
        }

        if (artwork.Tools.Length > 0)
        {
            answer.Tools = new uint[artwork.Tools.Length];
            for (var i = 0; i < answer.Tools.Length; i++)
            {
                answer.Tools[i] = toolSet.Register(artwork.Tools[i]);
            }
        }

        var page = (artwork.MetaSinglePage.OriginalImageUrl ?? artwork.MetaPages[0].ImageUrls.Original).AsSpan();
        if (!TryParseDate(page, out answer.FileDate))
        {
            answer.IsOfficiallyRemoved = true;
            answer.FileDate = answer.CreateDate.ToLocalTime();
        }

        return answer;

        static bool TryParseDate(ReadOnlySpan<char> page, out DateTime dateTime)
        {
            Unsafe.SkipInit(out dateTime);
            page = page[..page.LastIndexOf('/')];
            var secondIndex = page.LastIndexOf('/');
            if (secondIndex == -1 || !byte.TryParse(page[(secondIndex + 1)..], out var second))
            {
                return false;
            }

            page = page[..secondIndex];
            var minuteIndex = page.LastIndexOf('/');
            if (minuteIndex == -1 || !byte.TryParse(page[(minuteIndex + 1)..], out var minute))
            {
                return false;
            }

            page = page[..minuteIndex];
            var hourIndex = page.LastIndexOf('/');
            if (hourIndex == -1 || !byte.TryParse(page[(hourIndex + 1)..], out var hour))
            {
                return false;
            }

            page = page[..hourIndex];
            var dayIndex = page.LastIndexOf('/');
            if (dayIndex == -1 || !byte.TryParse(page[(dayIndex + 1)..], out var day))
            {
                return false;
            }
            page = page[..dayIndex];
            var monthIndex = page.LastIndexOf('/');
            if (monthIndex == -1 || !byte.TryParse(page[(monthIndex + 1)..], out var month))
            {
                return false;
            }
            page = page[..monthIndex];
            var yearIndex = page.LastIndexOf('/');
            if (yearIndex == -1 || !uint.TryParse(page[(yearIndex + 1)..], out var year))
            {
                return false;
            }

            dateTime = new((int)year, month, day, hour, minute, second);
            return true;
        }
    }

    public override string ToString() => Id.ToString();

    public override int GetHashCode() => Id.GetHashCode();

    public bool Equals(Artwork? other) => ReferenceEquals(this, other) || (other is not null && Id == other.Id && UserId == other.UserId);

    public override bool Equals(object? obj) => Equals(obj as Artwork);

    private bool stringify = false;

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
            var segment = ArraySegmentFromPool.Rent(BinLength);
            var segmentSpan = segment.AsSpan();
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

            segment.Dispose();
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
    [StringLiteral.Utf8("gif")] private static partial ReadOnlySpan<byte> LiteralGif();
    [StringLiteral.Utf8("zip")] private static partial ReadOnlySpan<byte> LiteralZip();
    [StringLiteral.Utf8("bmp")] private static partial ReadOnlySpan<byte> LiteralBmp();

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

    public sealed class JsonFormatter : JsonConverter<Artwork>
    {
        public static readonly JsonFormatter Instance = new();

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
            if (value.stringify)
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
                FileExtensionKind.Gif => LiteralGif(),
                FileExtensionKind.Zip => LiteralZip(),
                FileExtensionKind.Bmp => LiteralBmp(),
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

            if (value.stringify)
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
