namespace PixivApi.Core.SqliteDatabase;

internal static class UserFilterUtility
{
    public static sqlite3_stmt CreateStatement(sqlite3 database, ref Utf8ValueStringBuilder builder, UserFilter filter)
    {
        var and = false;
        filter.Filter(ref builder, ref and, "Origin");
        sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var answer);
        return answer;
    }

    public static void Filter(this UserFilter filter, ref Utf8ValueStringBuilder builder, ref bool and, string origin)
    {
        builder.Filter(ref and, origin, filter.IdFilter);
        builder.Filter(ref and, origin, filter.TagFilter, "UserTagCrossTable");
        builder.Filter(ref and, origin, filter.HideFilter);
        builder.Filter(ref and, origin, filter.IsFollowed, nameof(filter.IsFollowed));
        Filter(ref builder, ref and, origin, filter.NameFilter);
    }

    private static void Filter(ref Utf8ValueStringBuilder builder, ref bool and, string origin, TextFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(filter.Exact))
        {
            builder.And(ref and);
            builder.Append("\""); builder.Append(origin); builder.Append("\".\"Name\" = '");
            builder.AddSingleQuoteTextWithoutQuote(filter.Exact);
            builder.AppendAscii('\'');
        }

        if (!string.IsNullOrEmpty(filter.IgnoreExact))
        {
            builder.And(ref and);
            builder.Append("\""); builder.Append(origin); builder.Append("\".\"Name\" <> '");
            builder.AddSingleQuoteTextWithoutQuote(filter.IgnoreExact);
            builder.AppendAscii('\'');
        }

        if (filter.Partials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.Append("(\""); builder.Append(origin); builder.Append("\".\"Name\" LIKE '%");
            builder.AddSingleQuoteTextWithoutQuote(filter.Partials[0]);
            builder.Append("%'");

            foreach (var item in filter.Partials.AsSpan(1))
            {
                builder.Append(filter.PartialOr ? " OR " : " AND ");
                builder.Append("\""); builder.Append(origin); builder.Append("\".\"Name\" LIKE '%");
                builder.AddSingleQuoteTextWithoutQuote(item);
                builder.Append("%'");
            }

            builder.AppendAscii(')');
        }

        if (filter.IgnorePartials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.Append("NOT (\""); builder.Append(origin); builder.Append("\".\"Name\" LIKE '%");
            builder.AddSingleQuoteTextWithoutQuote(filter.IgnorePartials[0]);
            builder.Append("%'");

            foreach (var item in filter.IgnorePartials.AsSpan(1))
            {
                builder.Append(filter.IgnorePartialOr ? " OR " : " AND ");
                builder.Append("\""); builder.Append(origin); builder.Append("\".\"Name\" LIKE '%");
                builder.AddSingleQuoteTextWithoutQuote(item);
                builder.Append("%'");
            }

            builder.AppendAscii(')');
        }
    }
}