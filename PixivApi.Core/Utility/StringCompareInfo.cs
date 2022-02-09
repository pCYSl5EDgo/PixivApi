using System.Globalization;
namespace PixivApi;

public readonly struct StringCompareInfo
{
    private readonly CompareInfo? compareInfo;
    private readonly CompareOptions compareOptions;
    private readonly StringComparison stringComparison;

    public StringCompareInfo(string? culture, bool ignoreCase)
    {
        compareInfo = culture switch
        {
            null => CultureInfo.CurrentCulture.CompareInfo,
            "" => CultureInfo.InvariantCulture.CompareInfo,
            "ordinal" => null,
            _ => CultureInfo.GetCultureInfo(culture, true).CompareInfo,
        };

        compareOptions = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
        stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    public bool Equals(string arg0, string arg1) => compareInfo is null ? arg0.Equals(arg1, stringComparison) : compareInfo.Compare(arg0, arg1, compareOptions) == 0;
    
    public bool Contains(string arg0, string arg1) => compareInfo is null ? arg0.Contains(arg1, stringComparison) : compareInfo.IndexOf(arg0, arg1, compareOptions) != -1;
}
