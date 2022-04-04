namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    [StringLiteral.Utf8("))")]
    private static partial ReadOnlySpan<byte> Literal_RRParen();

    private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, TextFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.Partials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendLiteral(Literal_ExistsTextTableRowId());
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotId());
            builder.AppendLiteral(Literal_And());
            builder.AppendAscii('(');
            builder.TextPartial(filter.Partials, filter.PartialOr);
            builder.AppendLiteral(Literal_RRParen());

            if (filter.IgnorePartials is { Length: > 0 })
            {
                builder.AppendLiteral(Literal_And());
                builder.AppendLiteral(Literal_NotLeftParen());
                builder.TextPartial(filter.IgnorePartials, filter.IgnorePartialOr);
                builder.AppendLiteral(Literal_RRParen());
            }
        }
        else
        {
            if (filter.IgnorePartials is { Length: > 0 })
            {
                builder.And(ref and);
                builder.AppendLiteral(Literal_ExistsTextTableRowId());
                builder.AppendLiteral(origin);
                builder.AppendLiteral(Literal_DotId());
                builder.AppendLiteral(Literal_And());
                builder.AppendLiteral(Literal_NotLeftParen());
                builder.TextPartial(filter.IgnorePartials, filter.IgnorePartialOr);
                builder.AppendLiteral(Literal_RRParen());
            }
        }

        if (filter.Exact is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendAscii('(');
            builder.TextExact(origin, filter.Exact);
            builder.AppendAscii(')');
        }

        if (filter.IgnoreExact is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendLiteral(Literal_NotLeftParen());
            builder.TextExact(origin, filter.IgnoreExact);
            builder.AppendAscii(')');
        }
    }

    private static void TextExact(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> origin, string text)
    {
        builder.AppendLiteral(origin);
        builder.AppendLiteral(Literal_DotTitle());
        builder.AppendLiteral(Literal_Equal());
        builder.AddSingleQuoteText(text);
        builder.AppendLiteral(Literal_Or());
        builder.AppendLiteral(origin);
        builder.AppendLiteral(Literal_DotCaption());
        builder.AppendLiteral(Literal_Equal());
        builder.AddSingleQuoteText(text);
        builder.AppendLiteral(Literal_Or());
        builder.AppendLiteral(origin);
        builder.AppendLiteral(Literal_DotMemo());
        builder.AppendLiteral(Literal_Equal());
        builder.AddSingleQuoteText(text);
    }

    private static void TextPartial(ref this Utf8ValueStringBuilder builder, string[] partials, bool or)
    {
        var (oneOrTwo, oneOrTwoCount, threeOrMore, threeOrMoreCount) = Divide(partials);
        if (threeOrMore is not null)
        {
            builder.TextMatch(or, threeOrMore.AsSpan(0, threeOrMoreCount));

            if (oneOrTwo is not null)
            {
                builder.AppendLiteral(or ? Literal_Or() : Literal_And());
                builder.TextLike(or, oneOrTwo.AsSpan(0, oneOrTwoCount));
                ArrayPool<string>.Shared.Return(oneOrTwo);
                ArrayPool<string>.Shared.Return(threeOrMore);
            }
        }
        else
        {
            builder.TextLike(or, (oneOrTwo ?? throw new NullReferenceException()).AsSpan(0, oneOrTwoCount));
        }
    }

    private static void TextLike(ref this Utf8ValueStringBuilder builder, bool or, ReadOnlySpan<string> span)
    {
        builder.AppendAscii('(');
        builder.TextLike(or, span[0]);
        foreach (var item in span[1..])
        {
            builder.AppendLiteral(Literal_Or());
            builder.TextLike(or, item);
        }

        builder.AppendAscii(')');
    }

    [StringLiteral.Utf8(" LIKE '%")]
    private static partial ReadOnlySpan<byte> Literal_LikeQuotePercent();

    [StringLiteral.Utf8("%'")]
    private static partial ReadOnlySpan<byte> Literal_PercentQuote();

    private static void TextLike(ref this Utf8ValueStringBuilder builder, bool or, string first)
    {
        builder.AppendAscii('(');
        builder.AppendLiteral(Literal_TextTable());
        builder.AppendLiteral(Literal_DotTitle());
        builder.AppendLiteral(Literal_LikeQuotePercent());
        builder.AddSingleQuoteTextWithoutQuote(first);

        builder.AppendLiteral(Literal_PercentQuote());
        builder.AppendLiteral(or ? Literal_Or() : Literal_And());
        builder.AppendLiteral(Literal_TextTable());
        builder.AppendLiteral(Literal_DotCaption());
        builder.AppendLiteral(Literal_LikeQuotePercent());

        builder.AddSingleQuoteTextWithoutQuote(first);
        builder.AppendLiteral(Literal_PercentQuote());
        builder.AppendLiteral(or ? Literal_Or() : Literal_And());
        builder.AppendLiteral(Literal_TextTable());
        builder.AppendLiteral(Literal_DotMemo());
        builder.AppendLiteral(Literal_LikeQuotePercent());

        builder.AddSingleQuoteTextWithoutQuote(first);
        builder.AppendLiteral(Literal_PercentQuote());
    }

    [StringLiteral.Utf8(" MATCH ")]
    private static partial ReadOnlySpan<byte> Literal_Match();

    private static void TextMatch(ref this Utf8ValueStringBuilder builder, bool or, ReadOnlySpan<string> span)
    {
        builder.AppendAscii('(');
        builder.AppendLiteral(Literal_TextTable());
        builder.AppendLiteral(Literal_Match());
        if (span.Length == 1)
        {
            builder.AddSingleQuoteText(span[0]);
        }
        else
        {
            builder.AppendAscii('\'');
            builder.AddDoubleQuoteText(span[0]);
            var orOrAnd = or ? Literal_Or() : Literal_And();
            foreach (var item in span[1..])
            {
                builder.AppendLiteral(orOrAnd);
                builder.AddDoubleQuoteText(item);
            }

            builder.AppendAscii('\'');
        }

        builder.AppendAscii(')');
    }
}
