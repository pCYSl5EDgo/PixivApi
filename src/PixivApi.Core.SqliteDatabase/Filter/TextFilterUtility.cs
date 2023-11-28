namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
  private static void Filter(ref this Utf8ValueStringBuilder builder, ref bool and, ReadOnlySpan<byte> origin, TextFilter? filter)
  {
    if (filter is null)
    {
      return;
    }

    if (filter.Partials is { Length: > 0 })
    {
      builder.And(ref and);
      builder.AppendLiteral("EXISTS (SELECT * FROM \"ArtworkTextTable\" AS \"TextTable\" WHERE \"TextTable\".\"rowid\" = "u8);
      builder.AppendLiteral(origin);
      builder.AppendLiteral(".\"Id\" AND "u8);
      builder.AppendAscii('(');
      builder.TextPartial(filter.Partials, filter.PartialOr);
      builder.AppendLiteral("))"u8);

      if (filter.IgnorePartials is { Length: > 0 })
      {
        builder.AppendLiteral(" AND  NOT ("u8);
        builder.TextPartial(filter.IgnorePartials, filter.IgnorePartialOr);
        builder.AppendLiteral("))"u8);
      }
    }
    else
    {
      if (filter.IgnorePartials is { Length: > 0 })
      {
        builder.And(ref and);
        builder.AppendLiteral("EXISTS (SELECT * FROM \"ArtworkTextTable\" AS \"TextTable\" WHERE \"TextTable\".\"rowid\" = "u8);
        builder.AppendLiteral(origin);
        builder.AppendLiteral(".\"Id\" AND  NOT ("u8);
        builder.TextPartial(filter.IgnorePartials, filter.IgnorePartialOr);
        builder.AppendLiteral("))"u8);
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
      builder.AppendLiteral(" NOT ("u8);
      builder.TextExact(origin, filter.IgnoreExact);
      builder.AppendAscii(')');
    }
  }

  private static void TextExact(ref this Utf8ValueStringBuilder builder, ReadOnlySpan<byte> origin, string text)
  {
    builder.AppendLiteral(origin);
    builder.AppendLiteral(".\"Title\" = "u8);
    builder.AddSingleQuoteText(text);
    builder.AppendLiteral(" OR "u8);
    builder.AppendLiteral(origin);
    builder.AppendLiteral(".\"Caption\" = "u8);
    builder.AddSingleQuoteText(text);
    builder.AppendLiteral(" OR "u8);
    builder.AppendLiteral(origin);
    builder.AppendLiteral(".\"Memo\" = "u8);
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
        builder.AppendLiteral(or ? " OR "u8 : " AND "u8);
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
      builder.AppendLiteral(" OR "u8);
      builder.TextLike(or, item);
    }

    builder.AppendAscii(')');
  }

  private static void TextLike(ref this Utf8ValueStringBuilder builder, bool or, string first)
  {
    builder.AppendAscii('(');
    builder.AppendLiteral("\"TextTable\".\"Title\" LIKE '%"u8);
    builder.AddSingleQuoteTextWithoutQuote(first);

    builder.AppendLiteral("%'"u8);
    builder.AppendLiteral(or ? " OR "u8 : " AND \"TextTable\".\"Caption\" LIKE '%"u8);

    builder.AddSingleQuoteTextWithoutQuote(first);
    builder.AppendLiteral("%'"u8);
    builder.AppendLiteral(or ? " OR "u8 : " AND \"TextTable\".\"Memo\" LIKE '%"u8);

    builder.AddSingleQuoteTextWithoutQuote(first);
    builder.AppendLiteral("%'"u8);
  }

  private static void TextMatch(ref this Utf8ValueStringBuilder builder, bool or, ReadOnlySpan<string> span)
  {
    builder.AppendAscii('(');
    builder.AppendLiteral("\"TextTable\" MATCH "u8);
    if (span.Length == 1)
    {
      builder.AddSingleQuoteText(span[0]);
    }
    else
    {
      builder.AppendAscii('\'');
      builder.AddDoubleQuoteText(span[0]);
      var orOrAnd = or ? " OR "u8 : " AND "u8;
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
