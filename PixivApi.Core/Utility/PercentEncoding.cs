using System.Text;

namespace PixivApi;

public readonly struct PercentEncoding : ISpanFormattable, IEquatable<PercentEncoding>
{
    private readonly string text;

    public PercentEncoding(string text)
    {
        this.text = text;
    }

    public bool Equals(PercentEncoding other) => text.Equals(other.text);

    public string ToString(string? format, IFormatProvider? formatProvider) => $"{text}";

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = 0;
        var enumerator = text.AsSpan().EnumerateRunes();
        static char CalcNumber(int v)
        {
            if (v >= 10)
            {
                return (char)('A' - 10 + v);
            }

            return (char)('0' + v);
        }

        static (char, char) Calc(Rune rune)
        {
            var c = rune.Value;
            return (CalcNumber(c >> 4), CalcNumber(c & 15));
        }

        uint bytes = 0;
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref bytes, 1));
        while (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            if (c.IsAscii)
            {
                switch (c.Value)
                {
                    case ':':
                    case ';':
                    case ' ':
                    case '%':
                    case '=':
                    case '+':
                    case '*':
                    case '(':
                    case ')':
                    case '\'':
                    case '"':
                    case '&':
                    case '$':
                    case '!':
                    case '@':
                    case '[':
                    case ']':
                    case '#':
                    case '?':
                    case '/':
                        if (destination.Length < 3)
                        {
                            return false;
                        }

                        charsWritten += 3;
                        destination[0] = '%';
                        (destination[1], destination[2]) = Calc(c);
                        destination = destination[3..];
                        continue;
                    default:
                        if (destination.IsEmpty)
                        {
                            return false;
                        }

                        charsWritten++;
                        destination[0] = (char)c.Value;
                        continue;
                }
            }

            var length = c.EncodeToUtf8(span);
            var written = length * 3;
            charsWritten += written;
            if (destination.Length < written)
            {
                return false;
            }

            var tmp = span[..length];
            foreach (byte v in tmp)
            {
                destination[0] = '%';
                destination[1] = CalcNumber(v >> 4);
                destination[2] = CalcNumber(v & 15);
                destination = destination[3..];
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is PercentEncoding other && Equals(other);

    public static bool operator ==(PercentEncoding left, PercentEncoding right) => left.Equals(right);

    public static bool operator !=(PercentEncoding left, PercentEncoding right) => !(left == right);

    public override int GetHashCode() => text.GetHashCode();
}
