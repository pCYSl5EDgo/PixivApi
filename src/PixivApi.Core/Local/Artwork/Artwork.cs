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

  public DateTime CreateDate;
  public DateTime FileDate;
  public uint[] Tags = [];
  public uint[]? ExtraTags;
  public uint[]? ExtraFakeTags;
  public uint[] Tools = [];
  public string Title = string.Empty;
  public string Caption = string.Empty;
  public string? ExtraMemo;
  public Dictionary<uint, HideReason>? ExtraPageHideReasonDictionary;
  public ushort[]? UgoiraFrames;

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

  public Dictionary<uint, uint> CalculateTags()
  {
    var dic = new Dictionary<uint, uint>();
    foreach (var item in ExtraTags.AsSpan())
    {
      dic.Add(item, 2);
    }

    foreach (var item in Tags)
    {
      CollectionsMarshal.GetValueRefOrAddDefault(dic, item, out _) = 1;
    }

    foreach (var item in ExtraFakeTags.AsSpan())
    {
      ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(dic, item, out var exists);
      if (exists)
      {
        if (value == 1)
        {
          value = 0;
        }
        else
        {
          dic.Remove(item);
        }
      }
    }

    return dic;
  }
  public IEnumerable<string>? StringifiedTags { get; private set; }

  public IEnumerable<string>? StringifiedTools { get; private set; }

  public string? UserName { get; private set; }

  public async ValueTask StringifyAsync(IUserDatabase userDatabase, ITagDatabase tagDatabase, IToolDatabase toolDatabase, CancellationToken token)
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

    if (set.Count == 0)
    {
      StringifiedTags = Array.Empty<string>();
    }
    else
    {
      var tagSet = new HashSet<string>(set.Count);
      foreach (var item in set)
      {
        var tag = await tagDatabase.GetTagAsync(item, token).ConfigureAwait(false);
        if (string.IsNullOrEmpty(tag))
        {
          continue;
        }

        tagSet.Add(tag);
      }

      StringifiedTags = tagSet;
    }

    if (Tools.Length == 0)
    {
      StringifiedTools = Array.Empty<string>();
    }
    else
    {
      var toolSet = new HashSet<string>(Tools.Length);
      foreach (var item in Tools)
      {
        var tool = await toolDatabase.GetToolAsync(item, token).ConfigureAwait(false);
        if (string.IsNullOrEmpty(tool))
        {
          continue;
        }

        toolSet.Add(tool);
      }

      StringifiedTools = toolSet;
    }

    UserName = (await userDatabase.GetUserAsync(UserId, token).ConfigureAwait(false))?.Name;
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
            }
            break;
          case 0x01:
            answer.CreateDate = reader.ReadDateTime();
            break;
          case 0x02:
            answer.FileDate = reader.ReadDateTime();
            break;
          case 0x03:
            answer.Tags = ReadUInt32Array(ref reader) ?? [];
            break;
          case 0x04:
            answer.ExtraTags = ReadUInt32Array(ref reader);
            break;
          case 0x05:
            answer.ExtraFakeTags = ReadUInt32Array(ref reader);
            break;
          case 0x06:
            answer.Tools = ReadUInt32Array(ref reader) ?? [];
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
        return [];
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
          return [];
        }

        if (mapHeader == 1)
        {
          var count = reader.ReadInt32();
          if (count == 0)
          {
            return [];
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
            return [];
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
          return [];
        }

        answer = new ushort[mapHeader];
        sequence.CopyTo(MemoryMarshal.AsBytes(answer.AsSpan()));
      }

      return answer;
    }
  }

  public bool IsNotHided(uint pageIndex)
  {
    if (ExtraHideReason != HideReason.NotHidden)
    {
      return false;
    }

    if (ExtraPageHideReasonDictionary is { Count: > 0 } dictionary && dictionary.TryGetValue(pageIndex, out var reason) && reason != HideReason.NotHidden)
    {
      return false;
    }

    return true;
  }

  public struct PageIndexEnumerator : IEnumerator<uint>
  {
    private readonly int maxExclusive;
    private readonly Dictionary<uint, HideReason>? dictionary;
    private int index;

    public PageIndexEnumerator(Artwork artwork)
    {
      maxExclusive = (int)artwork.PageCount;
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
      HideReason.NotHidden => "not-hidden"u8,
      HideReason.TemporaryHidden => "temporary-hidden"u8,
      HideReason.LowQuality => "low-quality"u8,
      HideReason.Irrelevant => "irrelevant"u8,
      HideReason.ExternalLink => "external-link"u8,
      HideReason.Dislike => "dislike"u8,
      HideReason.Crop => "crop"u8,
      _ => throw new InvalidDataException(hideReason.ToString()),
    };

    public override void Write(Utf8JsonWriter writer, Artwork value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      writer.WriteNumber("id"u8, value.Id);
      writer.WriteString("title"u8, value.Title);

      writer.WriteNumber("user-id"u8, value.UserId);
      if (value.IsStringified)
      {
        writer.WriteString("user-name"u8, value.UserName);
      }

      writer.WriteNumber("total-view"u8, value.TotalView);
      writer.WriteNumber("total-bookmarks"u8, value.TotalBookmarks);

      writer.WriteNumber("page-count"u8, value.PageCount);
      writer.WriteNumber("width"u8, value.Width);
      writer.WriteNumber("height"u8, value.Height);

      writer.WritePropertyName("create-date"u8);
      writer.WriteStringValue(value.CreateDate);
      if (value.FileDate != value.CreateDate.ToLocalTime())
      {
        writer.WritePropertyName("file-date"u8);
        writer.WriteStringValue(value.FileDate);
      }

      writer.WriteString("type"u8, value.Type switch
      {
        ArtworkType.Illust => "illust"u8,
        ArtworkType.Manga => "manga"u8,
        ArtworkType.Ugoira => "ugoira"u8,
        ArtworkType.None or _ => "none"u8,
      });

      writer.WriteString("extension"u8, value.Extension switch
      {
        FileExtensionKind.Jpg => "jpg"u8,
        FileExtensionKind.Png => "png"u8,
        FileExtensionKind.Zip => "zip"u8,
        FileExtensionKind.Gif => "gif"u8,
        FileExtensionKind.None or _ => "none"u8,
      });

      writer.WriteBoolean("officially-removed"u8, value.IsOfficiallyRemoved);
      writer.WriteBoolean("r18"u8, value.IsXRestricted);
      writer.WriteBoolean("bookmarked"u8, value.IsBookmarked);
      writer.WriteBoolean("visible"u8, value.IsVisible);
      writer.WriteBoolean("muted"u8, value.IsMuted);

      if (value.ExtraHideReason != HideReason.NotHidden)
      {
        writer.WriteString("hide-reason"u8, GetLiteral(value.ExtraHideReason));
      }

      if (value.ExtraPageHideReasonDictionary is { Count: > 0 } dictionary)
      {
        writer.WriteStartObject("page-hide-reason-dictionary"u8);
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
          writer.WriteNull("ugoira-frames"u8);
        }
        else
        {
          writer.WriteStartArray("ugoira-frames"u8);
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
          writer.WriteStartArray("tags"u8);
          foreach (var tag in tags)
          {
            writer.WriteStringValue(tag);
          }
          writer.WriteEndArray();
        }

        if (value.StringifiedTools is { } tools)
        {
          writer.WriteStartArray("tools"u8);
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
          writer.WriteStartArray("tags"u8);
          foreach (var tag in tags)
          {
            writer.WriteNumberValue(tag);
          }
          writer.WriteEndArray();
        }

        if (value.ExtraTags is { Length: > 0 } extraTags)
        {
          writer.WriteStartArray("extra-tags"u8);
          foreach (var tag in extraTags)
          {
            writer.WriteNumberValue(tag);
          }
          writer.WriteEndArray();
        }

        if (value.ExtraFakeTags is { Length: > 0 } extraFakeTags)
        {
          writer.WriteStartArray("fake-tags"u8);
          foreach (var tag in extraFakeTags)
          {
            writer.WriteNumberValue(tag);
          }
          writer.WriteEndArray();
        }

        if (value.Tools is { Length: > 0 } tools)
        {
          writer.WriteStartArray("tools"u8);
          foreach (var tool in tools)
          {
            writer.WriteNumberValue(tool);
          }
          writer.WriteEndArray();
        }
      }

      writer.WriteString("caption"u8, value.Caption);

      if (!string.IsNullOrWhiteSpace(value.ExtraMemo))
      {
        writer.WriteString("memo"u8, value.ExtraMemo);
      }

      writer.WriteEndObject();
    }
  }
}
