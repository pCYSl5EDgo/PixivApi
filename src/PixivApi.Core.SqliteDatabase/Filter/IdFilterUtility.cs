namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
  private static void WithOrComma(ref this Utf8ValueStringBuilder builder, ref bool first)
  {
    if (first)
    {
      builder.AppendLiteral("WITH "u8);
      first = false;
    }
    else
    {
      builder.AppendLiteral(", "u8);
    }
  }

  private static void Add(ref this Utf8ValueStringBuilder builder, byte alias, int index)
  {
    var span = builder.GetSpan(2);
    span[0] = (byte)'"';
    span[1] = alias;
    builder.Advance(2);
    builder.Append(index);
    builder.GetSpan(1)[0] = (byte)'"';
    builder.Advance(1);
  }

  private static void Add(ref this Utf8ValueStringBuilder builder, byte b0, byte b1, int index)
  {
    var span = builder.GetSpan(3);
    span[0] = (byte)'"';
    span[1] = b0;
    span[2] = b1;
    builder.Advance(3);
    builder.Append(index);
    builder.AppendAscii('"');
  }

  private const byte I = (byte)'I';
  private const byte E = (byte)'E';

  private static void Preprocess(ref this Utf8ValueStringBuilder builder, IdFilter? filter, byte intersectAlias, byte exceptAlias, ref bool first, ref int intersect, ref int except)
  {
    if (filter is null)
    {
      return;
    }

    if (filter.Ids is { Length: > 0 } intersects)
    {
      builder.WithOrComma(ref first);
      builder.Add(intersectAlias, ++intersect);
      builder.AppendLiteral(" (\"Id\") AS (VALUES ("u8);
      builder.Append(intersects[0]);
      for (var i = 1; i < intersects.Length; i++)
      {
        builder.AppendLiteral("), ("u8);
        builder.Append(intersects[i]);
      }

      builder.AppendAscii(')');

      if (intersect == 0 && except >= 0)
      {
        builder.AppendLiteral(" EXCEPT SELECT \"Id\" FROM "u8);
        builder.Add(exceptAlias, except);
      }

      builder.AppendLiteral(") "u8);
    }

    if (filter.IgnoreIds is { Length: > 0 } excepts)
    {
      if (intersect == -1)
      {
        builder.WithOrComma(ref first);
        builder.Add(exceptAlias, ++except);
        builder.AppendLiteral(" (\"Id\") AS (VALUES ("u8);
        builder.Append(excepts[0]);
        for (var i = 1; i < excepts.Length; i++)
        {
          builder.AppendLiteral("), ("u8);
          builder.Append(excepts[i]);
        }

        builder.AppendLiteral(")) "u8);
      }
      else
      {
        builder.WithOrComma(ref first);
        builder.Add(intersectAlias, ++intersect);
        builder.AppendLiteral(" (\"Id\") AS ("u8);
        builder.Add(intersectAlias, intersect - 1);
        builder.AppendLiteral(" EXCEPT VALUES ("u8);
        builder.Append(excepts[0]);
        for (var i = 1; i < excepts.Length; i++)
        {
          builder.AppendLiteral("), ("u8);
          builder.Append(excepts[i]);
        }

        builder.AppendLiteral(")) "u8);
      }
    }
  }
}
