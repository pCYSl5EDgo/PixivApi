namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    private static void AddSingleQuoteTextWithoutQuote(ref this Utf8ValueStringBuilder builder, string text)
    {
        const byte special = (byte)'\'';
        var enumerator = text.EnumerateRunes();
        while (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            if (c.Value == '\'')
            {
                var span = builder.GetSpan(2);
                span[1] = span[0] = special;
                builder.Advance(2);
            }
            else
            {
                builder.Advance(c.EncodeToUtf8(builder.GetSpan(4)));
            }
        }
    }

    public static void AppendAscii(ref this Utf8ValueStringBuilder builder, char value)
    {
        builder.GetSpan(1)[0] = (byte)value;
        builder.Advance(1);
    }

    private static void AddName(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> origin, ReadOnlySpan<byte> name)
    {
        builder.AppendLiteral(origin);
        builder.AppendAscii('.');
        builder.AppendLiteral(name);
    }

    private static void And(ref this Utf8ValueStringBuilder builder, ref bool and)
    {
        if (and)
        {
            builder.AppendLiteral(" AND "u8);
        }
        else
        {
            and = true;
        }
    }

    private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, HideFilter? filter)
    {
        if (filter is null)
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(".\"HideReason\" = "u8);
            builder.AppendAscii('0');
        }
        else
        {
            if (filter.AllowedReason is { Count: > 0 } allow)
            {
                builder.And(ref and);
                builder.AppendLiteral(origin);
                builder.AppendLiteral(".\"HideReason\" IN "u8);
                builder.AppendAscii('(');
                using var enumerator = allow.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    builder.Append((byte)enumerator.Current);
                    while (enumerator.MoveNext())
                    {
                        builder.AppendAscii(',');
                        builder.Append((byte)enumerator.Current);
                    }
                }

                builder.AppendAscii(')');
            }
            else if (filter.DisallowedReason is { Count: > 0 } disallow)
            {
                builder.And(ref and);
                builder.AppendLiteral(origin);
                builder.AppendLiteral(".\"HideReason\" NOT IN "u8);
                builder.AppendAscii('(');
                using var enumerator = disallow.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    builder.Append((byte)enumerator.Current);
                    while (enumerator.MoveNext())
                    {
                        builder.AppendAscii(',');
                        builder.Append((byte)enumerator.Current);
                    }
                }

                builder.AppendAscii(')');
            }
        }
    }

    private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, bool? value, ReadOnlySpan<byte> name)
    {
        if (value is null)
        {
            return;
        }

        builder.And(ref and);
        builder.AddName(origin, name);
        builder.AppendLiteral(" = "u8);
        builder.AppendAscii(value.Value ? '1' : '0');
    }

    private static void FilterInOrNotIn(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, byte aliasIntersect, byte aliasExcept, int intersect, int except)
    {
        if (intersect == -1)
        {
            if (except == -1)
            {
                return;
            }

            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(".\"Id\" NOT IN "u8);
            builder.Add(aliasExcept, except);
        }
        else
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(".\"Id\" IN "u8);
            builder.Add(aliasIntersect, intersect);
        }
    }
}
