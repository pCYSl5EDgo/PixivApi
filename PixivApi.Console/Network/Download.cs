using PixivApi.Core;
using PixivApi.Core.Local;
using System.Runtime.ExceptionServices;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("download-original")]
    public async ValueTask<int> DownloadOriginalFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("d")] bool detail = false,
        bool displayAlreadyExists = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), configSettings.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var downloadItemCount = 0;
        var failFlag = 0;
        var machine = new DownloadAsyncMachine(this, token);
        async ValueTask DownloadEach(Artwork artwork, CancellationToken token)
        {
            if (artwork.PageCount == 0)
            {
                return;
            }

            if ((machine.downloadByteCount >> 30) >= gigaByteCount)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            var pageInfos = ArrayPool<FileInfo>.Shared.Rent((int)artwork.PageCount);
            try
            {
                var haveAll = true;
                for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
                {
                    pageInfos[pageIndex] = machine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetOriginalFileName(pageIndex));
                    if (!pageInfos[pageIndex].Exists)
                    {
                        haveAll = false;
                    }
                }

                var ugoiraZipFile = artwork.Type == ArtworkType.Ugoira ? machine.PrepareFileInfo(configSettings.UgoiraFolder, artwork.Id, artwork.GetZipFileName()) : null;
                if (ugoiraZipFile is { Exists: false })
                {
                    haveAll = false;
                }

                if (haveAll)
                {
                    goto FAIL;
                }

                if (detail && !await machine.DownloadFilePrepareDetailAsync(database, artwork).ConfigureAwait(false))
                {
                    goto FAIL;
                }

                if (ugoiraZipFile is not null && !ugoiraZipFile.Exists && !await machine.DownloadAsync(artwork.GetZipUrl(), ugoiraZipFile).ConfigureAwait(false))
                {
                    goto FAIL;
                }

                for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
                {
                    var pageFile = pageInfos[pageIndex];
                    if (!pageFile.Exists && !await machine.DownloadAsync(artwork.GetOriginalUrl(pageIndex), pageFile).ConfigureAwait(false))
                    {
                        goto FAIL;
                    }
                }

                Interlocked.Increment(ref downloadItemCount);
                return;

            FAIL:
                Interlocked.Increment(ref failFlag);
            }
            finally
            {
                ArrayPool<FileInfo>.Shared.Return(pageInfos);
            }
        }

        try
        {
            await Parallel.ForEachAsync(artworks, machine.ParallelOptions, DownloadEach).ConfigureAwait(false);
        }
        finally
        {
            logger.LogInformation($"Item: {downloadItemCount}, File: {machine.downloadFileCount}, Already: {failFlag}, Transfer: {ToDisplayableByteAmount(machine.downloadByteCount)}");
            if (detail)
            {
                await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            }
            if (failFlag != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, false, true, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    [Command("download-thumbnail")]
    public async ValueTask<int> DownloadThumbnailFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("d")] bool detail = false,
        bool displayAlreadyExists = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), configSettings.ThumbnailFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var downloadItemCount = 0;
        var failFlag = 0;
        var machine = new DownloadAsyncMachine(this, token);
        async ValueTask DownloadEach(Artwork artwork, CancellationToken token)
        {
            if ((machine.downloadByteCount >> 30) >= gigaByteCount)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            var file = machine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetThumbnailUrl());
            if (!file.Exists)
            {
                goto FAIL;
            }

            if (detail && !await machine.DownloadFilePrepareDetailAsync(database, artwork).ConfigureAwait(false))
            {
                goto FAIL;
            }

            if (!await machine.DownloadAsync(artwork.GetThumbnailUrl(), file).ConfigureAwait(false))
            {
                goto FAIL;
            }


            Interlocked.Increment(ref downloadItemCount);
            return;

        FAIL:
            Interlocked.Increment(ref failFlag);
        }

        try
        {
            await Parallel.ForEachAsync(artworks, machine.ParallelOptions, DownloadEach).ConfigureAwait(false);
        }
        finally
        {
            logger.LogInformation($"Item: {downloadItemCount}, File: {machine.downloadFileCount}, Already: {failFlag}, Transfer: {ToDisplayableByteAmount(machine.downloadByteCount)}");
            if (detail)
            {
                await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            }
            if (failFlag != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, false, true, Context.CancellationToken).ConfigureAwait(false);
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
            logger.LogError($"{ArgumentDescriptions.ErrorColor}file does not exist. Path: {path}{ArgumentDescriptions.NormalizeColor}");
            return default;
        }

        if (!Directory.Exists(destinationDirectory))
        {
            logger.LogError($"{ArgumentDescriptions.ErrorColor}directory does not exist. Path: {destinationDirectory}{ArgumentDescriptions.NormalizeColor}");
            return default;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is not { Artworks.Length: > 0 })
        {
            logger.LogError($"{ArgumentDescriptions.ErrorColor}database is empty. Path: {path}{ArgumentDescriptions.NormalizeColor}");
            return default;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return default;
        }

        filter.PageCount ??= new();
        filter.PageCount.Min = 1;

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };
        var artworkCollection = await ArtworkEnumerable.CreateAsync(configSettings, database, filter, parallelOptions).ConfigureAwait(false);
        return (database, artworkCollection);
    }

    private static string ToDisplayableByteAmount(ulong byteCount)
    {
        var last = "B";
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
        public DownloadAsyncMachine(NetworkClient networkClient, CancellationToken token)
        {
            client = networkClient.client;
            logger = networkClient.logger;
            this.networkClient = networkClient;
            ParallelOptions = new()
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = networkClient.configSettings.MaxParallel,
            };
        }

        private readonly HttpClient client;
        private readonly ILogger logger;
        private readonly NetworkClient networkClient;
        public readonly ParallelOptions ParallelOptions;
        public int downloadFileCount = 0;
        public ulong downloadByteCount = 0UL;
        private ulong retryPair = 0UL;

        public FileInfo PrepareFileInfo(string folder, ulong id, string fileName)
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
            var (byteCount, exception) = await PassThroughFromHttpGetQueryToFileAsync(url, file).ConfigureAwait(false);
            switch (exception)
            {
                case null:
                    Interlocked.Add(ref downloadByteCount, byteCount);
                    var donwloaded = Interlocked.Increment(ref downloadFileCount);
                    logger.LogInformation($"{ArgumentDescriptions.SuccessColor}Download success. Index: {donwloaded,4} Transfer: {byteCount,20} Url: {url}{ArgumentDescriptions.NormalizeColor}");
                    return true;
                case TaskCanceledException:
                    ExceptionDispatchInfo.Throw(exception);
                    throw new Exception();
                default:
                    logger.LogError(exception, $"{ArgumentDescriptions.ErrorColor}Download failed. Url: {url}{ArgumentDescriptions.NormalizeColor}");
                    return false;
            }
        }

        private async ValueTask<(ulong ByteCount, Exception? Exception)> PassThroughFromHttpGetQueryToFileAsync(string url, FileInfo file)
        {
            try
            {
                using var stream = new FileStream(file.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 8192, true);
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var headers = request.Headers;
                headers.Referrer = referer;
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ParallelOptions.CancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(stream, ParallelOptions.CancellationToken).ConfigureAwait(false);
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
                    var detailArtwork = await networkClient.GetArtworkDetailAsync(artwork.Id, ParallelOptions.CancellationToken).ConfigureAwait(false);
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
                        var timeSpan = networkClient.configSettings.RetryTimeSpan;
                        logger.LogError(e, $"let me just sleep for {timeSpan.TotalSeconds} seconds.");
                        await Task.Delay(timeSpan, ParallelOptions.CancellationToken).ConfigureAwait(false);
                        while (myMatterIndex == (Interlocked.Read(ref retryPair) >> 1))
                        {
                            if ((pair & 1UL) != 0)
                            {
                                await Task.Delay(timeSpan, ParallelOptions.CancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            if (Interlocked.CompareExchange(ref retryPair, pair + 1UL, pair) != pair)
                            {
                                await Task.Delay(timeSpan, ParallelOptions.CancellationToken).ConfigureAwait(false);
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
