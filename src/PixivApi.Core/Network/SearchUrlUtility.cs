namespace PixivApi.Core.Network;

public sealed class SearchUrlUtility
{
    public static bool TryGetEndDate(ReadOnlySpan<char> span, out DateOnly date) => TryGetDate(span, out date, "&end_date=");

    public static bool TryGetStartDate(ReadOnlySpan<char> span, out DateOnly date) => TryGetDate(span, out date, "&start_date=");

    private static bool TryGetDate(ReadOnlySpan<char> span, out DateOnly date, ReadOnlySpan<char> dateText)
    {
        var index = span.IndexOf(dateText);
        if (index == -1)
        {
            goto FALSE;
        }

        span = span[(index + dateText.Length)..];
        var yearIndex = span.IndexOf('-');
        if (yearIndex == -1 || !uint.TryParse(span[..yearIndex], out var year))
        {
            goto FALSE;
        }

        span = span[(yearIndex + 1)..];
        var monthIndex = span.IndexOf('-');
        if (monthIndex == -1 || !byte.TryParse(span[..monthIndex], out var month))
        {
            goto FALSE;
        }

        span = span[(monthIndex + 1)..];
        var ampersandIndex = span.IndexOf('&');
        if (ampersandIndex != -1)
        {
            span = span[..ampersandIndex];
        }

        if (byte.TryParse(span, out var day))
        {
            date = new((int)year, month, day);
            return true;
        }

    FALSE:
        Unsafe.SkipInit(out date);
        return false;
    }

    public static string CalculateNextEndDateUrl(ReadOnlySpan<char> noOffset, DateOnly date)
    {
        const string Date = "&end_date=";
        var index = noOffset.IndexOf(Date);
        if (index == -1)
        {
            return $"{noOffset}{Date}{date.Year}-{date.Month}-{date.Day}";
        }
        else
        {
            return $"{noOffset[..index]}{Date}{date.Year}-{date.Month}-{date.Day}";
        }
    }

    public static string CalculateNextStartDateUrl(ReadOnlySpan<char> noOffset, DateOnly date)
    {
        const string Date = "&start_date=";
        var index = noOffset.IndexOf(Date);
        if (index == -1)
        {
            return $"{noOffset}{Date}{date.Year}-{date.Month}-{date.Day}";
        }
        else
        {
            return $"{noOffset[..index]}{Date}{date.Year}-{date.Month}-{date.Day}";
        }
    }

    public static int GetIndexOfOldestDay(ReadOnlySpan<ArtworkResponseContent> newToOld)
    {
        Debug.Assert(!newToOld.IsEmpty);
        var date = DateOnly.FromDateTime(newToOld[^1].CreateDate);
        for (var i = newToOld.Length - 2; i >= 0; i--)
        {
            var other = DateOnly.FromDateTime(newToOld[i].CreateDate);
            if (date != other)
            {
                Debug.Assert(date.CompareTo(other) < 0);
                return i;
            }
        }

        return 0;
    }

    public static int GetIndexOfNewestDay(ReadOnlySpan<ArtworkResponseContent> oldToNew)
    {
        Debug.Assert(!oldToNew.IsEmpty);
        var date = DateOnly.FromDateTime(oldToNew[^1].CreateDate);
        for (var i = oldToNew.Length - 2; i >= 0; i--)
        {
            var other = DateOnly.FromDateTime(oldToNew[i].CreateDate);
            if (date != other)
            {
                Debug.Assert(date.CompareTo(other) > 0);
                return i;
            }
        }

        return 0;
    }
}
