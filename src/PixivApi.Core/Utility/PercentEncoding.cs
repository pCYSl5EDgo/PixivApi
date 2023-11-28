using System.Text;

namespace PixivApi.Core;

public readonly struct PercentEncoding : ISpanFormattable
{
  private readonly string text;

  public PercentEncoding(string text)
  {
    this.text = text;
  }

  public bool Equals(PercentEncoding other) => text.Equals(other.text);

  public string ToString(string? format, IFormatProvider? formatProvider) => $"{text}";

  public static void Encode(ref Utf8ValueStringBuilder builder, SpanRuneEnumerator enumerator)
  {
    Span<byte> span = stackalloc byte[4];
    while (enumerator.MoveNext())
    {
      Encode(ref builder, enumerator.Current, span);
    }
  }

  public static void Encode(ref Utf8ValueStringBuilder builder, Rune c) => Encode(ref builder, c, stackalloc byte[4]);

  public static void Encode(ref Utf8ValueStringBuilder builder, ReadOnlySpan<char> text) => Encode(ref builder, text.EnumerateRunes());

  private static void Encode(ref Utf8ValueStringBuilder builder, Rune c, Span<byte> span)
  {
    Span<byte> destination;
    var value = c.Value;
    if (c.IsAscii)
    {
      switch (value)
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
          destination = builder.GetSpan(3);
          destination[0] = (byte)'%';
          (destination[1], destination[2]) = CalcBytePair(value);
          builder.Advance(3);
          return;
        default:
          if (value < 32)
          {
            destination = builder.GetSpan(3);
            destination[0] = (byte)'%';
            if (value < 16)
            {
              destination[1] = (byte)'0';
            }
            else
            {
              destination[1] = (byte)'1';
              value ^= 16;
            }

            destination[2] = CalcNumberByte(value);
            builder.Advance(3);
          }
          else
          {
            destination = builder.GetSpan(1);
            destination[0] = (byte)value;
            builder.Advance(1);
          }

          return;
      }
    }

    var length = c.EncodeToUtf8(span);
    var written = length * 3;
    destination = builder.GetSpan(written);
    var tmp = span[..length];
    foreach (var v in tmp)
    {
      destination[0] = (byte)'%';
      destination[1] = CalcNumberByte(v >> 4);
      destination[2] = CalcNumberByte(v & 15);
      destination = destination[3..];
    }

    builder.Advance(written);
  }

  public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
  {
    charsWritten = 0;
    var enumerator = text.AsSpan().EnumerateRunes();
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
            (destination[1], destination[2]) = CalcCharPair(c.Value);
            destination = destination[3..];
            continue;
          default:
            if (destination.IsEmpty)
            {
              return false;
            }

            charsWritten++;
            destination[0] = (char)c.Value;
            destination = destination[1..];
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
      foreach (var v in tmp)
      {
        destination[0] = '%';
        destination[1] = CalcNumberChar(v >> 4);
        destination[2] = CalcNumberChar(v & 15);
        destination = destination[3..];
      }
    }

    return true;
  }

  private static char CalcNumberChar(int v)
  {
    if (v >= 10)
    {
      return (char)('A' - 10 + v);
    }

    return (char)('0' + v);
  }

  private static byte CalcNumberByte(int v)
  {
    if (v >= 10)
    {
      return (byte)('A' - 10 + v);
    }

    return (byte)('0' + v);
  }

  private static (char, char) CalcCharPair(int value) => (CalcNumberChar(value >> 4), CalcNumberChar(value & 15));

  private static (byte, byte) CalcBytePair(int value) => (CalcNumberByte(value >> 4), CalcNumberByte(value & 15));
}
