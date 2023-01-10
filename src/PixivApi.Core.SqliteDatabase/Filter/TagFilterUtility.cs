namespace PixivApi.Core.SqliteDatabase;

internal static partial class FilterUtility
{
    private static (string[], int) SplitLongerThan2(string[] array)
    {
        var answer = ArrayPool<string>.Shared.Rent(array.Length);
        var longerThan2 = 0;
        foreach (var item in array)
        {
            if (item.Length > 2)
            {
                answer[longerThan2++] = item;
            }
        }

        if (longerThan2 < array.Length)
        {
            var index = longerThan2;
            foreach (var item in array)
            {
                if (item.Length <= 2)
                {
                    answer[index++] = item;
                }
            }
        }

        return (answer, longerThan2);
    }

    private static void Preprocess(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, bool or, string[]? exacts, string[]? partials, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (exacts is { Length: > 0 })
        {
            if (or)
            {
                builder.PreprocessArtworkOr(ref first, alias, ref index, exacts, parts0, parts1);
            }
            else
            {
                builder.PreprocessArtworkAnd(ref first, alias, ref index, exacts, parts0, parts1);
            }
        }

        if (partials is { Length: > 0 })
        {
            var (array, longerThan2) = SplitLongerThan2(partials);
            if (or)
            {
                builder.PreprocessArtworkOr(ref first, alias, ref index, array.AsSpan(0, longerThan2), array.AsSpan(longerThan2, partials.Length - longerThan2), parts0, parts1);
            }
            else
            {
                builder.PreprocessArtworkAnd(ref first, alias, ref index, array.AsSpan(0, longerThan2), array.AsSpan(longerThan2, partials.Length - longerThan2), parts0, parts1);
            }

            ArrayPool<string>.Shared.Return(array);
        }
    }

    private static void Preprocess(ref this Utf8ValueStringBuilder builder, TagFilter? filter, byte intersectAlias, byte exceptAlias, ref bool first, ref int intersect, ref int except, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (filter is null)
        {
            return;
        }

        var oldIntersect = intersect;
        var oldExcept = except;
        var done = false;
        if (filter.Exacts is { Length: > 0 } exacts)
        {
            done = true;
            if (filter.Or)
            {
                builder.PreprocessArtworkOr(ref first, intersectAlias, ref intersect, exacts, parts0, parts1);
            }
            else
            {
                builder.PreprocessArtworkAnd(ref first, intersectAlias, ref intersect, exacts, parts0, parts1);
            }
        }

        if (filter.Partials is { Length: > 0 } partials)
        {
            done = true;
            var (array, longerThan2) = SplitLongerThan2(partials);
            if (filter.Or)
            {
                builder.PreprocessArtworkOr(ref first, intersectAlias, ref intersect, array.AsSpan(0, longerThan2), array.AsSpan(longerThan2, partials.Length - longerThan2), parts0, parts1);
            }
            else
            {
                builder.PreprocessArtworkAnd(ref first, intersectAlias, ref intersect, array.AsSpan(0, longerThan2), array.AsSpan(longerThan2, partials.Length - longerThan2), parts0, parts1);
            }

            ArrayPool<string>.Shared.Return(array);
        }

        if (done && oldIntersect == -1 && oldExcept != -1)
        {
            builder.WithOrComma(ref first);
            builder.Add(intersectAlias, ++intersect);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"Id\" FROM "u8);
            builder.Add(intersectAlias, intersect - 1);
            builder.AppendLiteral(" EXCEPT SELECT \"Id\" FROM "u8);
            builder.Add(exceptAlias, except);
            builder.AppendAscii(')');
        }

        if (intersect == -1)
        {
            builder.Preprocess(ref first, exceptAlias, ref except, filter.IgnoreOr, filter.IgnoreExacts, filter.IgnorePartials, parts0, parts1);
        }
        else
        {
            builder.PreprocessExcept(intersectAlias, exceptAlias, ref first, ref intersect, ref except, filter.IgnoreOr, filter.IgnoreExacts, filter.IgnorePartials, parts0, parts1);
        }
    }

    private static void PreprocessArtworkAnd(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        foreach (var item in exacts)
        {
            builder.WithOrComma(ref first);
            builder.Add(alias, ++index);
            builder.AppendLiteral(" (\"Id\") AS ("u8);

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            if (index > 0)
            {
                builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
                builder.Add(alias, index - 1);
                builder.AppendLiteral(" AND "u8);
            }

            builder.AppendLiteral("\"TT\".\"Value\" = "u8);
            builder.AddSingleQuoteText(item);

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessArtworkOr(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        builder.WithOrComma(ref first);
        builder.Add(alias, ++index);
        builder.AppendLiteral(" (\"Id\") AS ("u8);

        builder.AppendLiteral(parts0);
        builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
        builder.AppendLiteral(parts1);
        if (index > 0)
        {
            builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
            builder.Add(alias, index - 1);
            builder.AppendLiteral(" AND "u8);
        }

        builder.AppendLiteral("\"TT\".\"Value\" IN "u8);
        builder.AppendAscii('(');
        for (var i = 0; i < exacts.Length; i++)
        {
            if (i != 0)
            {
                builder.AppendAscii(',');
            }

            builder.AddSingleQuoteText(exacts[i]);

        }

        builder.AppendLiteral("))"u8);
    }

    private static int StringLengthReverseCompare(string x, string y) => y.Length.CompareTo(x.Length);

    private static void PreprocessArtworkAnd(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        match.Sort(StringLengthReverseCompare);
        foreach (var item in match)
        {
            // match temp table
            builder.WithOrComma(ref first);
            builder.Add(alias, alias, ++index);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"rowid\" FROM \"TagTextTable\"('"u8);
            builder.AddDoubleQuoteText(item);
            builder.AppendLiteral("')), "u8);

            // main
            builder.Add(alias, index);
            builder.AppendLiteral(" (\"Id\") AS ("u8);

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(parts1);
            if (index > 0)
            {
                builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
                builder.Add(alias, index - 1);
                builder.AppendLiteral(" AND "u8);
            }

            builder.AppendLiteral("\"CT\".\"TagId\" IN "u8);
            builder.Add(alias, alias, index);

            builder.AppendAscii(')');
        }

        if (like.Length == 0)
        {
            return;
        }

        like.Sort(StringLengthReverseCompare);

        if (index == -1)
        {
            // like temp table
            builder.WithOrComma(ref first);
            builder.Add(alias, alias, ++index);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" LIKE '%"u8);
            builder.AddSingleQuoteTextWithoutQuote(like[0]);
            builder.AppendLiteral("%\\)"u8);

            // main
            builder.AppendLiteral(", "u8);
            builder.Add(alias, index);
            builder.AppendLiteral(" (\"Id\") AS ("u8);
            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            builder.AppendLiteral("\"CT\".\"TagId\" IN "u8);
            builder.Add(alias, alias, index);
            builder.AppendAscii(')');
        }
        else
        {
            Like(ref builder, ref first, alias, ref index, parts0, parts1, like[0]);
        }

        for (var i = 1; i < like.Length; i++)
        {
            var item = like[i];
            Like(ref builder, ref first, alias, ref index, parts0, parts1, item);
        }

        static void Like(ref Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1, string item)
        {
            builder.WithOrComma(ref first);
            builder.Add(alias, ++index);
            builder.AppendLiteral(" (\"Id\") AS ("u8);

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            if (index > 0)
            {
                builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
                builder.Add(alias, index - 1);
                builder.AppendLiteral(" AND "u8);
            }

            builder.AppendLiteral("\"TT\".\"Value\" LIKE '%"u8);
            builder.AddSingleQuoteTextWithoutQuote(item);
            builder.AppendLiteral("%\\)"u8);
        }
    }

    private static void PreprocessArtworkOr(ref this Utf8ValueStringBuilder builder, ref bool first, byte alias, ref int index, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (match.Length > 0)
        {
            match.Sort(StringLengthReverseCompare);
            // match temp table
            builder.WithOrComma(ref first);
            builder.Add(alias, alias, ++index);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"rowid\" FROM \"TagTextTable\"('"u8);
            builder.AddDoubleQuoteText(match[0]);
            for (var i = 1; i < match.Length; i++)
            {
                builder.AppendLiteral(" OR "u8);
                builder.AddDoubleQuoteText(match[i]);
            }

            builder.AppendLiteral("')), "u8);

            // main
            builder.Add(alias, index);
            builder.AppendLiteral(" (\"Id\") AS ("u8);

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(parts1);
            if (index > 0)
            {
                builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
                builder.Add(alias, index - 1);
                builder.AppendLiteral(" AND "u8);
            }

            builder.AppendLiteral("\"CT\".\"TagId\" IN "u8);
            builder.Add(alias, alias, index);

            builder.AppendAscii(')');
        }

        if (like.Length > 0)
        {
            like.Sort(StringLengthReverseCompare);
            builder.WithOrComma(ref first);
            builder.Add(alias, ++index);
            builder.AppendLiteral(" (\"Id\") AS ("u8);

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            if (index > 0)
            {
                builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
                builder.Add(alias, index - 1);
                builder.AppendLiteral(" AND "u8);
            }

            builder.AppendLiteral("\"TT\".\"Value\" LIKE '%"u8);
            builder.AddSingleQuoteTextWithoutQuote(like[0]);
            builder.AppendLiteral("%'"u8);
            for (var i = 1; i < like.Length; i++)
            {
                builder.AppendLiteral(" OR \"TT\".\"Value\" LIKE '%"u8);
                builder.AddSingleQuoteTextWithoutQuote(like[i]);
                builder.AppendLiteral("%'"u8);
            }

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessExcept(ref this Utf8ValueStringBuilder builder, byte intersectAlias, byte exceptAlias, ref bool first, ref int intersect, ref int except, bool ignoreOr, string[]? ignoreExacts, string[]? ignorePartials, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (ignoreOr)
        {
            if (ignoreExacts is { Length: > 0 })
            {
                PreprocessExceptOr(ref builder, intersectAlias, ref first, ref intersect, ignoreExacts, parts0, parts1);
            }

            if (ignorePartials is { Length: > 0 })
            {
                var (array, longerThan2) = SplitLongerThan2(ignorePartials);
                PreprocessExceptOr(ref builder, intersectAlias, ref first, ref intersect, ignorePartials.AsSpan(0, longerThan2), ignorePartials.AsSpan(longerThan2, ignorePartials.Length - longerThan2), parts0, parts1);
                ArrayPool<string>.Shared.Return(array);
            }
        }
        else
        {
            var oldExcept = except;
            if (ignoreExacts is { Length: > 0 })
            {
                PreprocessExceptAnd(ref builder, intersectAlias, exceptAlias, ref first, intersect, ref except, ignoreExacts, parts0, parts1);
            }

            if (ignorePartials is { Length: > 0 })
            {
                var (array, longerThan2) = SplitLongerThan2(ignorePartials);
                PreprocessExceptAnd(ref builder, intersectAlias, exceptAlias, ref first, intersect, ref except, ignorePartials.AsSpan(0, longerThan2), ignorePartials.AsSpan(longerThan2, ignorePartials.Length - longerThan2), parts0, parts1);
                ArrayPool<string>.Shared.Return(array);
            }

            switch (except - oldExcept)
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    builder.WithOrComma(ref first);
                    builder.Add(exceptAlias, except + 1);
                    builder.AppendLiteral(" (\"Id\") AS ("u8);
                    builder.Add(exceptAlias, oldExcept + 1);
                    for (var i = oldExcept + 2; i <= except; i++)
                    {
                        builder.AppendLiteral(" INTERSECT "u8);
                        builder.Add(exceptAlias, i);
                    }

                    builder.AppendAscii(')');
                    ++except;
                    break;
            }

            builder.WithOrComma(ref first);
            builder.Add(intersectAlias, ++intersect);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"Id\" FROM "u8);
            builder.Add(intersectAlias, intersect - 1);
            builder.AppendLiteral(" EXCEPT SELECT \"Id\" FROM "u8);
            builder.Add(exceptAlias, except);
            builder.AppendAscii(')');
        }
    }

    private static void PreprocessExceptAnd(ref Utf8ValueStringBuilder builder, byte intersectAlias, byte exceptAlias, ref bool first, int intersect, ref int except, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        foreach (var item in exacts)
        {
            builder.WithOrComma(ref first);
            builder.Add(exceptAlias, ++except);
            builder.AppendLiteral(" (\"Id\") AS ("u8);

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
            builder.Add(intersectAlias, intersect);
            builder.AppendLiteral(" AND \"TT\".\"Value\" = "u8);
            builder.AddSingleQuoteText(item);

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessExceptAnd(ref Utf8ValueStringBuilder builder, byte intersectAlias, byte exceptAlias, ref bool first, int intersect, ref int except, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (match.Length > 0)
        {
            match.Sort(StringLengthReverseCompare);
            foreach (var item in match)
            {
                // match
                builder.WithOrComma(ref first);
                builder.Add(exceptAlias, exceptAlias, ++except);
                builder.AppendLiteral(" (\"Id\") AS (SELECT \"rowid\" FROM \"TagTextTable\"('"u8);
                builder.AddDoubleQuoteText(item);
                builder.AppendLiteral("')), "u8);

                // main
                builder.Add(exceptAlias, except);
                builder.AppendLiteral(" (\"Id\") AS ("u8);

                builder.AppendLiteral(parts0);
                builder.AppendLiteral(parts1);
                builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
                builder.Add(intersectAlias, intersect);
                builder.AppendLiteral(" AND \"CT\".\"TagId\" IN "u8);
                builder.Add(exceptAlias, exceptAlias, except);

                builder.AppendAscii(')');
            }
        }

        if (like.Length > 0)
        {
            like.Sort(StringLengthReverseCompare);
            if (intersect == -1)
            {
                // like temp table
                builder.WithOrComma(ref first);
                builder.Add(exceptAlias, exceptAlias, ++except);
                builder.AppendLiteral(" (\"Id\") AS (SELECT \"Id\" FROM \"TagTable\" WHERE \"Value\" LIKE '%"u8);
                builder.AddSingleQuoteTextWithoutQuote(like[0]);
                builder.AppendLiteral("%\\)"u8);

                // main
                builder.AppendLiteral(", "u8);
                builder.Add(exceptAlias, except);
                builder.AppendLiteral(" (\"Id\") AS ("u8);
                builder.AppendLiteral(parts0);
                builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
                builder.AppendLiteral(parts1);
                builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
                builder.Add(intersectAlias, intersect);
                builder.AppendLiteral(" AND \"CT\".\"TagId\" IN "u8);
                builder.Add(exceptAlias, exceptAlias, except);
                builder.AppendAscii(')');
            }
            else
            {
                Like(ref builder, ref first, intersectAlias, exceptAlias, intersect, ref except, parts0, parts1, like[0]);
            }

            for (var i = 1; i < like.Length; i++)
            {
                var item = like[i];
                Like(ref builder, ref first, intersectAlias, exceptAlias, intersect, ref except, parts0, parts1, item);
            }
        }

        static void Like(ref Utf8ValueStringBuilder builder, ref bool first, byte intersectAlias, byte exceptAlias, int intersect, ref int except, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1, string item)
        {
            builder.WithOrComma(ref first);
            builder.Add(exceptAlias, ++except);
            builder.AppendLiteral(" (\"Id\") AS ("u8);

            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
            builder.Add(intersectAlias, intersect);
            builder.AppendLiteral(" AND \"TT\".\"Value\" LIKE '%"u8);
            builder.AddSingleQuoteTextWithoutQuote(item);

            builder.AppendLiteral("%\\)"u8);
        }
    }

    private static void PreprocessExceptOr(ref Utf8ValueStringBuilder builder, byte alias, ref bool first, ref int intersect, Span<string> match, Span<string> like, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        if (match.Length > 0)
        {
            match.Sort(StringLengthReverseCompare);
            // match temp table
            builder.WithOrComma(ref first);
            builder.Add(alias, alias, ++intersect);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"rowid\" FROM \"TagTextTable\"('"u8);
            builder.AddDoubleQuoteText(match[0]);
            for (var i = 1; i < match.Length; i++)
            {
                builder.AppendLiteral(" OR "u8);
                builder.AddDoubleQuoteText(match[i]);
            }

            builder.AppendLiteral("')), "u8);

            // main
            builder.Add(alias, intersect);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"Id\" FROM "u8);
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(" EXCEPT "u8);
            builder.AppendLiteral(parts0);
            builder.AppendLiteral(parts1);
            builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(" AND \"CT\".\"TagId\" IN "u8);
            builder.Add(alias, alias, intersect);

            builder.AppendAscii(')');
        }

        if (like.Length > 0)
        {
            like.Sort(StringLengthReverseCompare);
            builder.WithOrComma(ref first);
            builder.Add(alias, ++intersect);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"Id\" FROM "u8);
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(" EXCEPT "u8);
            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(" AND \"TT\".\"Value\" LIKE '%"u8);
            builder.AddSingleQuoteTextWithoutQuote(like[0]);
            builder.AppendLiteral("%'"u8);
            for (var i = 1; i < like.Length; i++)
            {
                builder.AppendLiteral(" OR \"TT\".\"Value\" LIKE '%"u8);
                builder.AddSingleQuoteTextWithoutQuote(like[i]);
                builder.AppendLiteral("%'"u8);
            }

            builder.AppendAscii(')');
        }
    }

    private static void PreprocessExceptOr(ref Utf8ValueStringBuilder builder, byte alias, ref bool first, ref int intersect, string[] exacts, ReadOnlySpan<byte> parts0, ReadOnlySpan<byte> parts1)
    {
        foreach (var item in exacts)
        {
            builder.WithOrComma(ref first);
            builder.Add(alias, ++intersect);
            builder.AppendLiteral(" (\"Id\") AS (SELECT \"Id\" FROM "u8);
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(" EXCEPT "u8);
            builder.AppendLiteral(parts0);
            builder.AppendLiteral(" INNER JOIN \"TagTable\" AS \"TT\" ON \"CT\".\"TagId\"=\"TT\".\"Id\""u8);
            builder.AppendLiteral(parts1);
            builder.AppendLiteral("\"CT\".\"Id\" IN "u8);
            builder.Add(alias, intersect - 1);
            builder.AppendLiteral(" AND \"TT\".\"Value\" = "u8);
            builder.AddSingleQuoteText(item);
            builder.AppendAscii(')');
        }
    }
}
