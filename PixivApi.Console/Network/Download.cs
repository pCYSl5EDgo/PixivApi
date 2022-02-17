using PixivApi.Core.Local;
using PixivApi.Core.Local.Filter;
using System.Runtime.ExceptionServices;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("download-original")]
    public async ValueTask<int> DownloadOriginalFileFromDatabaseAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string path,
        [Option(1, IOUtility.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("d")] bool detail = false,
        bool displayAlreadyExists = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), config.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var failFlag = 0;
        var machine = new DownloadAsyncMachine(this, displayAlreadyExists, token);
        async ValueTask DownloadEach(Artwork artwork, CancellationToken token)
        {
            if ((machine.downloadByteCount >> 30) >= gigaByteCount)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            bool success = true;
            if (detail)
            {
                success = await machine.DownloadFilePrepareDetailAsync(database, artwork).ConfigureAwait(false);
            }

            if (success && artwork.Type == ArtworkType.Ugoira)
            {
                success = await machine.DownloadAsync(config.UgoiraFolder, artwork.Id, artwork.GetZipUrl(), artwork.GetZipFileName()).ConfigureAwait(false);
            }

            for (uint pageIndex = 0; success && pageIndex < artwork.PageCount; pageIndex++)
            {
                success = await machine.DownloadAsync(config.OriginalFolder, artwork.Id, artwork.GetOriginalUrl(pageIndex), artwork.GetOriginalFileName(pageIndex)).ConfigureAwait(false);
            }

            if (!success)
            {
                Interlocked.Exchange(ref failFlag, 1);
            }
        }

        try
        {
            await Parallel.ForEachAsync(artworks, token, DownloadEach).ConfigureAwait(false);
        }
        finally
        {
            logger.LogInformation($"Item: {machine.downloadItemCount}, File: {machine.downloadFileCount}, Already: {failFlag}, Transfer: {ToDisplayableByteAmount(machine.downloadByteCount)}");
            if (detail)
            {
                await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            }
            if (failFlag != 0)
            {
                await LocalClient.ClearAsync(logger, config, false, true, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    [Command("download-thumbnail")]
    public async ValueTask<int> DownloadThumbnailFileFromDatabaseAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string path,
        [Option(1, IOUtility.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("d")] bool detail = false,
        bool displayAlreadyExists = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), config.ThumbnailFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var failFlag = 0;
        var machine = new DownloadAsyncMachine(this, displayAlreadyExists, token);
        async ValueTask DownloadEach(Artwork artwork, CancellationToken token)
        {
            if ((machine.downloadByteCount >> 30) >= gigaByteCount)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            bool success = true;
            if (detail)
            {
                success = await machine.DownloadFilePrepareDetailAsync(database, artwork).ConfigureAwait(false);
            }

            if (success)
            {
                success = await machine.DownloadAsync(config.ThumbnailFolder, artwork.Id, artwork.GetThumbnailUrl(), artwork.GetThumbnailFileName()).ConfigureAwait(false);
            }

            if (!success)
            {
                Interlocked.Exchange(ref failFlag, 1);
            }
        }

        try
        {
            await Parallel.ForEachAsync(artworks, token, DownloadEach).ConfigureAwait(false);
        }
        finally
        {
            logger.LogInformation($"Item: {machine.downloadItemCount}, File: {machine.downloadFileCount}, Already: {failFlag}, Transfer: {ToDisplayableByteAmount(machine.downloadByteCount)}");
            if (detail)
            {
                await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            }
            if (failFlag != 0)
            {
                await LocalClient.ClearAsync(logger, config, false, true, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }



    private async ValueTask<(DatabaseFile, IEnumerable<Artwork>?)> PrepareDownloadFileAsync(
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
            logger.LogError($"{IOUtility.ErrorColor}file does not exist. Path: {path}{IOUtility.NormalizeColor}");
            return default;
        }

        if (!Directory.Exists(destinationDirectory))
        {
            logger.LogError($"{IOUtility.ErrorColor}directory does not exist. Path: {destinationDirectory}{IOUtility.NormalizeColor}");
            return default;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is not { Artworks.Length: > 0 })
        {
            logger.LogError($"{IOUtility.ErrorColor}database is empty. Path: {path}{IOUtility.NormalizeColor}");
            return default;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return default;
        }

        filter.PageCount ??= new();
        filter.PageCount.Min = 1;

        var artworkCollection = await ArtworkEnumerable.CreateAsync(database, filter, token).ConfigureAwait(false);
        return (database, artworkCollection);
    }

    private static string ToDisplayableByteAmount(ulong byteCount)
    {
        string last = "B";
        if (byteCount >= 1024)
        {
            byteCount >>= 10;
            last = "KB";

            if (byteCount > 1024)
            {
                byteCount >>= 10;
                last = "MB";

                if (byteCount > 1024)
                {
                    return $"{byteCount >> 10}GB + {byteCount & 1023} MB";
                }
            }
        }

        return $"{byteCount} {last}";
    }

    private static readonly Uri referer = new("https://app-api.pixiv.net/");

    private sealed class DownloadAsyncMachine
    {
        public DownloadAsyncMachine(NetworkClient networkClient, bool displayAlreadyExists, CancellationToken token)
        {
            client = networkClient.client;
            logger = networkClient.logger;
            this.networkClient = networkClient;
            this.displayAlreadyExists = displayAlreadyExists;
            this.token = token;
        }

        private readonly HttpClient client;
        private readonly ILogger logger;
        private readonly NetworkClient networkClient;
        private readonly bool displayAlreadyExists;
        private readonly CancellationToken token;
        public int downloadFileCount = 0;
        public int downloadItemCount = 0;
        public ulong downloadByteCount = 0UL;
        private ulong retryPair = 0UL;

        public async ValueTask<bool> DownloadAsync(string folder, ulong id, string url, string fileName)
        {
            var fileInfo = new FileInfo(Path.Combine(folder, $"{id & 255:X2}", fileName));
            if (fileInfo.Exists)
            {
                if (displayAlreadyExists)
                {
                    logger.LogInformation($"{IOUtility.WarningColor}Already exists. Path: {fileInfo.FullName}{IOUtility.NormalizeColor}");
                }

                return fileInfo.Length != 0;
            }

            var (byteCount, exception) = await PassThroughFromHttpGetQueryToFileAsync(url, fileInfo.FullName).ConfigureAwait(false);
            switch (exception)
            {
                case null:
                    Interlocked.Add(ref downloadByteCount, byteCount);
                    var donwloaded = Interlocked.Increment(ref downloadFileCount);
                    logger.LogInformation($"{IOUtility.SuccessColor}Download success. Index: {donwloaded,4} Transfer: {byteCount,20} Url: {url}{IOUtility.NormalizeColor}");
                    return true;
                case TaskCanceledException:
                    ExceptionDispatchInfo.Throw(exception);
                    throw null;
                default:
                    logger.LogError(exception, $"{IOUtility.ErrorColor}Download failed. Url: {url}{IOUtility.NormalizeColor}");
                    return false;
            }
        }

        private async ValueTask<(ulong ByteCount, Exception? Exception)> PassThroughFromHttpGetQueryToFileAsync(string url, string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 8192, true);
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var headers = request.Headers;
                headers.Referrer = referer;
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(stream, token).ConfigureAwait(false);
                var byteCount = (ulong)stream.Length;
                return (byteCount, null);
            }
            catch (Exception e)
            {
                return (0, e);
            }
        }

        public async ValueTask<bool> DownloadFilePrepareDetailAsync(DatabaseFile database, Artwork artwork)
        {
            bool success;
            do
            {
                try
                {
                    var detailArtwork = await networkClient.GetArtworkDetailAsync(artwork.Id, token).ConfigureAwait(false);
                    var converted = Artwork.ConvertFromNetwrok(detailArtwork, database.TagSet, database.ToolSet, database.UserDictionary);
                    artwork.Overwrite(converted);
                    success = !converted.IsOfficiallyRemoved;
                }
                catch (HttpRequestException e)
                {
                    if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        artwork.IsOfficiallyRemoved = true;
                    }
                    else if (e.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var pair = Interlocked.Read(ref retryPair);
                        var myMatterIndex = pair >> 1;
                        var timeSpan = networkClient.config.RetryTimeSpan;
                        logger.LogError(e, $"let me just sleep for {timeSpan.TotalSeconds} seconds.");
                        await Task.Delay(timeSpan, token).ConfigureAwait(false);
                        while (myMatterIndex == (Interlocked.Read(ref retryPair) >> 1))
                        {
                            if ((pair & 1UL) != 0)
                            {
                                await Task.Delay(timeSpan, token).ConfigureAwait(false);
                                continue;
                            }
                            
                            if (Interlocked.CompareExchange(ref retryPair, pair + 1UL, pair) != pair)
                            {
                                await Task.Delay(timeSpan, token).ConfigureAwait(false);
                                continue;
                            }

                            if (!await networkClient.Reconnect().ConfigureAwait(false))
                            {
                                ExceptionDispatchInfo.Throw(e);
                            }

                            Interlocked.Increment(ref retryPair);
                            break;
                        }

                        continue;
                    }

                    success = false;
                }

                break;
            } while (true);
            logger.LogInformation($"Detail success: {success} Id: {artwork.Id,20}");
            return success;
        }
    }
}
