using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace PixivApi.Site;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        _ = builder.WebHost.UseKestrel();
        _ = builder.Services.AddHttpClient("pixiv").ConfigurePrimaryHttpMessageHandler(provider => new SocketsHttpHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            MaxConnectionsPerServer = 2,
        }).SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        _ = builder.Services.AddSingleton(provider =>
        {
            var lifetime = provider.GetRequiredService<IHostApplicationLifetime>();
            var client = provider.GetRequiredService<HttpClient>();
#pragma warning disable CA2012
            return GetConfigSettingAsync(client, lifetime.ApplicationStopping).Result;
#pragma warning restore CA2012
        })
        .AddSingleton(provider =>
        {
            var lifetime = provider.GetRequiredService<IHostApplicationLifetime>();
            var configSettings = provider.GetRequiredService<ConfigSettings>();
            if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
            {
                return new();
            }

            using var stream = File.OpenRead(configSettings.DatabaseFilePath);
            return MessagePack.MessagePackSerializer.Deserialize<DatabaseFile>(stream, null, lifetime.ApplicationStopping) ?? new();
        })
        .AddSingleton(provider =>
        {
            var lifetime = provider.GetRequiredService<IHostApplicationLifetime>();
            var configSettings = provider.GetRequiredService<ConfigSettings>();
#pragma warning disable CA2012
            return FinderFacade.CreateAsync(configSettings, lifetime.ApplicationStopping).Result;
#pragma warning restore CA2012
        })
        .AddSingleton(provider =>
        {
            var lifetime = provider.GetRequiredService<IHostApplicationLifetime>();
            var configSettings = provider.GetRequiredService<ConfigSettings>();
#pragma warning disable CA2012
            return ConverterFacade.CreateAsync(configSettings, lifetime.ApplicationStopping).Result;
#pragma warning restore CA2012
        })
        .AddSingleton(provider =>
        {
            var jsonSerializerOptions = IOUtility.JsonSerializerOptionsNoIndent;
            jsonSerializerOptions.Converters.Add(UgoiraArtworkUtilityStruct.Converter.Instance);
            jsonSerializerOptions.Converters.Add(NotUgoiraArtworkUtilityStruct.Converter.Instance);
            return jsonSerializerOptions;
        });

        var app = builder.Build();
        var isDevelopment = builder.Environment.IsDevelopment();

        if (!isDevelopment)
        {
            app.UseHsts();
        }

        var configSettings = app.Services.GetRequiredService<ConfigSettings>();
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
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

        app.MapGet("/local/count", Api.CountAsync);
        app.MapGet("/local/map", Api.MapAsync);
        app.Map("/local/hide/{id}", Api.HideAsync);

        var database = app.Services.GetRequiredService<DatabaseFile>();
        try
        {
            await app.RunAsync().ConfigureAwait(false);
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
