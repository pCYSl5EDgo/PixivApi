using Microsoft.Extensions.FileProviders;
using PixivApi.Core;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    cancellationTokenSource.Cancel();
    e.Cancel = true;
}

var token = cancellationTokenSource.Token;
var configSettings = await IOUtility.JsonDeserializeAsync<ConfigSettings>(IOUtility.GetConfigFileNameDependsOnEnvironmentVariable(), token).ConfigureAwait(false);
if (string.IsNullOrWhiteSpace(configSettings?.RefreshToken))
{
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel();

var app = builder.Build();
var isDevelopment = builder.Environment.IsDevelopment();

if (!isDevelopment)
{
    app.UseHsts();
}

app.UseHttpsRedirection();

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

app.UseFileServer();
app.UseStaticFiles(new StaticFileOptions(originalSharedProvider));
app.UseStaticFiles(new StaticFileOptions(thumbnailSharedProvider));
app.UseStaticFiles(new StaticFileOptions(ugoiraSharedProvider));

if (isDevelopment)
{
    app.UseDirectoryBrowser(new DirectoryBrowserOptions(originalSharedProvider));
    app.UseDirectoryBrowser(new DirectoryBrowserOptions(thumbnailSharedProvider));
    app.UseDirectoryBrowser(new DirectoryBrowserOptions(ugoiraSharedProvider));
}

app.Run();
