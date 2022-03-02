using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public sealed class Program
{
    private static readonly CancellationTokenSource cts = new();

    public static async Task Main(string[] args)
    {
        System.Console.CancelKeyPress += CancelKeyPress;

        using var handler = new SocketsHttpHandler()
        {
            AutomaticDecompression = DecompressionMethods.All,
            MaxConnectionsPerServer = 2,
        };

        using var httpClient = new HttpClient(handler, true)
        {
            Timeout = TimeSpan.FromHours(4),
            DefaultRequestVersion = new(2, 0),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        var configSettings = await GetConfigSettingAsync(httpClient, cts.Token).ConfigureAwait(false);

        var finderFacade = await FinderFacade.CreateAsync(configSettings, cts.Token).ConfigureAwait(false);
        var converterFacade = await ConverterFacade.CreateAsync(configSettings, cts.Token).ConfigureAwait(false);

        var builder = ConsoleApp
            .CreateBuilder(args, ConfigureOptions)
            .ConfigureHostOptions(ConfigureHostOptions)
            .ConfigureLogging(ConfigureLogger)
            .ConfigureServices(services =>
            {
                services.AddSingleton(configSettings);
                services.AddSingleton(httpClient);
                services.AddSingleton(finderFacade);
                services.AddSingleton(converterFacade);
            });

        var app = builder.Build();
        app.AddSubCommands<NetworkClient>();
        app.AddSubCommands<LocalClient>();
        await app.RunAsync(cts.Token).ConfigureAwait(false);
    }

    private static async ValueTask<ConfigSettings> GetConfigSettingAsync(HttpClient httpClient, CancellationToken token)
    {
        var configFileName = IOUtility.GetConfigFileNameDependsOnEnvironmentVariable();
        ConfigSettings? configSettings = null;
        if (File.Exists(configFileName))
        {
            configSettings = await IOUtility.JsonDeserializeAsync<ConfigSettings>(configFileName, token).ConfigureAwait(false);
        }

        configSettings ??= new();
        if (string.IsNullOrWhiteSpace(configSettings.RefreshToken))
        {
            var valueTask = AccessTokenUtility.AuthAsync(httpClient, configSettings, token);
            await InitializeDirectoriesAsync(configSettings.OriginalFolder, token).ConfigureAwait(false);
            await InitializeDirectoriesAsync(configSettings.ThumbnailFolder, token).ConfigureAwait(false);
            await InitializeDirectoriesAsync(configSettings.UgoiraFolder, token).ConfigureAwait(false);
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

    private static void ConfigureOptions(HostBuilderContext context, ConsoleAppOptions options) => options.JsonSerializerOptions = IOUtility.JsonSerializerOptionsNoIndent;

    private static void CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        cts.Cancel();
    }

    private static void ConfigureLogger(ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.SetMinimumLevel(LogLevel.Information);
        SimpleConsoleLoggerExtensions.AddSimpleConsole(builder);
        EnableConsoleVirtualCode();
    }

    private static void ConfigureHostOptions(HostBuilderContext context, HostOptions options) => options.ShutdownTimeout = TimeSpan.FromDays(1);

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
