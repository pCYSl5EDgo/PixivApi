namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    public static void Preprocess(ref Utf8ValueStringBuilder builder, UserFilter filter, ref bool first, ref int intersect, ref int except)
        => Preprocess(ref builder, filter.TagFilter, P, Q, ref first, ref intersect, ref except, "SELECT \"CT\".\"Id\" FROM \"ArtworkTagCrossTable\" AS \"CT\""u8, " WHERE "u8);

    public static sqlite3_stmt CreateStatement(sqlite3 database, ref Utf8ValueStringBuilder builder, UserFilter filter, ILogger logger, int intersect, int except)
    {
        var and = false;
        filter.Filter(ref builder, ref and, "\"Origin\""u8, intersect, except);
        sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var answer);
        if (logger.IsEnabled(LogLevel.Debug))
        {
#pragma warning disable CA2254
            logger.LogDebug($"Query: {builder}");
#pragma warning restore CA2254
        }

        return answer;
    }

    private const byte P = (byte)'P';
    private const byte Q = (byte)'Q';

    public static void Filter(this UserFilter filter, ref Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, int intersect, int except)
    {
        builder.FilterInOrNotIn(ref and, origin, P, Q, intersect, except);
        builder.Filter(ref and, origin, filter.HideFilter);
        builder.Filter(ref and, origin, filter.IsFollowed, "\"IsFollowed\""u8);
        builder.TextFilterOfUser(ref and, origin, filter.NameFilter);
    }

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
            builder.AppendLiteral(".\"Name\" = '"u8);
            builder.AddSingleQuoteTextWithoutQuote(filter.Exact);
            builder.AppendAscii('\'');
        }

        if (!string.IsNullOrEmpty(filter.IgnoreExact))
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(".\"Name\" <> '"u8);
            builder.AddSingleQuoteTextWithoutQuote(filter.IgnoreExact);
            builder.AppendAscii('\'');
        }

        if (filter.Partials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendAscii('(');
            builder.AppendLiteral(origin);
            builder.AppendLiteral(".\"Name\" LIKE '%"u8);
            builder.AddSingleQuoteTextWithoutQuote(filter.Partials[0]);
            builder.AppendLiteral("%'"u8);

            foreach (var item in filter.Partials.AsSpan(1))
            {
                builder.AppendLiteral(filter.PartialOr ? " OR "u8 : " AND "u8);
                builder.AppendLiteral(origin);
                builder.AppendLiteral(".\"Name\" LIKE '%"u8);
                builder.AddSingleQuoteTextWithoutQuote(item);
                builder.AppendLiteral("%'"u8);
            }

            builder.AppendAscii(')');
        }

        if (filter.IgnorePartials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendLiteral(" NOT ("u8);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(".\"Name\" LIKE '%"u8);
            builder.AddSingleQuoteTextWithoutQuote(filter.IgnorePartials[0]);
            builder.AppendLiteral("%'"u8);

            foreach (var item in filter.IgnorePartials.AsSpan(1))
            {
                builder.AppendLiteral(filter.IgnorePartialOr ? " OR "u8 : " AND "u8);
                builder.AppendLiteral(origin);
                builder.AppendLiteral(".\"Name\" LIKE '%"u8);
                builder.AddSingleQuoteTextWithoutQuote(item);
                builder.AppendLiteral("%'"u8);
            }

            builder.AppendAscii(')');
        }
    }
}