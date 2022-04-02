namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    public static void AddSingleQuoteTextWithoutQuote(ref this Utf8ValueStringBuilder builder, string text)
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

    public static void AddName(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> origin, ReadOnlySpan<byte> name)
    {
        builder.AppendLiteral(origin);
        builder.AppendAscii('.');
        builder.AppendLiteral(name);
    }

    [StringLiteral.Utf8(" AND ")]
    public static partial ReadOnlySpan<byte> Literal_And();

    [StringLiteral.Utf8(" OR ")]
    public static partial ReadOnlySpan<byte> Literal_Or();

    public static void And(ref this Utf8ValueStringBuilder builder, ref bool and)
    {
        if (and)
        {
            builder.AppendLiteral(Literal_And());
        }
        else
        {
            and = true;
        }
    }

    [StringLiteral.Utf8("), (")]
    private static partial ReadOnlySpan<byte> Literal_ParenCommaParen();

    public static void AppendValues(ref this Utf8ValueStringBuilder builder, IEnumerable<uint> values)
    {
        var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }

        builder.AppendAscii('(');
        builder.Append(enumerator.Current);

        if (!enumerator.MoveNext())
        {
            goto END;
        }

        do
        {
            builder.AppendLiteral(Literal_ParenCommaParen());
            builder.Append(enumerator.Current);
        } while (enumerator.MoveNext());

    END:
        builder.AppendAscii(')');
    }

    public static void AppendValues(ref this Utf8ValueStringBuilder builder, IEnumerable<ulong> values)
    {
        builder.AppendAscii('(');
        var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            goto RETURN;
        }

        builder.Append(enumerator.Current);
        while (enumerator.MoveNext())
        {
            builder.AppendAscii(',');
            builder.Append(enumerator.Current);
        }

    RETURN:
        builder.AppendAscii(')');
    }

    [StringLiteral.Utf8(".\"Id\"")]
    public static partial ReadOnlySpan<byte> Literal_DotId();

    [StringLiteral.Utf8(" IN ")]
    public static partial ReadOnlySpan<byte> Literal_In();

    [StringLiteral.Utf8(" NOT ")]
    public static partial ReadOnlySpan<byte> Literal_Not();

    [StringLiteral.Utf8(" NOT IN ")]
    public static partial ReadOnlySpan<byte> Literal_NotIn();

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, IdFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.Ids is { Length: > 0 } ids)
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotId());
            builder.AppendLiteral(Literal_In());
            builder.AppendValues(ids);
        }

        if (filter.IgnoreIds is { Length: > 0 } ignoreIds)
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotId());
            builder.AppendLiteral(Literal_NotIn());
            builder.AppendValues(ignoreIds);
        }
    }

    [StringLiteral.Utf8(" = ")]
    public static partial ReadOnlySpan<byte> Literal_Equal();

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, TagFilter? filter, ReadOnlySpan<byte> table)
    {
        if (filter is null)
        {
            return;
        }

        throw new NotImplementedException();
    }

    [StringLiteral.Utf8(".\"HideReason\"")]
    private static partial ReadOnlySpan<byte> Literal_DotHideReason();

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, HideFilter? filter)
    {
        if (filter is null)
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotHideReason());
            builder.AppendLiteral(Literal_Equal());
            builder.AppendAscii('0');
        }
        else
        {
            if (filter.AllowedReason is { Count: > 0 } allow)
            {
                builder.And(ref and);
                builder.AppendLiteral(origin);
                builder.AppendLiteral(Literal_DotHideReason());
                builder.AppendLiteral(Literal_In());
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
                builder.AppendLiteral(Literal_DotHideReason());
                builder.AppendLiteral(Literal_NotIn());
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

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, bool? value, ReadOnlySpan<byte> name)
    {
        if (value is null)
        {
            return;
        }

        builder.And(ref and);
        builder.AddName(origin, name);
        builder.AppendLiteral(Literal_Equal());
        builder.AppendAscii(value.Value ? '1' : '0');
    }
}
