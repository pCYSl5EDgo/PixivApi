using System.Buffers.Binary;

namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
public sealed class Artwork : IOverwrite<Artwork>, IEquatable<Artwork>
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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public uint[]? ExtraTags;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public uint[]? ExtraFakeTags;
    public uint[] Tools = Array.Empty<uint>();
    public string Title = string.Empty;
    public string Caption = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public string? ExtraMemo;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public Dictionary<uint, HideReason>? ExtraPageHideReasonDictionary;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public ushort[]? UgoiraFrames;

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
        AddOriginalFileName(pageIndex, ref handler);
        return handler.ToStringAndClear();
    }

    public string GetThumbnailUrl()
    {
        DefaultInterpolatedStringHandler handler = $"https://i.pximg.net/c/360x360_70/img-master/img/";
        AddDateToUrl(ref handler);
        handler.AppendFormatted('/');
        AddThumbnailFileName(ref handler);
        return handler.ToStringAndClear();
    }

    public void AddOriginalFileName(uint pageIndex, ref DefaultInterpolatedStringHandler handler)
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
        AddOriginalFileName(pageIndex, ref handler);
        return handler.ToStringAndClear();
    }

    public void AddThumbnailFileName(ref DefaultInterpolatedStringHandler handler)
    {
        handler.AppendFormatted(Id);
        if (Type == ArtworkType.Ugoira)
        {
            handler.AppendLiteral("_square1200.jpg");
        }
        else
        {
            handler.AppendLiteral("_p0_square1200.jpg");
        }
    }

    public string GetThumbnailFileName()
    {
        DefaultInterpolatedStringHandler handler = new();
        AddThumbnailFileName(ref handler);
        return handler.ToStringAndClear();
    }

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
        };

        userDictionary.TryAdd(artwork.User.Id, artwork.User);

        if (artwork.Tags.Length > 0)
        {
            answer.Tags = new uint[artwork.Tags.Length];
            for (int i = 0; i < answer.Tags.Length; i++)
            {
                answer.Tags[i] = tagSet.Register(artwork.Tags[i].Name);
            }
        }

        if (artwork.Tools.Length > 0)
        {
            answer.Tools = new uint[artwork.Tools.Length];
            for (int i = 0; i < answer.Tools.Length; i++)
            {
                answer.Tools[i] = toolSet.Register(artwork.Tools[i]);
            }
        }

        var page = (artwork.MetaSinglePage.OriginalImageUrl ?? artwork.MetaPages[0].ImageUrls.Original).AsSpan();
        if (!TryParseDate(page, out answer.FileDate))
        {
            answer.IsOfficiallyRemoved = true;
            answer.FileDate = answer.CreateDate;
        }

        return answer;

        static bool TryParseDate(ReadOnlySpan<char> page, out DateTime dateTime)
        {
            Unsafe.SkipInit(out dateTime);
            page = page[..page.LastIndexOf('/')];
            var secondIndex = page.LastIndexOf('/');
            if (secondIndex == -1 || !byte.TryParse(page[(secondIndex + 1)..], out byte second))
            {
                return false;
            }

            page = page[..secondIndex];
            var minuteIndex = page.LastIndexOf('/');
            if (minuteIndex == -1 || !byte.TryParse(page[(minuteIndex + 1)..], out byte minute))
            {
                return false;
            }

            page = page[..minuteIndex];
            var hourIndex = page.LastIndexOf('/');
            if (hourIndex == -1 || !byte.TryParse(page[(hourIndex + 1)..], out byte hour))
            {
                return false;
            }

            page = page[..hourIndex];
            var dayIndex = page.LastIndexOf('/');
            if (dayIndex == -1 || !byte.TryParse(page[(dayIndex + 1)..], out byte day))
            {
                return false;
            }
            page = page[..dayIndex];
            var monthIndex = page.LastIndexOf('/');
            if (monthIndex == -1 || !byte.TryParse(page[(monthIndex + 1)..], out byte month))
            {
                return false;
            }
            page = page[..monthIndex];
            var yearIndex = page.LastIndexOf('/');
            if (yearIndex == -1 || !uint.TryParse(page[(yearIndex + 1)..], out uint year))
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

    public sealed class Formatter : IMessagePackFormatter<Artwork>
    {
        const int BinLength = sizeof(ulong) * 4 + sizeof(uint) * 3 + sizeof(ArtworkType) + sizeof(FileExtensionKind) + sizeof(HideReason) + 1;

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

        public Artwork Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => DeserializeStatic(ref reader, options);

        public static Artwork DeserializeStatic(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (!reader.TryReadArrayHeader(out var header))
            {
                return new();
            }

            var answer = new Artwork();
            var segment = ArraySegmentFromPool.Rent(BinLength);
            var segmentSpan = segment.AsSpan();
            for (int i = 0; i < header; i++)
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
                for (int i = 0; i < length; i += 5)
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
                int tmpIndex = 0;
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
            writer.WriteBinHeader(value is null ? 0 : value.Length << 1);
            if (value is not { Length: > 0 })
            {
                return;
            }

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
            var bytes = reader.ReadBytes();
            if (!bytes.HasValue)
            {
                return null;
            }

            var sequence = bytes.Value;
            var length = sequence.Length >> 1;
            if (length == 0)
            {
                return Array.Empty<ushort>();
            }

            var answer = new ushort[length];
            sequence.CopyTo(MemoryMarshal.AsBytes(answer.AsSpan()));
            return answer;
        }
    }
}
