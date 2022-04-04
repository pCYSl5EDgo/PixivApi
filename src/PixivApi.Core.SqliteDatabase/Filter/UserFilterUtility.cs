namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    [StringLiteral.Utf8("\"UserTagCrossTable\"")]
    private static partial ReadOnlySpan<byte> Literal_UserTagCrossTable();

    [StringLiteral.Utf8(".\"Name\"")]
    private static partial ReadOnlySpan<byte> Literal_DotName();

    public static void Preprocess(ref Utf8ValueStringBuilder builder, UserFilter filter, ref bool first, ref int intersect, ref int except) => Preprocess(ref builder, filter.TagFilter, P, Q, ref first, ref intersect, ref except, Literal_SelectIdFromUserTagCrossTableAsCT(), Literal_Where());

    public static sqlite3_stmt CreateStatement(sqlite3 database, ref Utf8ValueStringBuilder builder, UserFilter filter, ILogger logger, int intersect, int except)
    {
        var and = false;
        filter.Filter(ref builder, ref and, Literal_Origin(), intersect, except);
        sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var answer);
        if (logger.IsEnabled(LogLevel.Debug))
        {
#pragma warning disable CA2254
            logger.LogDebug($"Query: {builder}");
#pragma warning restore CA2254
        }

        return answer;
    }

    [StringLiteral.Utf8("\"IsFollowed\"")]
    private static partial ReadOnlySpan<byte> Literal_IsFollowed();

    private const byte P = (byte)'P';
    private const byte Q = (byte)'Q';

    public static void Filter(this UserFilter filter, ref Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, int intersect, int except)
    {
        builder.FilterInOrNotIn(ref and, origin, P, Q, intersect, except);
        builder.Filter(ref and, origin, filter.HideFilter);
        builder.Filter(ref and, origin, filter.IsFollowed, Literal_IsFollowed());
        builder.TextFilterOfUser(ref and, origin, filter.NameFilter);
    }

    [StringLiteral.Utf8(" = '")]
    private static partial ReadOnlySpan<byte> Literal_EqualQuote();

    [StringLiteral.Utf8(" <> '")]
    private static partial ReadOnlySpan<byte> Literal_NotEqualQuote();

    private static void TextFilterOfUser(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, TextFilter? filter)
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
                builder.AppendLiteral(filter.PartialOr ? Literal_Or() : Literal_And());
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
                builder.AppendLiteral(filter.IgnorePartialOr ? Literal_Or() : Literal_And());
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