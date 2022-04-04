namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    [StringLiteral.Utf8("WITH ")]
    private static partial ReadOnlySpan<byte> Literal_With();

    [StringLiteral.Utf8(", ")]
    private static partial ReadOnlySpan<byte> Literal_CommaSpace();

    [StringLiteral.Utf8(" (\"Id\") AS (")]
    private static partial ReadOnlySpan<byte> Literal_ParenIdParenAs();

    [StringLiteral.Utf8("VALUES ")]
    private static partial ReadOnlySpan<byte> Literal_Values();

    private static void WithOrComma(ref this Utf8ValueStringBuilder builder, ref bool first)
    {
        if (first)
        {
            builder.AppendLiteral(Literal_With());
            first = false;
        }
        else
        {
            builder.AppendLiteral(Literal_CommaSpace());
        }
    }

    private static void Add(ref this Utf8ValueStringBuilder builder, byte alias, int index)
    {
        var span = builder.GetSpan(2);
        span[0] = (byte)'"';
        span[1] = alias;
        builder.Advance(2);
        builder.Append(index);
        builder.GetSpan(1)[0] = (byte)'"';
        builder.Advance(1);
    }

    private static void Add(ref this Utf8ValueStringBuilder builder, byte b0, byte b1, int index)
    {
        var span = builder.GetSpan(3);
        span[0] = (byte)'"';
        span[1] = b0;
        span[2] = b1;
        builder.Advance(3);
        builder.Append(index);
        builder.AppendAscii('"');
    }

    private const byte I = (byte)'I';
    private const byte E = (byte)'E';

    private static void Preproces(ref this Utf8ValueStringBuilder builder, IdFilter? filter, ref bool first, ref int intersect, ref int except)
    {
        if (filter is null)
        {
            return;
        }

        static void Add(ref Utf8ValueStringBuilder builder, ref bool first, ref int index, ulong[] ids, byte alias)
        {
            builder.WithOrComma(ref first);
            builder.Add(alias, ++index);
            builder.AppendLiteral(Literal_ParenIdParenAs());
            builder.AppendLiteral(Literal_Values());
            builder.AppendAscii('(');
            builder.Append(ids[0]);
            for (var i = 1; i < ids.Length; i++)
            {
                builder.AppendLiteral(Literal_ParenCommaParen());
                builder.Append(ids[i]);
            }

            builder.AppendLiteral(Literal_RRParen());
        }

        if (filter.Ids is { Length: > 0 })
        {
            Add(ref builder, ref first, ref intersect, filter.Ids, I);
        }

        if (filter.IgnoreIds is { Length: > 0 } excepts)
        {
            if (intersect == -1)
            {
                Add(ref builder, ref first, ref except, filter.IgnoreIds, E);
            }
            else
            {
                builder.WithOrComma(ref first);
                builder.Add(I, ++intersect);
                builder.AppendLiteral(Literal_ParenIdParenAs());
                builder.Add(I, intersect - 1);
                builder.AppendLiteral(Literal_Except());
                builder.AppendLiteral(Literal_Values());
                builder.AppendAscii('(');
                builder.Append(excepts[0]);
                for (var i = 1; i < excepts.Length; i++)
                {
                    builder.AppendLiteral(Literal_ParenCommaParen());
                    builder.Append(excepts[i]);
                }

                builder.AppendLiteral(Literal_RRParen());
            }
        }
    }
}
