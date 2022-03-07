using Cysharp.Text;

namespace PixivApi.Site;

public static class FileUriUtility
{
    public static void Convert(ref Utf8ValueStringBuilder builder, ReadOnlySpan<char> path)
    {
        builder.Clear();
        builder.AppendLiteral(LiteralUtility.LiteralQuoteFile());
        var enumerator = path.EnumerateRunes();
        while (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            switch (c.Value)
            {
                case '\\':
                    builder.GetSpan(1)[0] = (byte)'/';
                    builder.Advance(1);
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.AppendLiteral(LiteralUtility.LiteralQuote());
    }
}
