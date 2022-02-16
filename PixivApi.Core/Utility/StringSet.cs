namespace PixivApi;

[MessagePackFormatter(typeof(Formatter))]
public sealed class StringSet
{
    public readonly ConcurrentDictionary<uint, string> Values;
    public readonly ConcurrentDictionary<string, uint> Reverses;
    private uint index;

    public StringSet(int initialCapacity)
    {
        Values = new(Environment.ProcessorCount, initialCapacity);
        Reverses = new(Environment.ProcessorCount, initialCapacity);
    }

    public uint Register(string? text)
    {
        if (text is not { Length: > 0 })
        {
            return 0;
        }

        return Reverses.GetOrAdd(text, p =>
        {
            var incremented = Interlocked.Increment(ref index);
            Values.TryAdd(incremented, text);
            return incremented;
        });
    }

    public sealed class Formatter : IMessagePackFormatter<StringSet?>
    {
        public StringSet? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => DeserializeStatic(ref reader);

        public static StringSet? DeserializeStatic(ref MessagePackReader reader)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var length = reader.ReadMapHeader();
            var answer = new StringSet(length);
            for (int i = 0; i < length; i++)
            {
                var number = reader.ReadUInt32();
                var text = reader.ReadString();
                if (text is not { Length: > 0 } || number == 0)
                {
                    continue;
                }

                if (number > answer.index)
                {
                    answer.index = number;
                }

                answer.Values.TryAdd(number, text);
                answer.Reverses.TryAdd(text, number);
            }

            return answer;
        }

        public void Serialize(ref MessagePackWriter writer, StringSet? value, MessagePackSerializerOptions options) => SerializeStatic(ref writer, value);
        
        public static void SerializeStatic(ref MessagePackWriter writer, StringSet? value)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(value.Values.Count);
            foreach (var (number, text) in value.Values)
            {
                writer.Write(number);
                writer.Write(text);
            }
        }
    }
}
