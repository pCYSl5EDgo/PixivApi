namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    private static (string[], int) SplitLongerThan2(string[] array)
    {
        var answer = ArrayPool<string>.Shared.Rent(array.Length);
        var longerThan2 = 0;
        foreach (var item in array)
        {
            if (item.Length > 2)
            {
                answer[longerThan2++] = item;
            }
        }

        if (longerThan2 < array.Length)
        {
            var index = longerThan2;
            foreach (var item in array)
            {
                if (item.Length <= 2)
                {
                    answer[index++] = item;
                }
            }
        }

        return (answer, longerThan2);
    }

    private static void Preprocess(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, bool or, string[]? exacts, string[]? partials, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (exacts is { Length: > 0 })
        {
            if (or)
            {
                builder.PreprocessArtworkOr(ref first, alias, ref index, exacts, parts0, parts1);
            }
            else
            {
                builder.PreprocessArtworkAnd(ref first, alias, ref index, exacts, parts0, parts1);
            }
        }

        if (partials is { Length: > 0 })
        {
            var (array, longerThan2) = SplitLongerThan2(partials);
            if (or)
            {
                builder.PreprocessArtworkOr(ref first, alias, ref index, array.AsSpan(0, longerThan2), array.AsSpan(longerThan2, partials.Length - longerThan2), parts0, parts1);
            }
            else
            {
                builder.PreprocessArtworkAnd(ref first, alias, ref index, array.AsSpan(0, longerThan2), array.AsSpan(longerThan2, partials.Length - longerThan2), parts0, parts1);
            }

            ArrayPool<string>.Shared.Return(array);
        }
    }

    private static void Preprocess(ref this Utf8ValueStringBuilder builder, TagFilter? filter, byte intersectAlias, byte exceptAlias, ref bool first, ref int intersect, ref int except, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (filter is null)
        {
            return;
        }

        builder.Preprocess(ref first, intersectAlias, ref intersect, filter.Or, filter.Exacts, filter.Partials, parts0, parts1);

        if (intersect == -1)
        {
            builder.Preprocess(ref first, exceptAlias, ref except, filter.IgnoreOr, filter.IgnoreExacts, filter.IgnorePartials, parts0, parts1);
        }
        else
        {
            builder.PreprocessExcept(intersectAlias, exceptAlias, ref first, ref intersect, ref except, filter.IgnoreOr, filter.IgnoreExacts, filter.IgnorePartials, parts0, parts1);
        }
    }

    [StringLiteral.Utf8("SELECT \"CT\".\"Id\" FROM \"ArtworkTagCrossTable\" AS \"CT\"")]
    private static partial ReadOnlySpan<byte> Literal_SelectIdFromArtworkTagCrossTableAsCT();

    [StringLiteral.Utf8(" WHERE \"CT\".\"ValueKind\" <> 0 AND ")]
    private static partial ReadOnlySpan<byte> Literal_WhereCTDotValueKindNotEqual0And();

    [StringLiteral.Utf8("\"TT\".\"Value\"")]
    private static partial ReadOnlySpan<byte> Literal_TTDotValue();

    [StringLiteral.Utf8("\"CT\".\"Id\" IN ")]
    private static partial ReadOnlySpan<byte> Literal_CTDotIdIn();

    [StringLiteral.Utf8("\"CT\".\"TagId\" IN ")]
    private static partial ReadOnlySpan<byte> Literal_CTDotTagIdIn();

    [StringLiteral.Utf8(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\"")]
    private static partial ReadOnlySpan<byte> Literal_InnerJoinTagTableAsTTOnTagId();

    private static void PreprocessArtworkAnd(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        foreach (var item in exacts)
        {
            builder.WithOrComma(ref first);
            builder.Add(alias, ++index);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
            builder.AppendLiteral(parts1);
            if (index != 0)
            {
                builder.AppendLiteral(Literal_CTDotIdIn());
                builder.Add(alias, index - 1);
                builder.AppendLiteral(Literal_And());
            }

            builder.AppendLiteral(Literal_TTDotValue());
            builder.AppendLiteral(Literal_Equal());
            builder.AddSingleQuoteText(item);

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessArtworkOr(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        builder.WithOrComma(ref first);
        builder.Add(alias, ++index);
        builder.AppendLiteral(Literal_ParenIdParenAs());

        builder.AppendLiteral(parts0);
        builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
        builder.AppendLiteral(parts1);
        if (index != 0)
        {
            builder.AppendLiteral(Literal_CTDotIdIn());
            builder.Add(alias, index - 1);
            builder.AppendLiteral(Literal_And());
        }

        builder.AppendLiteral(Literal_TTDotValue());
        builder.AppendLiteral(Literal_In());
        builder.AppendAscii('(');
        for (var i = 0; i < exacts.Length; i++)
        {
            if (i != 0)
            {
                builder.AppendAscii(',');
            }

            builder.AddSingleQuoteText(exacts[i]);

        }

        builder.AppendLiteral(Literal_RRParen());
    }

    private static int StringLengthReverseCompare(string x, string y) => y.Length.CompareTo(x.Length);

    [StringLiteral.Utf8("(\"Id\") AS (SELECT \"rowid\" FROM \"TagTextTable\"('")]
    private static partial ReadOnlySpan<byte> Literal_ParenIdParenAsSelectRowIdFromTagTextTableMatch();

    [StringLiteral.Utf8("')), ")]
    private static partial ReadOnlySpan<byte> Literal_QuoteRRParenCommaSpace();

    private static void PreprocessArtworkAnd(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        match.Sort(StringLengthReverseCompare);
        foreach (var item in match)
        {
            // match temp table
            builder.WithOrComma(ref first);
            builder.Add(alias, alias, ++index);
            builder.AppendLiteral(Literal_ParenIdParenAsSelectRowIdFromTagTextTableMatch());
            builder.AddDoubleQuoteText(item);
            builder.AppendLiteral(Literal_QuoteRRParenCommaSpace());

            // main
            builder.Add(alias, index);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(parts1);
            if (index != 0)
            {
                builder.AppendLiteral(Literal_CTDotIdIn());
                builder.Add(alias, index - 1);
                builder.AppendLiteral(Literal_And());
            }

            builder.AppendLiteral(Literal_CTDotTagIdIn());
            builder.Add(alias, alias, index);

            builder.AppendAscii(')');
        }

        like.Sort(StringLengthReverseCompare);
        foreach (var item in like)
        {
            builder.WithOrComma(ref first);
            builder.Add(alias, ++index);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
            builder.AppendLiteral(parts1);
            if (index != 0)
            {
                builder.AppendLiteral(Literal_CTDotIdIn());
                builder.Add(alias, index - 1);
                builder.AppendLiteral(Literal_And());
            }

            builder.AppendLiteral(Literal_TTDotValue());
            builder.AppendLiteral(Literal_LikeQuotePercent());
            builder.AddSingleQuoteTextWithoutQuote(item);
            var span = builder.GetSpan(3);
            span[0] = (byte)'%';
            span[1] = (byte)'\'';
            span[2] = (byte)')';
            builder.Advance(3);

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessArtworkOr(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (match.Length > 0)
        {
            match.Sort(StringLengthReverseCompare);
            // match temp table
            builder.WithOrComma(ref first);
            builder.Add(alias, alias, ++index);
            builder.AppendLiteral(Literal_ParenIdParenAsSelectRowIdFromTagTextTableMatch());
            builder.AddDoubleQuoteText(match[0]);
            for (var i = 1; i < match.Length; i++)
            {
                builder.AppendLiteral(Literal_Or());
                builder.AddDoubleQuoteText(match[i]);
            }

            builder.AppendLiteral(Literal_QuoteRRParenCommaSpace());

            // main
            builder.Add(alias, index);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(parts1);
            if (index != 0)
            {
                builder.AppendLiteral(Literal_CTDotIdIn());
                builder.Add(alias, index - 1);
                builder.AppendLiteral(Literal_And());
            }

            builder.AppendLiteral(Literal_CTDotTagIdIn());
            builder.Add(alias, alias, index);

            builder.AppendAscii(')');
        }

        if (like.Length > 0)
        {
            like.Sort(StringLengthReverseCompare);
            builder.WithOrComma(ref first);
            builder.Add(alias, ++index);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
            builder.AppendLiteral(parts1);
            if (index != 0)
            {
                builder.AppendLiteral(Literal_CTDotIdIn());
                builder.Add(alias, index - 1);
                builder.AppendLiteral(Literal_And());
            }

            builder.AppendLiteral(Literal_TTDotValue());
            builder.AppendLiteral(Literal_LikeQuotePercent());
            builder.AddSingleQuoteTextWithoutQuote(like[0]);
            builder.AppendLiteral(Literal_PercentQuote());
            for (var i = 1; i < like.Length; i++)
            {
                builder.AppendLiteral(Literal_Or());
                builder.AppendLiteral(Literal_TTDotValue());
                builder.AppendLiteral(Literal_LikeQuotePercent());
                builder.AddSingleQuoteTextWithoutQuote(like[i]);
                builder.AppendLiteral(Literal_PercentQuote());
            }

            builder.AppendAscii(')');
        }
    }

    [StringLiteral.Utf8(" EXCEPT ")]
    private static partial ReadOnlySpan<byte> Literal_Except();

    [StringLiteral.Utf8(" INTERSECT ")]
    private static partial ReadOnlySpan<byte> Literal_Intersect();

    private static void PreprocessExcept(ref this Utf8ValueStringBuilder builder, byte intersectAlias, byte exceptAlias, ref bool first, ref int intersect, ref int except, bool ignoreOr, string[]? ignoreExacts, string[]? ignorePartials, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (ignoreOr)
        {
            if (ignoreExacts is { Length: > 0 })
            {
                PreprocessExceptOr(ref builder, intersectAlias, ref first, ref intersect, ignoreExacts, parts0, parts1);
            }

            if (ignorePartials is { Length: > 0 })
            {
                var (array, longerThan2) = SplitLongerThan2(ignorePartials);
                PreprocessExceptOr(ref builder, intersectAlias, ref first, ref intersect, ignorePartials.AsSpan(0, longerThan2), ignorePartials.AsSpan(longerThan2, ignorePartials.Length - longerThan2), parts0, parts1);
                ArrayPool<string>.Shared.Return(array);
            }
        }
        else
        {
            var oldExcept = except;
            if (ignoreExacts is { Length: > 0 })
            {
                PreprocessExceptAnd(ref builder, intersectAlias, exceptAlias, ref first, intersect, ref except, ignoreExacts, parts0, parts1);
            }

            if (ignorePartials is { Length: > 0 })
            {
                var (array, longerThan2) = SplitLongerThan2(ignorePartials);
                PreprocessExceptAnd(ref builder, intersectAlias, exceptAlias, ref first, intersect, ref except, ignorePartials.AsSpan(0, longerThan2), ignorePartials.AsSpan(longerThan2, ignorePartials.Length - longerThan2), parts0, parts1);
                ArrayPool<string>.Shared.Return(array);
            }

            switch (except - oldExcept)
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    builder.WithOrComma(ref first);
                    builder.Add(exceptAlias, except + 1);
                    builder.AppendLiteral(Literal_ParenIdParenAs());
                    builder.Add(exceptAlias, oldExcept + 1);
                    for (var i = oldExcept + 2; i <= except; i++)
                    {
                        builder.AppendLiteral(Literal_Intersect());
                        builder.Add(exceptAlias, i);
                    }

                    builder.AppendAscii(')');
                    ++except;
                    break;
            }

            builder.WithOrComma(ref first);
            builder.Add(intersectAlias, ++intersect);
            builder.AppendLiteral(Literal_ParenIdParenAs());
            builder.AppendLiteral(Literal_SelectIdFrom());
            builder.Add(intersectAlias, intersect - 1);
            builder.AppendLiteral(Literal_Except());
            builder.AppendLiteral(Literal_SelectIdFrom());
            builder.Add(exceptAlias, except);
            builder.AppendAscii(')');
        }
    }

    private static void PreprocessExceptAnd(ref Utf8ValueStringBuilder builder, byte intersectAlias, byte exceptAlias, ref bool first, int intersect, ref int except, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        foreach (var item in exacts)
        {
            builder.WithOrComma(ref first);
            builder.Add(exceptAlias, ++except);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
            builder.AppendLiteral(parts1);
            builder.AppendLiteral(Literal_CTDotIdIn());
            builder.Add(intersectAlias, intersect);
            builder.AppendLiteral(Literal_And());
            builder.AppendLiteral(Literal_TTDotValue());
            builder.AppendLiteral(Literal_Equal());
            builder.AddSingleQuoteText(item);

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessExceptAnd(ref Utf8ValueStringBuilder builder, byte intersectAlias, byte exceptAlias, ref bool first, int intersect, ref int except, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (match.Length > 0)
        {
            match.Sort(StringLengthReverseCompare);
            foreach (var item in match)
            {
                // match
                builder.WithOrComma(ref first);
                builder.Add(exceptAlias, exceptAlias, ++except);
                builder.AppendLiteral(Literal_ParenIdParenAsSelectRowIdFromTagTextTableMatch());
                builder.AddDoubleQuoteText(item);
                builder.AppendLiteral(Literal_QuoteRRParenCommaSpace());

                // main
                builder.Add(exceptAlias, except);
                builder.AppendLiteral(Literal_ParenIdParenAs());

                builder.AppendLiteral(parts0);
                builder.AppendLiteral(parts1);
                builder.AppendLiteral(Literal_CTDotIdIn());
                builder.Add(intersectAlias, intersect);
                builder.AppendLiteral(Literal_And());
                builder.AppendLiteral(Literal_CTDotTagIdIn());
                builder.Add(exceptAlias, exceptAlias, except);

                builder.AppendAscii(')');
            }
        }

        if (like.Length > 0)
        {
            like.Sort(StringLengthReverseCompare);
            foreach (var item in like)
            {
                builder.WithOrComma(ref first);
                builder.Add(exceptAlias, ++except);
                builder.AppendLiteral(Literal_ParenIdParenAs());

                builder.AppendLiteral(parts0);
                builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
                builder.AppendLiteral(parts1);
                builder.AppendLiteral(Literal_CTDotIdIn());
                builder.Add(intersectAlias, intersect);
                builder.AppendLiteral(Literal_And());
                builder.AppendLiteral(Literal_TTDotValue());
                builder.AppendLiteral(Literal_LikeQuotePercent());
                builder.AddSingleQuoteTextWithoutQuote(item);

                var span = builder.GetSpan(3);
                span[0] = (byte)'%';
                span[1] = (byte)'\'';
                span[2] = (byte)')';
                builder.Advance(3);
            }
        }
    }

    [StringLiteral.Utf8("SELECT \"Id\" FROM ")]
    private static partial ReadOnlySpan<byte> Literal_SelectIdFrom();

    private static void PreprocessExceptOr(ref Utf8ValueStringBuilder builder, byte alias, ref bool first, ref int intersect, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (match.Length > 0)
        {
            match.Sort(StringLengthReverseCompare);
            // match temp table
            builder.WithOrComma(ref first);
            builder.Add(alias, alias, ++intersect);
            builder.AppendLiteral(Literal_ParenIdParenAsSelectRowIdFromTagTextTableMatch());
            builder.AddDoubleQuoteText(match[0]);
            for (var i = 1; i < match.Length; i++)
            {
                builder.AppendLiteral(Literal_Or());
                builder.AddDoubleQuoteText(match[i]);
            }

            builder.AppendLiteral(Literal_QuoteRRParenCommaSpace());

            // main
            builder.Add(alias, intersect);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(Literal_SelectIdFrom());
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(Literal_Except());
            builder.AppendLiteral(parts0);
            builder.AppendLiteral(parts1);
            builder.AppendLiteral(Literal_CTDotIdIn());
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(Literal_And());
            builder.AppendLiteral(Literal_CTDotTagIdIn());
            builder.Add(alias, alias, intersect);

            builder.AppendAscii(')');
        }

        if (like.Length > 0)
        {
            like.Sort(StringLengthReverseCompare);
            builder.WithOrComma(ref first);
            builder.Add(alias, ++intersect);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(Literal_SelectIdFrom());
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(Literal_Except());
            builder.AppendLiteral(parts0);
            builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
            builder.AppendLiteral(parts1);
            builder.AppendLiteral(Literal_CTDotIdIn());
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(Literal_And());
            builder.AppendLiteral(Literal_TTDotValue());
            builder.AppendLiteral(Literal_LikeQuotePercent());
            builder.AddSingleQuoteTextWithoutQuote(like[0]);
            builder.AppendLiteral(Literal_PercentQuote());
            for (var i = 1; i < like.Length; i++)
            {
                builder.AppendLiteral(Literal_Or());
                builder.AppendLiteral(Literal_TTDotValue());
                builder.AppendLiteral(Literal_LikeQuotePercent());
                builder.AddSingleQuoteTextWithoutQuote(like[i]);
                builder.AppendLiteral(Literal_PercentQuote());
            }

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessExceptOr(ref Utf8ValueStringBuilder builder, byte alias, ref bool first, ref int intersect, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        foreach (var item in exacts)
        {
            builder.WithOrComma(ref first);
            builder.Add(alias, ++intersect);
            builder.AppendLiteral(Literal_ParenIdParenAs());

            builder.AppendLiteral(Literal_SelectIdFrom());
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(Literal_Except());
            builder.AppendLiteral(parts0);
            builder.AppendLiteral(Literal_InnerJoinTagTableAsTTOnTagId());
            builder.AppendLiteral(parts1);
            builder.AppendLiteral(Literal_CTDotIdIn());
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(Literal_And());
            builder.AppendLiteral(Literal_TTDotValue());
            builder.AppendLiteral(Literal_Equal());
            builder.AddSingleQuoteText(item);

            builder.AppendAscii(')');
        }
    }

    [StringLiteral.Utf8("SELECT \"CT\".\"Id\" FROM \"ArtworkTagCrossTable\" AS \"CT\"")]
    private static partial ReadOnlySpan<byte> Literal_SelectIdFromUserTagCrossTableAsCT();

    [StringLiteral.Utf8(" WHERE ")]
    private static partial ReadOnlySpan<byte> Literal_Where();
}
