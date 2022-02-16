using Microsoft.Win32.SafeHandles;
using PixivApi.Core.Local;
using PixivApi.Core.Local.Filter;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("download-original")]
    public async ValueTask<int> DownloadOriginalFileFromDatabaseAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string path,
        [Option(1, IOUtility.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        bool displayAlreadyExists = false
    )
    {
        var token = Context.CancellationToken;
        var artworks = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), config.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var alreadyCount = 0U;
        var downloadFileCount = 0;
        var downloadItemCount = 0;
        var downloadByteCount = 0UL;
        async ValueTask<bool> DownloadAsync(ulong id, string url, string fileName, CancellationToken token)
        {
            var fileInfo = new FileInfo(Path.Combine(config.OriginalFolder, $"{id & 255:X2}", fileName));
            if (fileInfo.Exists && fileInfo.Length != 0)
            {
                Interlocked.Increment(ref alreadyCount);
                if (displayAlreadyExists)
                {
                    logger.LogInformation($"{IOUtility.WarningColor}Already exists. Path: {fileInfo.FullName}{IOUtility.NormalizeColor}");
                }

                return false;
            }

            SafeFileHandle? handle = null;
            try
            {
                handle = File.OpenHandle(fileInfo.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous);
                try
                {
                    byte[] contentByteArray;
                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        var headers = request.Headers;
                        headers.Referrer = referer;
                        using var response = await client.SendAsync(request, token).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        contentByteArray = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                    }

                    var contentLength = (ulong)contentByteArray.Length;
                    Interlocked.Add(ref downloadByteCount, contentLength);
                    await RandomAccess.WriteAsync(handle, contentByteArray, 0, token).ConfigureAwait(false);
                    var donwloaded = Interlocked.Increment(ref downloadFileCount);
                    logger.LogInformation($"{IOUtility.SuccessColor}Download success. Index: {donwloaded,4} Transfer: {contentLength,20} Url: {url}{IOUtility.NormalizeColor}");
                    return true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"{IOUtility.ErrorColor}Download failed. Url: {url}{IOUtility.NormalizeColor}");
                    return false;
                }
            }
            catch (IOException)
            {
                Interlocked.Increment(ref alreadyCount);
                logger.LogInformation($"{IOUtility.WarningColor}Already exists. Path: {fileInfo.FullName}{IOUtility.NormalizeColor}");
                return false;
            }
            finally
            {
                handle?.Dispose();
            }
        }

        async ValueTask DownloadEach(Artwork artwork, CancellationToken token)
        {
            if ((downloadByteCount >> 30) >= gigaByteCount)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
            {
                await DownloadAsync(artwork.Id, artwork.GetOriginalUrl(pageIndex), artwork.GetOriginalFileName(pageIndex), token).ConfigureAwait(false);
            }
        }

        try
        {
            await Parallel.ForEachAsync(artworks, Context.CancellationToken, DownloadEach).ConfigureAwait(false);
        }
        finally
        {
            await LocalClient.ClearAsync(logger, config, false, true, Context.CancellationToken).ConfigureAwait(false);
            logger.LogInformation($"Item: {downloadItemCount}, File: {downloadFileCount}, Already: {alreadyCount}, Transfer: {ToDisplayableByteAmount(downloadByteCount)}");
        }

        return 0;
    }

    [Command("download-thumbnail")]
    public async ValueTask<int> DownloadThumbnailFileFromDatabaseAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string path,
        [Option(1, IOUtility.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        bool displayAlreadyExists = false
    )
    {
        var token = Context.CancellationToken;
        var artworks = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), config.ThumbnailFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var alreadyCount = 0U;
        var downloadFileCount = 0;
        var downloadItemCount = 0;
        var downloadByteCount = 0UL;
        async ValueTask<bool> DownloadAsync(ulong id, string url, string fileName, CancellationToken token)
        {
            var fileInfo = new FileInfo(Path.Combine(config.ThumbnailFolder, $"{id & 255:X2}", fileName));
            if (fileInfo.Exists && fileInfo.Length != 0)
            {
                Interlocked.Increment(ref alreadyCount);
                if (displayAlreadyExists)
                {
                    logger.LogInformation($"{IOUtility.WarningColor}Already exists. Path: {fileInfo.FullName}{IOUtility.NormalizeColor}");
                }

                return false;
            }

            SafeFileHandle? handle = null;
            try
            {
                handle = File.OpenHandle(fileInfo.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read, FileOptions.Asynchronous);
                try
                {
                    byte[] contentByteArray;
                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        var headers = request.Headers;
                        headers.Referrer = referer;
                        using var response = await client.SendAsync(request, token).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        contentByteArray = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                    }

                    var contentLength = (ulong)contentByteArray.Length;
                    Interlocked.Add(ref downloadByteCount, contentLength);
                    await RandomAccess.WriteAsync(handle, contentByteArray, 0, token).ConfigureAwait(false);
                    Interlocked.Increment(ref downloadFileCount);
                    logger.LogInformation($"{IOUtility.SuccessColor}Download success. Index: {downloadFileCount,4} Transfer: {contentLength,20} Url: {url}{IOUtility.NormalizeColor}");
                    return true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"{IOUtility.ErrorColor}Download failed. Url: {url}{IOUtility.NormalizeColor}");
                    return false;
                }
            }
            catch (IOException)
            {
                Interlocked.Increment(ref alreadyCount);
                logger.LogInformation($"{IOUtility.WarningColor}Already exists. Path: {fileInfo.FullName}{IOUtility.NormalizeColor}");
                return false;
            }
            finally
            {
                handle?.Dispose();
            }
        }

        async ValueTask DownloadEach(Artwork artwork, CancellationToken token)
        {
            if ((downloadByteCount >> 30) >= gigaByteCount)
            {
                cancellationTokenSource.Cancel();
                return;
            }

            await DownloadAsync(artwork.Id, artwork.GetThumbnailUrl(), artwork.GetThumbnailFileName(), token).ConfigureAwait(false);
        }

        try
        {
            await Parallel.ForEachAsync(artworks, Context.CancellationToken, DownloadEach).ConfigureAwait(false);
        }
        finally
        {
            await LocalClient.ClearAsync(logger, config, false, true, Context.CancellationToken).ConfigureAwait(false);
            logger.LogInformation($"Item: {downloadItemCount}, File: {downloadFileCount}, Already: {alreadyCount}, Transfer: {ToDisplayableByteAmount(downloadByteCount)}");
        }

        return 0;
    }

    private async ValueTask<IEnumerable<Artwork>?> PrepareDownloadFileAsync(
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
        return artworkCollection;
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
}
