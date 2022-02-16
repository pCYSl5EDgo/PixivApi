namespace PixivApi;

[MessagePackFormatter(typeof(Formatter))]
public class StringCompareInfo : IEqualityComparer<string>
{
    private readonly CompareInfo? compareInfo;
    private readonly CompareOptions compareOptions;
    private readonly StringComparison stringComparison;

    public StringCompareInfo(CultureInfo? cultureInfo, bool ignoreCase)
    {
        compareInfo = cultureInfo?.CompareInfo;
        compareOptions = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
        stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    public StringCompareInfo(string? culture, bool ignoreCase) : this(culture switch
    {
        null => CultureInfo.CurrentCulture,
        "" => CultureInfo.InvariantCulture,
        "ordinal" => null,
        _ => CultureInfo.GetCultureInfo(culture, true),
    }, ignoreCase)
    {
    }

    public bool Equals(string? x, string? y) => ReferenceEquals(x, y) || (x is not null && y is not null && (compareInfo is null ? x.Equals(y, stringComparison) : compareInfo.Compare(x, y, compareOptions) == 0));

    public bool Contains(string container, string value) => compareInfo is null ? container.Contains(value, stringComparison) : compareInfo.IndexOf(container, value, compareOptions) != -1;

    public bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => compareInfo is null ? x.Equals(y, stringComparison) : compareInfo.Compare(x, y, compareOptions) == 0;

    public bool Contains(ReadOnlySpan<char> container, ReadOnlySpan<char> value) => compareInfo is null ? container.Contains(value, stringComparison) : compareInfo.IndexOf(container, value, compareOptions) != -1;

    public int GetHashCode([DisallowNull] string obj) => obj.Length;

    public sealed class Formatter : IMessagePackFormatter<StringCompareInfo?>
    {
        public StringCompareInfo? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => DeserializeStatic(ref reader);

        public static StringCompareInfo? DeserializeStatic(ref MessagePackReader reader)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var header = reader.ReadMapHeader();
            if (header == 0)
            {
                return new(default(CultureInfo), false);
            }

            var culture = reader.ReadString();
            var ignoreCase = reader.ReadBoolean();
            header = (header - 1) << 1;
            for (int i = 0; i < header; i++)
            {
                reader.Skip();
            }

            return new(culture, ignoreCase);
        }

        public void Serialize(ref MessagePackWriter writer, StringCompareInfo? value, MessagePackSerializerOptions options) => SerializeStatic(ref writer, value);

        public static void SerializeStatic(ref MessagePackWriter writer, StringCompareInfo? value)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(1);
            writer.Write(value.compareInfo?.Name);
            writer.Write(value.compareOptions == CompareOptions.IgnoreCase);
        }
    }
}
