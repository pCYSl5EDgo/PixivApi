using Microsoft.Extensions.Configuration;

namespace PixivApi.Console;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var app = await BuildAsync(args).ConfigureAwait(false);
        app.AddSubCommands<NetworkClient>();
        app.AddSubCommands<LocalClient>();
        app.AddSubCommands<PluginClient>();
        await app.RunAsync().ConfigureAwait(false);
    }

    private static async ValueTask<ConsoleApp> BuildAsync(string[] args)
    {
        var configSettings = await GetConfigSettingAsync().ConfigureAwait(false);
        var builder = ConsoleApp
            .CreateBuilder(args, ConfigureOptions)
            .ConfigureLogging(ConfigureLogger)
            .ConfigureHostOptions(options => options.ShutdownTimeout = configSettings.ShutdownTimeSpan)
            .ConfigureServices(services =>
            {
                _ = services
                    .AddSingleton(configSettings)
                    .AddSingleton(static provier =>
                    {
                        var config = provier.GetRequiredService<ConfigSettings>();
                        var token = provier.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
                        return FinderFacade.CreateAsync(config, token).Result;
                    })
                    .AddSingleton(static provier =>
                    {
                        var config = provier.GetRequiredService<ConfigSettings>();
                        var token = provier.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
                        return ConverterFacade.CreateAsync(config, token).Result;
                    })
                    .AddSingleton<HttpMessageHandler, SocketsHttpHandler>(static _ => new SocketsHttpHandler
                    {
                        AutomaticDecompression = DecompressionMethods.All,
                        MaxConnectionsPerServer = 2,
                    });

                _ = services.AddHttpClient(Options.DefaultName, static (provider, client) =>
                {
                    var config = provider.GetRequiredService<ConfigSettings>();
                    client.DefaultRequestVersion = new(2, 0);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    client.Timeout = config.HttpRequestTimeSpan;
                    client.AddToDefaultHeader(config);
                })
                    .ConfigurePrimaryHttpMessageHandler(ServiceProviderServiceExtensions.GetRequiredService<HttpMessageHandler>)
                    .SetHandlerLifetime(configSettings.RetryTimeSpan * 2);

                _ = services.AddHttpClient("download", static (provider, client) =>
                {
                    var config = provider.GetRequiredService<ConfigSettings>();
                    client.DefaultRequestVersion = new(2, 0);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    client.Timeout = config.HttpRequestTimeSpan;
                    client.DefaultRequestHeaders.Referrer = new("https://app-api.pixiv.net/");
                })
                    .ConfigurePrimaryHttpMessageHandler(ServiceProviderServiceExtensions.GetRequiredService<HttpMessageHandler>)
                    .SetHandlerLifetime(configSettings.RetryTimeSpan * 2);

                _ = services.AddSingleton(static provider =>
                {
                    var config = provider.GetRequiredService<ConfigSettings>();
                    var client = provider.GetRequiredService<HttpClient>();
                    return new AuthenticationHeaderValueHolder(config, client, config.ReconnectLoopIntervalTimeSpan);
                });
            });

        return builder.Build();
    }

    private static async ValueTask<ConfigSettings> GetConfigSettingAsync()
    {
        var configFileName = IOUtility.GetConfigFileNameDependsOnEnvironmentVariable();
        ConfigSettings? configSettings = null;
        if (File.Exists(configFileName))
        {
            configSettings = await IOUtility.JsonDeserializeAsync<ConfigSettings>(configFileName, CancellationToken.None).ConfigureAwait(false);
        }

        configSettings ??= new();
        if (string.IsNullOrWhiteSpace(configSettings.RefreshToken))
        {
            using var httpClient = new HttpClient();
            var valueTask = AccessTokenUtility.AuthAsync(httpClient, configSettings, CancellationToken.None);
            await InitializeDirectoriesAsync(configSettings.OriginalFolder, CancellationToken.None).ConfigureAwait(false);
            await InitializeDirectoriesAsync(configSettings.ThumbnailFolder, CancellationToken.None).ConfigureAwait(false);
            await InitializeDirectoriesAsync(configSettings.UgoiraFolder, CancellationToken.None).ConfigureAwait(false);
            configSettings.RefreshToken = await valueTask.ConfigureAwait(false) ?? string.Empty;
            await IOUtility.JsonSerializeAsync(configFileName, configSettings, FileMode.Create).ConfigureAwait(false);
        }

        return configSettings;
    }

    private static Task InitializeDirectoriesAsync(string directory, CancellationToken token) => Parallel.ForEachAsync(Enumerable.Range(0, 256), token, (index, token) =>
    {
        var folder = Path.Combine(directory, IOUtility.ByteTexts[index]);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        for (var innerIndex = 0; innerIndex < 256; innerIndex++)
        {
            var innerFolder = Path.Combine(folder, IOUtility.ByteTexts[innerIndex]);
            if (!Directory.Exists(innerFolder))
            {
                Directory.CreateDirectory(innerFolder);
            }
        }

        return ValueTask.CompletedTask;
    });

    private static void ConfigureOptions(ConsoleAppOptions options) => options.JsonSerializerOptions = IOUtility.JsonSerializerOptionsNoIndent;

    private static void ConfigureLogger(HostBuilderContext context, ILoggingBuilder builder)
    {
        var section = context.Configuration.GetSection("Logging:LogLevel:System.Net.Http.HttpClient");
        section.Value ??= "None";

        builder.AddConfiguration(context.Configuration);
        SimpleConsoleLoggerExtensions.ReplaceToSimpleConsole(builder);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            EnableConsoleVirtualCode();
        }
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
                var mode = 0;
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
