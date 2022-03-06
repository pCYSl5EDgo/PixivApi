using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace PixivApi.Site;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += Console_CancelKeyPress;

        void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            cancellationTokenSource.Cancel();
            e.Cancel = true;
        }

        var token = cancellationTokenSource.Token;
        using var handler = new SocketsHttpHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            MaxConnectionsPerServer = 2,
        };

        using var httpClient = new HttpClient(handler, true)
        {
            Timeout = TimeSpan.FromHours(4),
            DefaultRequestVersion = new(2, 0),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        var configSettings = await GetConfigSettingAsync(httpClient, token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(configSettings.DatabaseFilePath, token).ConfigureAwait(false) ?? new();
        try
        {
            await using var finderFacade = await FinderFacade.CreateAsync(configSettings, cancellationTokenSource.Token).ConfigureAwait(false);
            await using var converterFacade = await ConverterFacade.CreateAsync(configSettings, cancellationTokenSource.Token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(configSettings?.RefreshToken))
            {
                return;
            }

            var jsonSerializerOptions = IOUtility.JsonSerializerOptionsNoIndent;
            jsonSerializerOptions.Converters.Add(UgoiraArtworkUtilityStruct.Converter.Instance);
            jsonSerializerOptions.Converters.Add(NotUgoiraArtworkUtilityStruct.Converter.Instance);
            var api = new Api(configSettings, httpClient, database, finderFacade, converterFacade, jsonSerializerOptions);

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseKestrel();

            var app = builder.Build();
            var isDevelopment = builder.Environment.IsDevelopment();

            if (!isDevelopment)
            {
                app.UseHsts();
            }

            var originalSharedProvider = new Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.GetFullPath(configSettings.OriginalFolder)),
                RequestPath = "/Original",
            };
            var thumbnailSharedProvider = new Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.GetFullPath(configSettings.ThumbnailFolder)),
                RequestPath = "/Thumbnail",
            };
            var ugoiraSharedProvider = new Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.GetFullPath(configSettings.UgoiraFolder)),
                RequestPath = "/Ugoira",
            };

            var contetTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>()
            {
                { ".png", "image/png" },
                { ".jpg", "image/jpeg" },
                { ".jxl", "image/jxl" },
            });

            app.UseStaticFiles(new StaticFileOptions(originalSharedProvider) { ContentTypeProvider = contetTypeProvider });
            app.UseStaticFiles(new StaticFileOptions(thumbnailSharedProvider) { ContentTypeProvider = contetTypeProvider });
            app.UseStaticFiles(new StaticFileOptions(ugoiraSharedProvider) { ContentTypeProvider = contetTypeProvider });

            if (isDevelopment)
            {
                app.UseDirectoryBrowser(new DirectoryBrowserOptions(originalSharedProvider));
                app.UseDirectoryBrowser(new DirectoryBrowserOptions(thumbnailSharedProvider));
                app.UseDirectoryBrowser(new DirectoryBrowserOptions(ugoiraSharedProvider));
            }

            app.MapGet("/local/count", api.CountAsync);
            app.MapGet("/local/map", api.MapAsync);
            app.Map("/local/hide/{id}", api.HideAsync);

            app.Run();
        }
        finally
        {
            await IOUtility.MessagePackSerializeAsync(configSettings.DatabaseFilePath, database, FileMode.Create).ConfigureAwait(false);
        }
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
}
