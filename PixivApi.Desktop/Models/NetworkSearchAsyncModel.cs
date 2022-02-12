namespace PixivApi.Desktop.Models;

public record NetworkSearchAsyncModel(string Text, bool? R18Filter, DateOnly? Since, DateOnly? Until)
{
#if DEBUG
    public async ValueTask StartSearchAsync(HttpClient? httpClient, ConfigSettings? configSettings, CancellationToken cancellationToken)
#else
    public async ValueTask StartSearchAsync(HttpClient httpClient, ConfigSettings configSettings, CancellationToken cancellationToken)
#endif
    {
    }
}
