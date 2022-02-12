namespace PixivApi.Desktop;

internal record DependencyStore(HttpClient HttpClient, ConfigSettings ConfigSettings) : IDisposable
{
    public static DependencyStore? Instance = null;

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}