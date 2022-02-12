using System.IO;

namespace PixivApi.Desktop;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        var handler = new SocketsHttpHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            MaxConnectionsPerServer = 2,
        };
        var httpClient = new HttpClient(handler, true);
#if DEBUG
        ConfigSettings configSettings = new();
#else
        var configSettings = await GetConfigAsync(httpClient).ConfigureAwait(false);
#endif
        DependencyStore.Instance = new(httpClient, configSettings);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            if (DependencyStore.Instance is not null)
            {
                DependencyStore.Instance.Dispose();
                DependencyStore.Instance = null;
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .UseReactiveUI();

    private static async ValueTask<ConfigSettings> GetConfigAsync(HttpClient httpClient)
    {
        ConfigSettings? configSettings = null;
        var appsettings = new FileInfo("appsettings.json");
        if (appsettings.Exists && appsettings.Length != 0)
        {
            try
            {
                configSettings = await IOUtility.JsonDeserializeAsync<ConfigSettings>("appsettings.json", default).ConfigureAwait(false);
            }
            catch
            {
                configSettings = null;
            }
        }

        configSettings ??= new();
        if (string.IsNullOrWhiteSpace(configSettings.RefreshToken))
        {
            configSettings.RefreshToken = await AccessTokenUtility.AuthAsync(httpClient, configSettings, default).ConfigureAwait(false) ?? string.Empty;
            await IOUtility.JsonSerializeAsync("appsettings.json", configSettings, FileMode.Create).ConfigureAwait(false);
        }

        return configSettings;
    }
}
