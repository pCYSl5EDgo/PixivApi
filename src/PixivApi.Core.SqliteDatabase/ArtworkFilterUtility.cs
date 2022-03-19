using Cysharp.Text;
using PixivApi.Core.Local;
using SQLitePCL;
using System.Buffers;
using static SQLitePCL.raw;

namespace PixivApi.Core.SqliteDatabase;

internal static class ArtworkFilterUtility
{
    public static sqlite3_stmt CreateStatement(sqlite3 database, string prefix, ArtworkFilter filter)
    {
        var builder = ZString.CreateUtf8StringBuilder();
        builder.Append(prefix);
        var and = false;
        Filter(filter, ref builder, ref and, "Origin");
        sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var answer);
        builder.Dispose();
        return answer;
    }

    public static void Filter(this ArtworkFilter filter, ref Utf8ValueStringBuilder builder, ref bool and, string origin)
    {
        builder.Filter(ref and, origin, filter.IdFilter);
        builder.Filter(ref and, origin, filter.TagFilter, "ArtworkTagCrossTable");
        builder.Filter(ref and, origin, filter.HideFilter);
        builder.Filter(ref and, origin, filter.IsOfficiallyRemoved, nameof(filter.IsOfficiallyRemoved));
        builder.Filter(ref and, origin, filter.IsVisible, nameof(filter.IsVisible));
        builder.Filter(ref and, origin, filter.IsMuted, nameof(filter.IsMuted));
        Filter(ref builder, ref and, origin, filter.TotalView, nameof(filter.TotalView));
        Filter(ref builder, ref and, origin, filter.TotalBookmarks, nameof(filter.TotalBookmarks));
        Filter(ref builder, ref and, origin, filter.PageCount, nameof(filter.PageCount));
        Filter(ref builder, ref and, origin, filter.Width, nameof(filter.Width));
        Filter(ref builder, ref and, origin, filter.Height, nameof(filter.Height));
        Filter(ref builder, ref and, origin, filter.Type);
        builder.Filter(ref and, origin, filter.R18, "IsXRestricted");
        Filter(ref builder, ref and, origin, filter.DateTimeFilter);
        Filter(ref builder, ref and, origin, filter.TitleFilter);
        if (filter.UserFilter is not null)
        {
            builder.And(ref and);
            builder.Append("\"");
            builder.Append(origin);
            builder.Append("\".\"UserId\" IN (");
            var userAnd = false;
            filter.UserFilter.Filter(ref builder, ref userAnd, "UT");
            builder.AppendAscii(')');
        }

        OrderBy(ref builder, origin, filter.Order);

        if (!filter.ShouldHandleFileExistanceFilter)
        {
            Limit(ref builder, filter.Count, filter.Offset);
        }
    }

    private static void AddSingleQuoteText(ref Utf8ValueStringBuilder builder, string text)
    {
        const byte special = (byte)'\'';
        builder.GetSpan(1)[0] = special;
        builder.Advance(1);
        builder.AddSingleQuoteTextWithoutQuote(text);
        builder.GetSpan(1)[0] = special;
        builder.Advance(1);
    }

    private static void AddDoubleQuoteText(ref Utf8ValueStringBuilder builder, string text)
    {
        const byte special = (byte)'"';
        builder.GetSpan(1)[0] = special;
        builder.Advance(1);
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

        builder.GetSpan(1)[0] = special;
        builder.Advance(1);
    }

    private static (string[]? OneOrTwo, int OneOrTwoCount, string[]? ThreeOrMore, int ThreeOrMoreCount) Divide(string[] array)
    {
        if (array.Length == 1)
        {
            if (array[0].Length < 3)
            {
                return (array, 1, null, 0);
            }
            else
            {
                return (null, 0, array, 1);
            }
        }

        var oneOrTwo = ArrayPool<string>.Shared.Rent(array.Length);
        var oneOrTwoCount = 0;
        var threeOrMore = ArrayPool<string>.Shared.Rent(array.Length);
        var threeOrMoreCount = 0;
        foreach (var item in array)
        {
            if (item.Length < 3)
            {
                oneOrTwo[oneOrTwoCount++] = item;
            }
            else
            {
                threeOrMore[threeOrMoreCount++] = item;
            }
        }

        if (oneOrTwoCount == 0)
        {
            ArrayPool<string>.Shared.Return(oneOrTwo);
            ArrayPool<string>.Shared.Return(threeOrMore);
            return (null, 0, array, array.Length);
        }

        if (threeOrMoreCount == 0)
        {
            ArrayPool<string>.Shared.Return(oneOrTwo);
            ArrayPool<string>.Shared.Return(threeOrMore);
            return (array, array.Length, null, 0);
        }

        return (oneOrTwo, oneOrTwoCount, threeOrMore, threeOrMoreCount);
    }

    #region TextFilter
    private static void Filter(ref Utf8ValueStringBuilder builder, ref bool and, string origin, TextFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.Partials is { Length: > 0 })
        {
            builder.And(ref and);
            builder.Append("EXISTS (SELECT * FROM \"ArtworkTextTable\" AS \"TextTable\" WHERE \"TextTable\".\"rowid\" = \""); builder.Append(origin); builder.Append("\".\"Id\" AND (");
            TextPartial(ref builder, filter.Partials, filter.PartialOr);
            builder.Append("))");

            if (filter.IgnorePartials is { Length: > 0 })
            {
                builder.Append(" AND NOT (");
                TextPartial(ref builder, filter.IgnorePartials, filter.IgnorePartialOr);
                builder.Append("))");
            }
        }
        else
        {
            if (filter.IgnorePartials is { Length: > 0 })
            {
                builder.And(ref and);
                builder.Append("EXISTS (SELECT * FROM \"ArtworkTextTable\" AS \"TextTable\" WHERE \"TextTable\".\"rowid\" = \""); builder.Append(origin); builder.Append("\".\"Id\" AND NOT (");
                TextPartial(ref builder, filter.IgnorePartials, filter.IgnorePartialOr);
                builder.Append("))");
            }
        }

        if (filter.Exact is { Length: > 0 })
        {
            builder.And(ref and);
            builder.AppendAscii('(');
            TextExact(ref builder, origin, filter.Exact);
            builder.AppendAscii(')');
        }

        if (filter.IgnoreExact is { Length: > 0 })
        {
            builder.And(ref and);
            builder.Append("NOT (");
            TextExact(ref builder, origin, filter.IgnoreExact);
            builder.AppendAscii(')');
        }
    }

    private static void TextExact(ref Utf8ValueStringBuilder builder, string origin, string text)
    {
        builder.Append("\""); builder.Append(origin); builder.Append("\".\"Title\" = ");
        AddSingleQuoteText(ref builder, text);
        builder.Append("OR \""); builder.Append(origin); builder.Append("\".\"Caption\" = ");
        AddSingleQuoteText(ref builder, text);
        builder.Append("OR \""); builder.Append(origin); builder.Append("\".\"Memo\" = ");
        AddSingleQuoteText(ref builder, text);
    }

    private static void TextPartial(ref Utf8ValueStringBuilder builder, string[] partials, bool or)
    {
        var (oneOrTwo, oneOrTwoCount, threeOrMore, threeOrMoreCount) = Divide(partials);
        if (threeOrMore is not null)
        {
            TextMatch(ref builder, or, threeOrMore.AsSpan(0, threeOrMoreCount));

            if (oneOrTwo is not null)
            {
                if (or)
                {
                    builder.Append(" OR ");
                }
                else
                {
                    builder.Append(" AND ");
                }

                TextLike(ref builder, or, oneOrTwo.AsSpan(0, oneOrTwoCount));
                ArrayPool<string>.Shared.Return(oneOrTwo);
                ArrayPool<string>.Shared.Return(threeOrMore);
            }
        }
        else
        {
            TextLike(ref builder, or, (oneOrTwo ?? throw new NullReferenceException()).AsSpan(0, oneOrTwoCount));
        }
    }

    private static void TextLike(ref Utf8ValueStringBuilder builder, bool or, ReadOnlySpan<string> span)
    {
        builder.AppendAscii('(');
        TextLike(ref builder, or, span[0]);
        foreach (var item in span[1..])
        {
            builder.Append(" OR ");
            TextLike(ref builder, or, item);
        }

        builder.AppendAscii(')');
    }

    private static void TextLike(ref Utf8ValueStringBuilder builder, bool or, string first)
    {
        builder.Append("(\"TextTable\".\"Title\" LIKE '%");
        builder.AddSingleQuoteTextWithoutQuote(first);
        if (or)
        {
            builder.Append("%' OR \"TextTable\".\"Caption\" LIKE '%");
        }
        else
        {
            builder.Append("%' AND \"TextTable\".\"Caption\" LIKE '%");
        }

        builder.AddSingleQuoteTextWithoutQuote(first);
        if (or)
        {
            builder.Append("%' OR \"TextTable\".\"Memo\" LIKE '%");
        }
        else
        {
            builder.Append("%' AND \"TextTable\".\"Memo\" LIKE '%");
        }

        builder.AddSingleQuoteTextWithoutQuote(first);
        builder.Append("%')");
    }

    private static void TextMatch(ref Utf8ValueStringBuilder builder, bool or, ReadOnlySpan<string> span)
    {
        builder.Append("(\"TextTable\" MATCH ");
        if (span.Length == 1)
        {
            AddSingleQuoteText(ref builder, span[0]);
        }
        else
        {
            builder.AppendAscii('\'');
            AddDoubleQuoteText(ref builder, span[0]);
            foreach (var item in span[1..])
            {
                if (or)
                {
                    builder.Append(" OR ");
                }
                else
                {
                    builder.Append(" AND ");
                }

                AddDoubleQuoteText(ref builder, item);
            }

            builder.AppendAscii('\'');
        }

        builder.AppendAscii(')');
    }
    #endregion

    private static void Filter(ref Utf8ValueStringBuilder builder, ref bool and, string origin, DateTimeFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.Since.HasValue)
        {
            var unixEpochSinceSeconds = (ulong)filter.Since.Value.Subtract(DateTime.UnixEpoch).TotalSeconds;
            builder.And(ref and);
            if (filter.Until.HasValue)
            {
                var unixEpochUntilSeconds = (ulong)filter.Until.Value.Subtract(DateTime.UnixEpoch).TotalSeconds;
                builder.Append("unixepoch(\""); builder.Append(origin); builder.Append("\".\"CreateData\") BETWEEN ");
                builder.Append(unixEpochSinceSeconds);
                builder.Append(" AND ");
                builder.Append(unixEpochUntilSeconds);
            }
            else
            {
                builder.Append("unixepoch(\""); builder.Append(origin); builder.Append("\".\"CreateData\") >= ");
                builder.Append(unixEpochSinceSeconds);
            }
        }
        else
        {
            if (filter.Until.HasValue)
            {
                var unixEpochUntilSeconds = (ulong)filter.Until.Value.Subtract(DateTime.UnixEpoch).TotalSeconds;
                builder.And(ref and);
                builder.Append("unixepoch(\""); builder.Append(origin); builder.Append("\".\"CreateData\") <= ");
                builder.Append(unixEpochUntilSeconds);
            }
            else
            {
                return;
            }
        }
    }

    private static void Filter(ref Utf8ValueStringBuilder builder, ref bool and, string origin, ArtworkType? value)
    {
        if (value is null)
        {
            return;
        }

        builder.And(ref and);
        builder.Append("\""); builder.Append(origin); builder.Append("\".\"Type\" = ");
        builder.Append((byte)value.Value);
    }

    private static void Filter(ref Utf8ValueStringBuilder builder, ref bool and, string origin, MinMaxFilter? filter, string name)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.Max.HasValue)
        {
            builder.And(ref and);
            var maxValue = filter.Max.Value;
            if (maxValue > 0)
            {
                if (filter.Min.HasValue && filter.Min > 0)
                {
                    builder.AddName(origin, name);
                    builder.Append(" BETWEEN ");
                    builder.Append(filter.Min.Value);
                    builder.Append(" AND ");
                    builder.Append(maxValue);
                }
                else
                {
                    builder.AddName(origin, name);
                    builder.Append(" <= ");
                    builder.Append(maxValue);
                }
            }
            else
            {
                builder.Append("FALSE");
            }
        }
        else if (filter.Min.HasValue && filter.Min > 0)
        {
            builder.And(ref and);
            builder.AddName(origin, name);
            builder.Append(" >= ");
            builder.Append(filter.Min.Value);
        }
    }

    private static void OrderBy(ref Utf8ValueStringBuilder builder, string origin, ArtworkOrderKind order)
    {
        if (order == ArtworkOrderKind.None)
        {
            return;
        }

        builder.Append(" ORDER BY \""); builder.Append(origin); builder.Append("\".\"");
        var orderKind = order switch
        {
            ArtworkOrderKind.Id or ArtworkOrderKind.ReverseId => "Id\"",
            ArtworkOrderKind.View or ArtworkOrderKind.ReverseView => "TotalView\"",
            ArtworkOrderKind.Bookmarks or ArtworkOrderKind.ReverseBookmarks => "TotalBookmarks\"",
            ArtworkOrderKind.UserId or ArtworkOrderKind.ReverseUserId => "UserId\"",
            _ => throw new InvalidDataException(),
        };
        builder.Append(orderKind);
        var orderKindAscDesc = order switch
        {
            ArtworkOrderKind.Id or ArtworkOrderKind.View or ArtworkOrderKind.Bookmarks or ArtworkOrderKind.UserId => " ASC",
            ArtworkOrderKind.ReverseId or ArtworkOrderKind.ReverseView or ArtworkOrderKind.ReverseBookmarks or ArtworkOrderKind.ReverseUserId => " DESC",
            _ => throw new InvalidDataException(),
        };
        builder.Append(orderKindAscDesc);
    }

    private static void Limit(ref Utf8ValueStringBuilder builder, int? count, int offset)
    {
        if (count.HasValue)
        {
            var c = count.Value;
            if (c == 0)
            {
                builder.Append(" LIMIT 0");
            }
            else if (c > 0)
            {
                builder.Append(" LIMIT ");
                builder.Append(c);
            }
        }

        if (offset > 0)
        {
            builder.Append(" OFFSET ");
            builder.Append(offset);
        }
    }
}
