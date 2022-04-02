namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    [StringLiteral.Utf8("\"Origin\"")]
    private static partial ReadOnlySpan<byte> Literal_Origin();

    [StringLiteral.Utf8("\"ArtworkTagCrossTable\"")]
    private static partial ReadOnlySpan<byte> Literal_ArtworkTagCrossTable();

    [StringLiteral.Utf8("\"TextTable\"")]
    private static partial ReadOnlySpan<byte> Literal_TextTable();

    [StringLiteral.Utf8("\"UT\"")]
    private static partial ReadOnlySpan<byte> Literal_UT();

    [StringLiteral.Utf8(".\"UserId\"")]
    private static partial ReadOnlySpan<byte> Literal_DotUserId();

    [StringLiteral.Utf8(".\"Title\"")]
    private static partial ReadOnlySpan<byte> Literal_DotTitle();

    [StringLiteral.Utf8(".\"Caption\"")]
    private static partial ReadOnlySpan<byte> Literal_DotCaption();

    [StringLiteral.Utf8(".\"Memo\"")]
    private static partial ReadOnlySpan<byte> Literal_DotMemo();

    [StringLiteral.Utf8(".\"CreateDate\"")]
    private static partial ReadOnlySpan<byte> Literal_DotCreateDate();

    [StringLiteral.Utf8(".\"Type\"")]
    private static partial ReadOnlySpan<byte> Literal_DotType();

    [StringLiteral.Utf8(".\"TotalView\"")]
    private static partial ReadOnlySpan<byte> Literal_DotTotalView();

    [StringLiteral.Utf8(".\"TotalBookmarks\"")]
    private static partial ReadOnlySpan<byte> Literal_DotTotalBookmarks();

    [StringLiteral.Utf8(" NOT (")]
    private static partial ReadOnlySpan<byte> Literal_NotLeftParen();

    [StringLiteral.Utf8("EXISTS (SELECT * FROM \"ArtworkTextTable\" AS \"TextTable\" WHERE \"TextTable\".\"rowid\" = ")]
    private static partial ReadOnlySpan<byte> Literal_ExistsTextTableRowId();

    public static sqlite3_stmt CreateStatement(sqlite3 database, ref Utf8ValueStringBuilder builder, ArtworkFilter filter, ILogger logger)
    {
        var and = false;
        builder.Filter(filter, ref and, Literal_Origin());
        sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var answer);
        if (logger.IsEnabled(LogLevel.Trace))
        {
#pragma warning disable CA2254
            logger.LogTrace($"Query: {builder}");
#pragma warning restore CA2254
        }

        return answer;
    }

    [StringLiteral.Utf8(" IN (SELECT \"UT\".\"Id\" FROM \"UserTable\" AS \"UT\" WHERE ")]
    private static partial ReadOnlySpan<byte> Literal_InUserTable();

    [StringLiteral.Utf8("\"TotalView\"")]
    private static partial ReadOnlySpan<byte> Literal_TotalView();

    [StringLiteral.Utf8("\"TotalBookmarks\"")]
    private static partial ReadOnlySpan<byte> Literal_TotalBookmarks();

    [StringLiteral.Utf8("\"PageCount\"")]
    private static partial ReadOnlySpan<byte> Literal_PageCount();

    [StringLiteral.Utf8("\"Width\"")]
    private static partial ReadOnlySpan<byte> Literal_Width();

    [StringLiteral.Utf8("\"Height\"")]
    private static partial ReadOnlySpan<byte> Literal_Height();

    [StringLiteral.Utf8("\"IsXRestricted\"")]
    private static partial ReadOnlySpan<byte> Literal_R18();

    [StringLiteral.Utf8("\"IsOfficiallyRemoved\"")]
    private static partial ReadOnlySpan<byte> Literal_IsOfficiallyRemoved();
    
    [StringLiteral.Utf8("\"IsBookmarked\"")]
    private static partial ReadOnlySpan<byte> Literal_IsBookmarked();

    [StringLiteral.Utf8("\"IsVisible\"")]
    private static partial ReadOnlySpan<byte> Literal_IsVisible();

    [StringLiteral.Utf8("\"IsMuted\"")]
    private static partial ReadOnlySpan<byte> Literal_IsMuted();

    private static void Filter(this ref Utf8ValueStringBuilder builder, ArtworkFilter filter, ref bool and, ReadOnlySpan<byte> origin)
    {
        builder.Filter(ref and, origin, filter.IdFilter);
        builder.Filter(ref and, origin, filter.TagFilter, Literal_ArtworkTagCrossTable());
        builder.Filter(ref and, origin, filter.HideFilter);
        builder.Filter(ref and, origin, filter.IsOfficiallyRemoved, Literal_IsOfficiallyRemoved());
        builder.Filter(ref and, origin, filter.IsBookmark, Literal_IsBookmarked());
        builder.Filter(ref and, origin, filter.IsVisible, Literal_IsVisible());
        builder.Filter(ref and, origin, filter.IsMuted, Literal_IsMuted());
        builder.Filter(ref and, origin, filter.TotalView, Literal_TotalView());
        builder.Filter(ref and, origin, filter.TotalBookmarks, Literal_TotalBookmarks());
        builder.Filter(ref and, origin, filter.PageCount, Literal_PageCount());
        builder.Filter(ref and, origin, filter.Width, Literal_Width());
        builder.Filter(ref and, origin, filter.Height, Literal_Height());
        builder.Filter(ref and, origin, filter.Type);
        builder.Filter(ref and, origin, filter.R18, Literal_R18());
        builder.Filter(ref and, origin, filter.DateTimeFilter);
        builder.Filter(ref and, origin, filter.TitleFilter);
        if (filter.UserFilter is not null)
        {
            builder.And(ref and);
            builder.AppendLiteral(origin);
            builder.AppendLiteral(Literal_DotUserId());
            builder.AppendLiteral(Literal_InUserTable());
            var userAnd = false;
            filter.UserFilter.Filter(ref builder, ref userAnd, Literal_UT());
            builder.AppendAscii(')');
        }

        builder.OrderBy(origin, filter.Order);
        if (!filter.ShouldHandleFileExistanceFilter)
        {
            builder.Limit(filter.Count, filter.Offset);
        }
    }

    private static void AddSingleQuoteText(ref this Utf8ValueStringBuilder builder, string text)
    {
        const byte special = (byte)'\'';
        builder.GetSpan(1)[0] = special;
        builder.Advance(1);
        builder.AddSingleQuoteTextWithoutQuote(text);
        builder.GetSpan(1)[0] = special;
        builder.Advance(1);
    }

    private static void AddDoubleQuoteText(ref this Utf8ValueStringBuilder builder, string text)
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
    #endregion

    [StringLiteral.Utf8(" BETWEEN ")]
    private static partial ReadOnlySpan<byte> Literal_Between();

    [StringLiteral.Utf8("unixepoch(")]
    private static partial ReadOnlySpan<byte> Literal_UnixEpoch();

    [StringLiteral.Utf8(" >= ")]
    private static partial ReadOnlySpan<byte> Literal_GreaterOrEqual();

    [StringLiteral.Utf8(" <= ")]
    private static partial ReadOnlySpan<byte> Literal_LessOrEqual();

    private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, DateTimeFilter? filter)
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
                builder.AppendLiteral(Literal_UnixEpoch());
                builder.AppendLiteral(origin);
                builder.AppendLiteral(Literal_DotCreateDate());
                builder.AppendAscii(')');
                builder.AppendLiteral(Literal_Between());
                builder.Append(unixEpochSinceSeconds);
                builder.AppendLiteral(Literal_And());
                builder.Append(unixEpochUntilSeconds);
            }
            else
            {
                builder.AppendLiteral(Literal_UnixEpoch());
                builder.AppendLiteral(origin);
                builder.AppendLiteral(Literal_DotCreateDate());
                builder.AppendAscii(')');
                builder.AppendLiteral(Literal_GreaterOrEqual());
                builder.Append(unixEpochSinceSeconds);
            }
        }
        else
        {
            if (filter.Until.HasValue)
            {
                var unixEpochUntilSeconds = (ulong)filter.Until.Value.Subtract(DateTime.UnixEpoch).TotalSeconds;
                builder.And(ref and);
                builder.AppendLiteral(Literal_UnixEpoch());
                builder.AppendLiteral(origin);
                builder.AppendLiteral(Literal_DotCreateDate());
                builder.AppendAscii(')');
                builder.AppendLiteral(Literal_LessOrEqual());
                builder.Append(unixEpochUntilSeconds);
            }
            else
            {
                return;
            }
        }
    }

    private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, ArtworkType? value)
    {
        if (value is null)
        {
            return;
        }

        builder.And(ref and);
        builder.AppendLiteral(origin);
        builder.AppendLiteral(Literal_DotType());
        builder.AppendLiteral(Literal_Equal());
        builder.Append((byte)value.Value);
    }

    private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, MinMaxFilter? filter, ReadOnlySpan<byte> name)
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
                    builder.AppendLiteral(Literal_Between());
                    builder.Append(filter.Min.Value);
                    builder.AppendLiteral(Literal_And());
                    builder.Append(maxValue);
                }
                else
                {
                    builder.AddName(origin, name);
                    builder.AppendLiteral(Literal_LessOrEqual());
                    builder.Append(maxValue);
                }
            }
            else
            {
                builder.AppendAscii('0');
            }
        }
        else if (filter.Min.HasValue && filter.Min > 0)
        {
            builder.And(ref and);
            builder.AddName(origin, name);
            builder.AppendLiteral(Literal_GreaterOrEqual());
            builder.Append(filter.Min.Value);
        }
    }

    [StringLiteral.Utf8(" ORDER BY ")]
    private static partial ReadOnlySpan<byte> Literal_OrderBy();

    [StringLiteral.Utf8(" ASC")]
    private static partial ReadOnlySpan<byte> Literal_Asc();

    [StringLiteral.Utf8(" DESC")]
    private static partial ReadOnlySpan<byte> Literal_Desc();

    private static void OrderBy(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> origin, ArtworkOrderKind order)
    {
        if (order == ArtworkOrderKind.None)
        {
            return;
        }

        builder.AppendLiteral(Literal_OrderBy());
        builder.AppendLiteral(origin);
        var orderKind = order switch
        {
            ArtworkOrderKind.Id or ArtworkOrderKind.ReverseId => Literal_DotId(),
            ArtworkOrderKind.View or ArtworkOrderKind.ReverseView => Literal_DotTotalView(),
            ArtworkOrderKind.Bookmarks or ArtworkOrderKind.ReverseBookmarks => Literal_DotTotalBookmarks(),
            ArtworkOrderKind.UserId or ArtworkOrderKind.ReverseUserId => Literal_DotUserId(),
            _ => throw new InvalidDataException(),
        };
        builder.AppendLiteral(orderKind);
        var orderKindAscDesc = order switch
        {
            ArtworkOrderKind.Id or ArtworkOrderKind.View or ArtworkOrderKind.Bookmarks or ArtworkOrderKind.UserId => Literal_Asc(),
            ArtworkOrderKind.ReverseId or ArtworkOrderKind.ReverseView or ArtworkOrderKind.ReverseBookmarks or ArtworkOrderKind.ReverseUserId => Literal_Desc(),
            _ => throw new InvalidDataException(),
        };
        builder.AppendLiteral(orderKindAscDesc);
    }

    [StringLiteral.Utf8(" LIMIT ")]
    private static partial ReadOnlySpan<byte> Literal_Limit();

    [StringLiteral.Utf8(" OFFSET ")]
    private static partial ReadOnlySpan<byte> Literal_Offset();

    private static void Limit(ref this Utf8ValueStringBuilder builder, int? count, int offset)
    {
        if (count.HasValue)
        {
            builder.AppendLiteral(Literal_Limit());
            var c = count.Value;
            if (c == 0)
            {
                builder.AppendAscii('0');
            }
            else if (c > 0)
            {
                builder.Append(c);
            }
        }

        if (offset > 0)
        {
            builder.AppendLiteral(Literal_Offset());
            builder.Append(offset);
        }
    }
}
