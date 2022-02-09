using Microsoft.Win32.SafeHandles;

namespace PixivApi;

partial class NetworkClient
{
    [Command("download-original")]
    public ValueTask<int> DownloadOriginalFileFromDatabaseAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string path,
        [Option(1, IOUtility.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        bool displayAlreadyExists = false
    ) => DownloadFileAsync(path, filter, gigaByteCount, displayAlreadyExists, config.OriginalFolder, item => (item.MetaSinglePage.OriginalImageUrl, item.MetaPages), urls => urls.Original);

    [Command("download-thumbnail")]
    public ValueTask<int> DownloadThumbnailFileFromDatabaseAsync(
        [Option(0, $"input {IOUtility.ArtworkDatabaseDescription}")] string path,
        [Option(1, IOUtility.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        bool displayAlreadyExists = false
    ) => DownloadFileAsync(path, filter, gigaByteCount, displayAlreadyExists, config.ThumbnailFolder, item => (item.ImageUrls.SquareMedium, Array.Empty<MetaPage>()), null);

    private async ValueTask<int> DownloadFileAsync(string path, string filter, ulong gigaByteCount, bool displayAlreadyExists, string destinationDirectory, Func<ArtworkDatabaseInfo, (string? Url, MetaPage[] MetaPages)> selector, Func<ImageUrls, string?>? urlSelector)
    {
        path = IOUtility.FindArtworkDatabase(path, true)!;
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogError($"{IOUtility.ErrorColor}file does not exist. Path: {path}{IOUtility.NormalizeColor}");
            return -1;
        }

        var output = Path.GetFullPath(destinationDirectory);
        if (output is null || !Directory.Exists(output))
        {
            logger.LogError($"{IOUtility.ErrorColor}directory does not exist. Path: {output}{IOUtility.NormalizeColor}");
            return -2;
        }

        var alreadyCount = 0U;
        var downloadFileCount = 0;
        var downloadItemCount = 0;
        var downloadByteCount = 0UL;
        var token = Context.CancellationToken;
        var artworkItemArray = await IOUtility.MessagePackDeserializeAsync<ArtworkDatabaseInfo[]>(path, token).ConfigureAwait(false);
        if (artworkItemArray is not { Length: > 0 })
        {
            logger.LogError($"{IOUtility.ErrorColor}database is empty. Path: {path}{IOUtility.NormalizeColor}");
            return -2;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        async ValueTask<bool> DownloadAsync(string? page)
        {
            if (string.IsNullOrWhiteSpace(page))
            {
                return false;
            }

            var pageFileName = IOUtility.GetFileNameFromUri(page);
            if (string.IsNullOrWhiteSpace(pageFileName))
            {
                return false;
            }

            var fileInfo = new FileInfo(Path.Combine(output, pageFileName));
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
                    using (var request = new HttpRequestMessage(HttpMethod.Get, page))
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
                    logger.LogInformation($"{IOUtility.SuccessColor}Download success. Index: {downloadFileCount,4} Transfer: {contentLength,20} Url: {page}{IOUtility.NormalizeColor}");
                    return true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"{IOUtility.ErrorColor}Download failed. Url: {page}{IOUtility.NormalizeColor}");
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

        async ValueTask DownloadEach(ArtworkDatabaseInfo artwork, CancellationToken token)
        {
            if ((downloadByteCount >> 30) >= gigaByteCount)
            {
                return;
            }

            var (url, metaPages) = selector(artwork);

            if (!string.IsNullOrWhiteSpace(url))
            {
                if (await DownloadAsync(url).ConfigureAwait(false))
                {
                    Interlocked.Increment(ref downloadItemCount);
                }
                return;
            }

            if (urlSelector is null)
            {
                return;
            }
            
            var anyDownload = false;
            foreach (var page in metaPages)
            {
                anyDownload |= await DownloadAsync(urlSelector(page.ImageUrls)).ConfigureAwait(false);
            }

            if (anyDownload)
            {
                Interlocked.Increment(ref downloadItemCount);
            }
        }

        var artworkItemFilter = await IOUtility.JsonParseAsync<ArtworkDatabaseInfoFilter>(filter, token).ConfigureAwait(false);
        IEnumerable<ArtworkDatabaseInfo> artworkCollection;
        if (artworkItemFilter is not null)
        {
            artworkCollection = await ArtworkDatabaseInfoEnumerable.CreateAsync(artworkItemArray, artworkItemFilter, token).ConfigureAwait(false);
        }
        else
        {
            artworkCollection = artworkItemArray;
        }

        artworkCollection = artworkCollection.Where(artwork => artwork.PageCount != 0 && artwork.Visible && !artwork.IsMuted);
        await Parallel.ForEachAsync(artworkCollection, token, DownloadEach).ConfigureAwait(false);
        logger.LogInformation($"Item: {downloadItemCount}, File: {downloadFileCount}, Already: {alreadyCount}, Transfer: {ShowByte(downloadByteCount)}");
        return 0;

        static string ShowByte(ulong byteCount)
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
    }

    private static readonly Uri referer = new("https://app-api.pixiv.net/");
}
