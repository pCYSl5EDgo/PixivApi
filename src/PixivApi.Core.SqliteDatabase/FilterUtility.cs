namespace PixivApi.Core.SqliteDatabase;

internal static class FilterUtility
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

    public static void AddName(ref this Utf8ValueStringBuilder builder, string origin, string name)
    {
        builder.Append("\""); builder.Append(origin); builder.Append("\".\"");
        builder.Append(name);
        builder.Append("\"");
    }

    public static void And(ref this Utf8ValueStringBuilder builder, ref bool and)
    {
        if (and)
        {
            builder.Append(" AND ");
        }
        else
        {
            and = true;
        }
    }

    public static void AppendValues(ref this Utf8ValueStringBuilder builder, IEnumerable<uint> values)
    {
        builder.Append("(");
        var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            goto RETURN;
        }

        builder.Append(enumerator.Current);
        while (enumerator.MoveNext())
        {
            builder.Append(", ");
            builder.Append(enumerator.Current);
        }

    RETURN:
        builder.AppendAscii(')');
    }

    public static void AppendValues(ref this Utf8ValueStringBuilder builder, IEnumerable<ulong> values)
    {
        builder.Append("(");
        var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            goto RETURN;
        }

        builder.Append(enumerator.Current);
        while (enumerator.MoveNext())
        {
            builder.Append(", ");
            builder.Append(enumerator.Current);
        }

    RETURN:
        builder.AppendAscii(')');
    }

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, string origin, IdFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.Ids is { Length: > 0 } ids)
        {
            builder.And(ref and);
            builder.Append("\""); builder.Append(origin); builder.Append("\".\"Id\" IN ");
            builder.AppendValues(ids);
        }

        if (filter.IgnoreIds is { Length: > 0 } ignoreIds)
        {
            builder.And(ref and);
            builder.Append("\""); builder.Append(origin); builder.Append("\".\"Id\" NOT IN ");
            builder.AppendValues(ignoreIds);
        }
    }

    private static void TagFilter(ref this Utf8ValueStringBuilder builder, string origin, string table, IEnumerable<uint> values, bool or)
    {
        if (or)
        {
            builder.Append("EXISTS ((SELECT \"TagTable\".\"TagId\" FROM \"");
            builder.Append(table);
            builder.Append("\" AS \"TagTable\" WHERE \"TagTable\".\"Id\" = \"");
            builder.Append(origin);
            builder.Append("\".\"Id\" AND \"TagTable\".\"ValueKind\" > 0) INTERSECT VALUES ");
            builder.AppendValues(values);
            builder.AppendAscii(')');
        }
        else
        {
            builder.Append("EXISTS (VALUES ");
            builder.AppendValues(values);
            builder.Append(" EXCEPT (SELECT \"TagTable\".\"TagId\" FROM \"");
            builder.Append(table);
            builder.Append("\" AS \"TagTable\" WHERE \"TagTable\".\"Id\" = \"");
            builder.Append(origin);
            builder.Append("\".\"Id\" AND \"TagTable\".\"ValueKind\" > 0))");
        }
    }

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, string origin, TagFilter? filter, string table)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.ExceptSet is { Count: > 0 } except)
        {
            builder.And(ref and);
            builder.Append("NOT ");
            builder.TagFilter(origin, table, except, filter.IgnoreOr);
        }

        if (filter.IntersectSet is { Count: > 0 } intersect)
        {
            builder.And(ref and);
            builder.TagFilter(origin, table, intersect, filter.Or);
        }
    }

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, string origin, HideFilter? filter)
    {
        if (filter is null)
        {
            builder.And(ref and);
            builder.Append("\""); builder.Append(origin); builder.Append("\".\"HideFilter\" = 0");
        }
        else
        {
            if (filter.AllowedReason is { Count: > 0 } allow)
            {
                builder.And(ref and);
                builder.Append("\""); builder.Append(origin); builder.Append("\".\"HideFilter\" IN (");
                using var enumerator = allow.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    builder.Append((byte)enumerator.Current);
                    while (enumerator.MoveNext())
                    {
                        builder.Append(", ");
                        builder.Append((byte)enumerator.Current);
                    }
                }

                builder.Append(")");
            }
            else if (filter.DisallowedReason is { Count: > 0 } disallow)
            {
                builder.And(ref and);
                builder.Append("\""); builder.Append(origin); builder.Append("\".\"HideFilter\" NOT IN (");
                using var enumerator = disallow.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    builder.Append((byte)enumerator.Current);
                    while (enumerator.MoveNext())
                    {
                        builder.Append(", ");
                        builder.Append((byte)enumerator.Current);
                    }
                }

                builder.Append(")");
            }
        }
    }

    public static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, string origin, bool? value, string name)
    {
        if (value is null)
        {
            return;
        }

        builder.And(ref and);
        builder.AddName(origin, name);
        builder.Append(" = ");
        builder.Append(value.Value ? 1 : 0);
    }
}
