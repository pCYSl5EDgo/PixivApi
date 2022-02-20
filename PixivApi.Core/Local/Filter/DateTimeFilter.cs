namespace PixivApi.Core.Local;

public sealed class DateTimeFilter : IFilter<DateTime>
{
    [JsonPropertyName("since")] public DateTime? Since;
    [JsonPropertyName("until")] public DateTime? Until;

    public bool Filter(DateTime dateTime) => (Since == null || dateTime.CompareTo(Since.Value) >= 0) && (Until == null || dateTime.CompareTo(Until.Value) <= 0);
}
