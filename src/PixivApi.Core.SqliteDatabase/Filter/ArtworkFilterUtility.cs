namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
  public static void Preprocess(ref Utf8ValueStringBuilder builder, ArtworkFilter filter, ref bool first, ref int intersectArtwork, ref int exceptArtwork, ref int intersectUser, ref int exceptUser)
  {
    Preprocess(ref builder, filter.IdFilter, I, E, ref first, ref intersectArtwork, ref exceptArtwork);
    Preprocess(ref builder, filter.TagFilter, I, E, ref first, ref intersectArtwork, ref exceptArtwork, "SELECT \"CT\".\"Id\" FROM \"ArtworkTagCrossTable\" AS \"CT\""u8, " WHERE \"CT\".\"ValueKind\" <> 0 AND "u8);
    Preprocess(ref builder, filter.UserFilter?.IdFilter, P, Q, ref first, ref intersectUser, ref exceptUser);
    Preprocess(ref builder, filter.UserFilter?.TagFilter, P, Q, ref first, ref intersectUser, ref exceptUser, "SELECT \"CT\".\"Id\" FROM \"ArtworkTagCrossTable\" AS \"CT\""u8, " WHERE "u8);
  }

  public static sqlite3_stmt CreateStatement(sqlite3 database, ref Utf8ValueStringBuilder builder, ArtworkFilter filter, ILogger logger, int intersectArtwork, int exceptArtwork, int intersectUser, int exceptUser)
  {
    var and = false;
    Filter(ref builder, filter, ref and, "\"Origin\""u8, intersectArtwork, exceptArtwork, intersectUser, exceptUser);
    sqlite3_prepare_v3(database, builder.AsSpan(), 0, out var answer);
    if (logger.IsEnabled(LogLevel.Debug))
    {
#pragma warning disable CA2254
      logger.LogDebug($"Query: {builder}");
#pragma warning restore CA2254
    }

    return answer;
  }

  public static void Filter(ref Utf8ValueStringBuilder builder, ArtworkFilter filter, ref bool and, ReadOnlySpan<byte> origin, int intersectArtwork, int exceptArtwork, int intersectUser, int exceptUser)
  {
    builder.FilterInOrNotIn(ref and, origin, I, E, intersectArtwork, exceptArtwork);
    builder.Filter(ref and, origin, filter.HideFilter);
    builder.Filter(ref and, origin, filter.IsOfficiallyRemoved, "\"IsOfficiallyRemoved\""u8);
    builder.Filter(ref and, origin, filter.IsBookmark, "\"IsBookmarked\""u8);
    builder.Filter(ref and, origin, filter.IsVisible, "\"IsVisible\""u8);
    builder.Filter(ref and, origin, filter.IsMuted, "\"IsMuted\""u8);
    builder.Filter(ref and, origin, filter.TotalView, "\"TotalView\""u8);
    builder.Filter(ref and, origin, filter.TotalBookmarks, "\"TotalBookmarks\""u8);
    builder.Filter(ref and, origin, filter.PageCount, "\"PageCount\""u8);
    builder.Filter(ref and, origin, filter.Width, "\"Width\""u8);
    builder.Filter(ref and, origin, filter.Height, "\"Height\""u8);
    builder.Filter(ref and, origin, filter.Type);
    builder.Filter(ref and, origin, filter.R18, "\"IsXRestricted\""u8);
    builder.Filter(ref and, origin, filter.DateTimeFilter);
    builder.Filter(ref and, origin, filter.TitleFilter);
    if (filter.UserFilter is not null)
    {
      builder.And(ref and);
      builder.AppendLiteral(origin);
      builder.AppendLiteral(".\"UserId\" IN (SELECT \"UT\".\"Id\" FROM \"UserTable\" AS \"UT\" WHERE "u8);
      var userAnd = false;
      filter.UserFilter.Filter(ref builder, ref userAnd, "\"UT\""u8, intersectUser, exceptUser);
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
        builder.AppendLiteral("unixepoch("u8);
        builder.AppendLiteral(origin);
        builder.AppendLiteral(".\"CreateDate\") BETWEEN "u8);
        builder.Append(unixEpochSinceSeconds);
        builder.AppendLiteral(" AND "u8);
        builder.Append(unixEpochUntilSeconds);
      }
      else
      {
        builder.AppendLiteral("unixepoch("u8);
        builder.AppendLiteral(origin);
        builder.AppendLiteral(".\"CreateDate\") >= "u8);
        builder.Append(unixEpochSinceSeconds);
      }
    }
    else
    {
      if (filter.Until.HasValue)
      {
        var unixEpochUntilSeconds = (ulong)filter.Until.Value.Subtract(DateTime.UnixEpoch).TotalSeconds;
        builder.And(ref and);
        builder.AppendLiteral("unixepoch("u8);
        builder.AppendLiteral(origin);
        builder.AppendLiteral(".\"CreateDate\") <= "u8);
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
    builder.AppendLiteral(".\"Type\" = "u8);
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
          builder.AppendLiteral(" BETWEEN "u8);
          builder.Append(filter.Min.Value);
          builder.AppendLiteral(" AND "u8);
          builder.Append(maxValue);
        }
        else
        {
          builder.AddName(origin, name);
          builder.AppendLiteral(" <= "u8);
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
      builder.AppendLiteral(" >= "u8);
      builder.Append(filter.Min.Value);
    }
  }

  private static void OrderBy(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> origin, ArtworkOrderKind order)
  {
    if (order == ArtworkOrderKind.None)
    {
      return;
    }

    builder.AppendLiteral(" ORDER BY "u8);
    builder.AppendLiteral(origin);
    var orderKind = order switch
    {
      ArtworkOrderKind.Id or ArtworkOrderKind.ReverseId => ".\"Id\""u8,
      ArtworkOrderKind.View or ArtworkOrderKind.ReverseView => ".\"TotalView\""u8,
      ArtworkOrderKind.Bookmarks or ArtworkOrderKind.ReverseBookmarks => ".\"TotalBookmarks\""u8,
      ArtworkOrderKind.UserId or ArtworkOrderKind.ReverseUserId => ".\"UserId\""u8,
      _ => throw new InvalidDataException(),
    };
    builder.AppendLiteral(orderKind);
    var orderKindAscDesc = order switch
    {
      ArtworkOrderKind.Id or ArtworkOrderKind.View or ArtworkOrderKind.Bookmarks or ArtworkOrderKind.UserId => " ASC"u8,
      ArtworkOrderKind.ReverseId or ArtworkOrderKind.ReverseView or ArtworkOrderKind.ReverseBookmarks or ArtworkOrderKind.ReverseUserId => " DESC"u8,
      _ => throw new InvalidDataException(),
    };
    builder.AppendLiteral(orderKindAscDesc);
  }

  private static void Limit(ref this Utf8ValueStringBuilder builder, int? count, int offset)
  {
    if (count.HasValue)
    {
      builder.AppendLiteral(" LIMIT "u8);
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
      builder.AppendLiteral(" OFFSET "u8);
      builder.Append(offset);
    }
  }
}
