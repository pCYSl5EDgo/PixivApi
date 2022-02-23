using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("download-original")]
    public async ValueTask<int> DownloadOriginalFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), configSettings.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var downloadItemCount = 0;
        var alreadyCount = 0;
        var updateFlag = false;
        var machine = new DownloadAsyncMachine(this, database, pipe, token);
        try
        {
            await foreach (var artwork in artworks)
            {
                if ((machine.DownloadByteCount >> 30) >= gigaByteCount)
                {
                    break;
                }

                machine.Initialize(artwork);
                if (artwork.Type == ArtworkType.Ugoira)
                {
                    var ugoiraZipFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.UgoiraFolder, artwork.Id, artwork.GetZipFileName());
                    if (!await machine.DownloadAsync(artwork.GetZipUrl(), ugoiraZipFile).ConfigureAwait(false))
                    {
                        goto FAIL;
                    }
                }

                for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
                {
                    var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetOriginalFileName(pageIndex));
                    if (!await machine.DownloadAsync(artwork.GetOriginalUrl(pageIndex), pageFile).ConfigureAwait(false))
                    {
                        goto FAIL;
                    }
                }

                if (machine.IsUpdated)
                {
                    updateFlag = true;
                }

                ++downloadItemCount;
                continue;

            FAIL:
                if (machine.IsUpdated)
                {
                    updateFlag = true;
                }

                ++alreadyCount;
            }
        }
        finally
        {
            if (!pipe)
            {
                logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ToDisplayableByteAmount(machine.DownloadByteCount)}");
            }

            if (updateFlag)
            {
                await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            }
            if (alreadyCount != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    [Command("download-thumbnail")]
    public async ValueTask<int> DownloadThumbnailFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), configSettings.ThumbnailFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var downloadItemCount = 0;
        var alreadyCount = 0;
        var updateFlag = false;
        var machine = new DownloadAsyncMachine(this, database, pipe, token);
        try
        {
            await foreach (var artwork in artworks)
            {
                if ((machine.DownloadByteCount >> 30) >= gigaByteCount)
                {
                    break;
                }

                machine.Initialize(artwork);
                if (artwork.Type == ArtworkType.Ugoira)
                {
                    var thumbnailFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetThumbnailFileName(0));
                    if (!await machine.DownloadAsync(artwork.GetThumbnailUrl(0), thumbnailFile).ConfigureAwait(false))
                    {
                        goto FAIL;
                    }
                }
                else
                {
                    for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
                    {
                        var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetThumbnailFileName(pageIndex));
                        if (!await machine.DownloadAsync(artwork.GetThumbnailUrl(pageIndex), pageFile).ConfigureAwait(false))
                        {
                            goto FAIL;
                        }
                    }
                }

                if (machine.IsUpdated)
                {
                    updateFlag = true;
                }

                ++downloadItemCount;
                continue;

            FAIL:
                if (machine.IsUpdated)
                {
                    updateFlag = true;
                }

                ++alreadyCount;
            }
        }
        finally
        {
            if (!pipe)
            {
                logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ToDisplayableByteAmount(machine.DownloadByteCount)}");
            }

            if (updateFlag)
            {
                await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            }

            if (alreadyCount != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    private async ValueTask<(DatabaseFile, IAsyncEnumerable<Artwork>?)> PrepareDownloadFileAsync(
        string path,
        ArtworkFilter? filter,
        string destinationDirectory,
        ulong gigaByteCount
    )
    {
        if (filter is null || gigaByteCount == 0)
        {
            return default;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogError($"{ConsoleUtility.ErrorColor}file does not exist. Path: {path}{ConsoleUtility.NormalizeColor}");
            return default;
        }

        if (!Directory.Exists(destinationDirectory))
        {
            logger.LogError($"{ConsoleUtility.ErrorColor}directory does not exist. Path: {destinationDirectory}{ConsoleUtility.NormalizeColor}");
            return default;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is not { Artworks.Length: > 0 })
        {
            logger.LogError($"{ConsoleUtility.ErrorColor}database is empty. Path: {path}{ConsoleUtility.NormalizeColor}");
            return default;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return default;
        }

        filter.PageCount ??= new();
        filter.PageCount.Min ??= 1;

        var artworkCollection = FilterExtensions.CreateAsyncEnumerable(configSettings, database, filter, token);
        return (database, artworkCollection);
    }

    private static string ToDisplayableByteAmount(ulong byteCount)
    {
        if (byteCount < (1 << 10))
        {
            return $"{byteCount} B";
        }
        else if (byteCount < (1 << 20))
        {
            return $"{byteCount >> 10} KB + {byteCount & 1023} B";
        }
        else if (byteCount < (1 << 30))
        {
            return $"{byteCount >> 20} MB + {(byteCount >> 10) & 1023} KB + {byteCount & 1023} B";
        }
        else
        {
            return $"{byteCount >> 30} GB + {(byteCount >> 20) & 1023} MB + {(byteCount >> 10) & 1023} KB + {byteCount & 1023} B";
        }
    }

    private static readonly Uri referer = new("https://app-api.pixiv.net/");

    /// <summary>
    /// Do not use concurrently.
    /// </summary>
    private sealed class DownloadAsyncMachine
    {
        public DownloadAsyncMachine(NetworkClient networkClient, DatabaseFile database, bool pipe, CancellationToken token)
        {
            client = networkClient.client;
            logger = networkClient.logger;
            this.networkClient = networkClient;
            this.database = database;
            this.pipe = pipe;
            this.token = token;
        }

        private readonly HttpClient client;
        private readonly ILogger logger;
        private readonly NetworkClient networkClient;
        private readonly DatabaseFile database;
        private readonly bool pipe;
        private readonly CancellationToken token;
        public int DownloadFileCount = 0;
        public ulong DownloadByteCount = 0UL;
        private Artwork? artwork;
        private bool noDetailDownload = true;
        public bool IsUpdated { get; private set; }

        public static FileInfo PrepareFileInfo(string folder, ulong id, string fileName)
        {
            DefaultInterpolatedStringHandler handler = $"{folder}";
            if (folder.Length != 0 && folder[^1] != '/' && folder[^1] != '\\')
            {
                handler.AppendLiteral("/");
            }

            IOUtility.AppendHashPath(ref handler, id);
            handler.AppendFormatted(fileName);
            var path = handler.ToStringAndClear();
            return new(path);
        }

        public async ValueTask<bool> DownloadAsync(string url, FileInfo file)
        {
            if (file.Exists)
            {
                return false;
            }

            ulong byteCount = 0;
            try
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var headers = request.Headers;
                headers.Referrer = referer;
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                using var stream = new FileStream(file.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 8192, true);
                await response.Content.CopyToAsync(stream, token).ConfigureAwait(false);
                byteCount = (ulong)stream.Length;
            }
            catch (HttpRequestException e) when (noDetailDownload && e.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return await DownloadFilePrepareDetailAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (!pipe)
                {
                    logger.LogError(e, $"{ConsoleUtility.ErrorColor}Download failed. Url: {url}{ConsoleUtility.NormalizeColor}");
                }

                return false;
            }

            DownloadByteCount += byteCount;
            ++DownloadFileCount;
            if (!pipe)
            {
                logger.LogInformation($"{ConsoleUtility.SuccessColor}Download success. Index: {DownloadFileCount,6} Transfer: {byteCount,20} Url: {url}{ConsoleUtility.NormalizeColor}");
            }

            return true;
        }

        private async ValueTask<bool> DownloadFilePrepareDetailAsync()
        {
            if (artwork is null)
            {
                throw new NullReferenceException();
            }

            noDetailDownload = false;
            bool success;
        RETRY:
            try
            {
                var detailArtwork = await networkClient.GetArtworkDetailAsync(artwork.Id, pipe, token).ConfigureAwait(false);
                var converted = Artwork.ConvertFromNetwrok(detailArtwork, database.TagSet, database.ToolSet, database.UserDictionary);
                artwork.Overwrite(converted);
                success = !converted.IsOfficiallyRemoved;
            }
            catch (HttpRequestException e) when (e.StatusCode.HasValue)
            {
                if (e.StatusCode.Value == System.Net.HttpStatusCode.NotFound)
                {
                    artwork.IsOfficiallyRemoved = true;
                }
                else if (e.StatusCode.Value == System.Net.HttpStatusCode.BadRequest)
                {
                    var timeSpan = networkClient.configSettings.RetryTimeSpan;
                    if (!pipe)
                    {
                        logger.LogError(e, $"let me just sleep for {timeSpan.TotalSeconds} seconds.");
                    }

                    await Task.Delay(timeSpan, token).ConfigureAwait(false);
                    if (!await networkClient.Reconnect().ConfigureAwait(false))
                    {
                        throw;
                    }

                    goto RETRY;
                }

                success = false;
            }

            if (!pipe)
            {
                logger.LogInformation($"Detail success: {success} Id: {artwork.Id,20}");
            }

            return success;
        }

        public void Initialize(Artwork artwork)
        {
            this.artwork = artwork;
            noDetailDownload = true;
            IsUpdated = false;
        }
    }
}
