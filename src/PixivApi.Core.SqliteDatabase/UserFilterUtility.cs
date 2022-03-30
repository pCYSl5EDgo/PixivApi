﻿namespace PixivApi.Core.SqliteDatabase;

internal static partial class UserFilterUtility
{
    [StringLiteral.Utf8("\"Origin\"")]
    private static partial ReadOnlySpan<byte> Literal_Origin();

    [StringLiteral.Utf8("\"UserTagCrossTable\"")]
    private static partial ReadOnlySpan<byte> Literal_UserTagCrossTable();

    [StringLiteral.Utf8(".\"Name\"")]
    private static partial ReadOnlySpan<byte> Literal_DotName();

    public static sqlite3_stmt CreateStatement(sqlite3 database, ref Utf8ValueStringBuilder builder, UserFilter filter)
    {
        var and = false;
        filter.Filter(ref builder, ref and, Literal_Origin());
        sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var answer);
        return answer;
    }

    [StringLiteral.Utf8("\"IsFollowed\"")]
    private static partial ReadOnlySpan<byte> Literal_IsFollowed();

    public static void Filter(this UserFilter filter, ref Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin)
    {
        builder.Filter(ref and, origin, filter.IdFilter);
        builder.Filter(ref and, origin, filter.TagFilter, Literal_UserTagCrossTable());
        builder.Filter(ref and, origin, filter.HideFilter);
        builder.Filter(ref and, origin, filter.IsFollowed, Literal_IsFollowed());
        builder.Filter(ref and, origin, filter.NameFilter);
    }

    [StringLiteral.Utf8(" LIKE '%")]
    private static partial ReadOnlySpan<byte> Literal_LikeQuotePercent();

    [StringLiteral.Utf8("%'")]
    private static partial ReadOnlySpan<byte> Literal_PercentQuote();

    [StringLiteral.Utf8(" = '")]
    private static partial ReadOnlySpan<byte> Literal_EqualQuote();

    [StringLiteral.Utf8(" <> '")]
    private static partial ReadOnlySpan<byte> Literal_NotEqualQuote();

    [StringLiteral.Utf8(" NOT (")]
    private static partial ReadOnlySpan<byte> Literal_NotLeftParen();

    private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, TextFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(filter.Exact))
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotName());
            builder.AppendLiteral(Literal_EqualQuote());
            builder.AddSingleQuoteTextWithoutQuote(filter.Exact);
            builder.AppendAscii('\'');
        }

        if (!string.IsNullOrEmpty(filter.IgnoreExact))
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotName());
            builder.AppendLiteral(Literal_NotEqualQuote());
            builder.AddSingleQuoteTextWithoutQuote(filter.IgnoreExact);
            builder.AppendAscii('\'');
        }

        if (filter.Partials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendAscii('(');
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotName());
            builder.AppendLiteral(Literal_LikeQuotePercent());
            builder.AddSingleQuoteTextWithoutQuote(filter.Partials[0]);
            builder.AppendLiteral(Literal_PercentQuote());

            foreach (var item in filter.Partials.AsSpan(1))
            {
                builder.AppendLiteral(filter.PartialOr ? FilterUtility.Literal_Or() : FilterUtility.Literal_And());
                builder.AppendLiteral(origin);
                builder.AppendLiteral(Literal_DotName());
                builder.AppendLiteral(Literal_LikeQuotePercent());
                builder.AddSingleQuoteTextWithoutQuote(item);
                builder.AppendLiteral(Literal_PercentQuote());
            }

            builder.AppendAscii(')');
        }

        if (filter.IgnorePartials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendLiteral(Literal_NotLeftParen());
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotName());
            builder.AppendLiteral(Literal_LikeQuotePercent());
            builder.AddSingleQuoteTextWithoutQuote(filter.IgnorePartials[0]);
            builder.AppendLiteral(Literal_PercentQuote());

            foreach (var item in filter.IgnorePartials.AsSpan(1))
            {
                builder.AppendLiteral(filter.IgnorePartialOr ? FilterUtility.Literal_Or() : FilterUtility.Literal_And());
                builder.AppendLiteral(origin);
                builder.AppendLiteral(Literal_DotName());
                builder.AppendLiteral(Literal_LikeQuotePercent());
                builder.AddSingleQuoteTextWithoutQuote(item);
                builder.AppendLiteral(Literal_PercentQuote());
            }

            builder.AppendAscii(')');
        }
    }
}