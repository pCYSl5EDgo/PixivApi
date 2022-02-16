namespace PixivApi.Desktop.Models;

public record NetworkSearchAsyncModel(string Text, bool? R18Filter, DateOnly? Since, DateOnly? Until)
{
#pragma warning disable CA1822
#pragma warning disable IDE0060
#if DEBUG
    public ValueTask StartSearchAsync(HttpClient? httpClient, ConfigSettings? configSettings, CancellationToken cancellationToken)
#else
    public ValueTask StartSearchAsync(HttpClient httpClient, ConfigSettings configSettings, CancellationToken cancellationToken)
#endif
#pragma warning restore IDE0060
#pragma warning restore CA1822
    {
        return ValueTask.CompletedTask;
    }
}
