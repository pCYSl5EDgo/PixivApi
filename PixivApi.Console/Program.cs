using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PixivApi.Console;

public sealed class Program
{
    private readonly static CancellationTokenSource cts = new();

    public static async Task Main(string[] args)
    {
        System.Console.CancelKeyPress += CancelKeyPress;

        var handler = new SocketsHttpHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            MaxConnectionsPerServer = 2,
        };

        var httpClient = new HttpClient(handler, true)
        {
            Timeout = TimeSpan.FromHours(4),
            DefaultRequestVersion = new(2, 0),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        var configSettings = await GetConfigSettingAsync(httpClient, cts.Token).ConfigureAwait(false);


        var builder = ConsoleApp
            .CreateBuilder(args, ConfigureOptions)
            .ConfigureHostOptions(ConfigureHostOptions)
            .ConfigureLogging(ConfigureLogger)
            .ConfigureServices(ConfigureServices)
            .ConfigureServices(services => services.AddSingleton(configSettings))
            .ConfigureServices(services => services.AddSingleton(httpClient))
            .ConfigureServices(services => services.AddSingleton(cts))
            ;

        var app = builder.Build();
        app.AddSubCommands<NetworkClient>();
        app.AddSubCommands<LocalClient>();
        await app.RunAsync(cts.Token).ConfigureAwait(false);
    }

    private static async ValueTask<ConfigSettings> GetConfigSettingAsync(HttpClient httpClient, CancellationToken token)
    {
        ConfigSettings? configSettings = null;
        if (File.Exists("appsettings.json"))
        {
            configSettings = await IOUtility.JsonDeserializeAsync<ConfigSettings>("appsettings.json", token).ConfigureAwait(false);
        }

        configSettings ??= new();
        if (string.IsNullOrWhiteSpace(configSettings.RefreshToken))
        {
            configSettings.RefreshToken = await AccessTokenUtility.AuthAsync(httpClient, configSettings, token).ConfigureAwait(false) ?? string.Empty;
            await IOUtility.JsonSerializeAsync("appsettings.json", configSettings, FileMode.Create).ConfigureAwait(false);
        }

        return configSettings;
    }

    private static void ConfigureOptions(HostBuilderContext context, ConsoleAppOptions options)
    {
    }

    private static void CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        cts.Cancel();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {

    }

    private static void ConfigureLogger(ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.SetMinimumLevel(LogLevel.Information);
        SimpleConsoleLoggerExtensions.AddSimpleConsole(builder);
        EnableConsoleVirtualCode();
    }

    private static void ConfigureHostOptions(HostBuilderContext context, HostOptions options)
    {
        options.ShutdownTimeout = TimeSpan.FromDays(1);
    }

    private static unsafe void EnableConsoleVirtualCode()
    {
        IntPtr kernel32;
        if (NativeLibrary.TryLoad(nameof(kernel32), out kernel32))
        {
            try
            {
                delegate* unmanaged[Stdcall]<int, void*> GetStdHandle;
                delegate* unmanaged[Stdcall]<void*, int*, int> GetConsoleMode;
                delegate* unmanaged[Stdcall]<void*, int, int> SetConsoleMode;
                if (!NativeLibrary.TryGetExport(kernel32, nameof(GetStdHandle), out var ptr0)
                    || !NativeLibrary.TryGetExport(kernel32, nameof(GetConsoleMode), out var ptr1)
                    || !NativeLibrary.TryGetExport(kernel32, nameof(SetConsoleMode), out var ptr2))
                {
                    return;
                }

                GetStdHandle = (delegate* unmanaged[Stdcall]<int, void*>)ptr0;
                GetConsoleMode = (delegate* unmanaged[Stdcall]<void*, int*, int>)ptr1;
                SetConsoleMode = (delegate* unmanaged[Stdcall]<void*, int, int>)ptr2;

                const int StandardOutputHandle = -11;
                var handle = GetStdHandle(StandardOutputHandle);
                int mode = 0;
                GetConsoleMode(handle, &mode);
                const int EnableVirtualTerminalProcessing = 0x0004;
                SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
            }
            finally
            {
                NativeLibrary.Free(kernel32);
            }
        }
    }
}
