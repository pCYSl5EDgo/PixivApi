namespace PixivApi.Core.Local;

[MessagePackFormatter(typeof(Formatter))]
public sealed class RankingSet : ConcurrentDictionary<RankingSet.Pair, ulong[]>
{
    public record struct Pair(DateOnly Date, RankingKind Kind) : IComparable<Pair>
    {
        public int CompareTo(Pair other)
        {
            var c = Date.CompareTo(other.Date);
            if (c != 0)
            {
                return c;
            }

            return ((byte)Kind).CompareTo((byte)other.Kind);
        }
    }

    public RankingSet() : base() { }

    private RankingSet(int capacity) : base(Environment.ProcessorCount, capacity) { }

    public sealed class Formatter : IMessagePackFormatter<RankingSet?>
    {
        public RankingSet? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => DeserializeStatic(ref reader);

        public static RankingSet? DeserializeStatic(ref MessagePackReader reader)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var header = reader.ReadMapHeader();
            var answer = new RankingSet(header);

            for (var i = 0; i < header; i++)
            {
                var arrayHeader = reader.ReadArrayHeader();
                DateOnly date = default;
                RankingKind kind = default;
                for (var j = 0; j < arrayHeader; j++)
                {
                    switch (j)
                    {
                        case 0:
                            date = DateOnly.FromDateTime(reader.ReadDateTime());
                            break;
                        case 1:
                            kind = (RankingKind)reader.ReadByte();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                var bytes = reader.ReadBytes() ?? default;
                var ids = bytes.IsEmpty ? Array.Empty<ulong>() : new ulong[bytes.Length >> 3];
                if (ids.Length > 0)
                {
                    bytes.CopyTo(MemoryMarshal.AsBytes(ids.AsSpan()));
                }

                answer.TryAdd(new(date, kind), ids);
            }

            return answer;
        }

        public void Serialize(ref MessagePackWriter writer, RankingSet? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
            }
            else
            {
                SerializeStatic(ref writer, value);
            }
        }

        public static void SerializeStatic(ref MessagePackWriter writer, RankingSet value)
        {
            writer.WriteMapHeader(value.Count);
            foreach (var ((date, kind), array) in value)
            {
                writer.WriteArrayHeader(2);
                writer.Write(date.ToDateTime(TimeOnly.MinValue));
                writer.Write((byte)kind);

                writer.WriteBinHeader(array.Length << 3);
                writer.WriteRaw(MemoryMarshal.AsBytes(array.AsSpan()));
            }
        }
    }
}
