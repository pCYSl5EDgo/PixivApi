namespace PixivApi.Core.Network;

public sealed class SearchUrlUtility
{
    public static string CalculateNextUrl(ReadOnlySpan<char> noOffset, DateOnly date)
    {
        const string end_date = "&end_date=";
        var end = noOffset.IndexOf(end_date);
        if (end == -1)
        {
            return $"{noOffset}{end_date}{date.Year}-{date.Month}-{date.Day}";
        }
        else
        {
            return $"{noOffset[..end]}{end_date}{date.Year}-{date.Month}-{date.Day}";
        }
    }

    public static int GetIndexOfOldestDay(ReadOnlySpan<Artwork> newToOld)
    {
        Debug.Assert(!newToOld.IsEmpty);
        var date = DateOnly.FromDateTime(newToOld[^1].CreateDate);
        for (int i = newToOld.Length - 2; i >= 0; i--)
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
}
