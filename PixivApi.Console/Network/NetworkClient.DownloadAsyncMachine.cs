using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class NetworkClient
{
    private sealed class DownloadAsyncMachine
    {
        public DownloadAsyncMachine(NetworkClient networkClient, DatabaseFile database, AuthenticationHeaderValue authentication, bool pipe, CancellationToken token)
        {
            client = networkClient.client;
            logger = networkClient.logger;
            this.networkClient = networkClient;
            this.database = database;
            this.authentication = authentication;
            this.pipe = pipe;
            this.token = token;
        }

        private readonly HttpClient client;
        private readonly ILogger logger;
        private readonly NetworkClient networkClient;
        private readonly DatabaseFile database;
        private AuthenticationHeaderValue authentication;
        private readonly bool pipe;
        private readonly CancellationToken token;
        public int DownloadFileCount = 0;
        public ulong DownloadByteCount = 0UL;

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

        public async ValueTask<(bool Success, bool NoDetailDownload)> DownloadAsync(FileInfo file, Artwork artwork, IConverter? converter, bool noDetailDownload, Func<uint, string> calcUrl, uint index)
        {
            string url;
            ulong byteCount;
            bool? branch;
            do
            {
                token.ThrowIfCancellationRequested();
                url = calcUrl(index);
                (branch, byteCount, noDetailDownload) = await PrivateRetryDownloadAsync(artwork, file, converter, url, noDetailDownload).ConfigureAwait(false);
                if (branch == false)
                {
                    return (false, noDetailDownload);
                }
            } while (branch == null);

            Interlocked.Add(ref DownloadByteCount, byteCount);
            Interlocked.Increment(ref DownloadFileCount);
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightBlueColor}Download success. Index: {DownloadFileCount,6} Transfer: {byteCount,20} Url: {url}{VirtualCodes.NormalizeColor}");
            }

            return (true, noDetailDownload);
        }

        public async ValueTask<(bool Success, bool NoDetailDownload)> DownloadAsync(FileInfo file, Artwork artwork, IConverter? converter, bool noDetailDownload, Func<string> calcUrl)
        {
            string url;
            ulong byteCount;
            bool? branch;
            do
            {
                token.ThrowIfCancellationRequested();
                url = calcUrl();
                (branch, byteCount, noDetailDownload) = await PrivateRetryDownloadAsync(artwork, file, converter, url, noDetailDownload).ConfigureAwait(false);
                if (branch == false)
                {
                    return (false, noDetailDownload);
                }
            } while (branch == null);

            Interlocked.Add(ref DownloadByteCount, byteCount);
            Interlocked.Increment(ref DownloadFileCount);
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightBlueColor}Download success. Index: {DownloadFileCount,6} Transfer: {byteCount,20} Url: {url}{VirtualCodes.NormalizeColor}");
            }

            return (true, noDetailDownload);
        }

        private async ValueTask<(bool?, ulong, bool)> PrivateRetryDownloadAsync(Artwork artwork, FileInfo file, IConverter? converter, string url, bool noDetailDownload)
        {
            HttpResponseMessage response;
            ulong byteCount;
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var headers = request.Headers;
                headers.Referrer = referer;
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            }

            try
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    Interlocked.Exchange(ref authentication, await networkClient.ReconnectAsync(pipe, token).ConfigureAwait(false));
                    return (default, default, noDetailDownload);
                }

                if (noDetailDownload && response.StatusCode == HttpStatusCode.NotFound)
                {
                    if (!pipe)
                    {
                        logger.LogError($"{VirtualCodes.BrightRedColor}Not Found: {url}{VirtualCodes.NormalizeColor}");
                    }

                    return (await DownloadFilePrepareDetailAsync(artwork).ConfigureAwait(false) ? null : false, default, false);
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (!pipe)
                    {
                        logger.LogError($"{VirtualCodes.BrightRedColor}Download failed. Url: {url}{VirtualCodes.NormalizeColor}");
                    }

                    return (false, default, noDetailDownload);
                }

                using var stream = new FileStream(file.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 8192, true);
                // Write to the file should not be cancelled.
                await response.Content.CopyToAsync(stream, CancellationToken.None).ConfigureAwait(false);
                byteCount = (ulong)stream.Length;
            }
            finally
            {
                response.Dispose();
            }

            _ = converter?.TryConvertAsync(artwork, logger, CancellationToken.None).AsTask().ContinueWith((Task<bool> task) =>
            {
                if (task.Result)
                {
                    converter.DeleteUnneccessaryOriginal(artwork, logger);
                }
            });

            return (true, byteCount, noDetailDownload);
        }

        private async ValueTask<bool> DownloadFilePrepareDetailAsync(Artwork artwork)
        {
            bool success;
        RETRY:
            try
            {
                var detailArtwork = await networkClient.GetArtworkDetailAsync(artwork.Id, authentication, pipe, token).ConfigureAwait(false);
                var converted = Artwork.ConvertFromNetwrok(detailArtwork, database.TagSet, database.ToolSet, database.UserDictionary);
                artwork.Overwrite(converted);
                if (artwork.Type == ArtworkType.Ugoira && artwork.UgoiraFrames is null)
                {
                    artwork.UgoiraFrames = await networkClient.GetArtworkUgoiraMetadataAsync(artwork.Id, authentication, pipe, token).ConfigureAwait(false);
                }

                success = !converted.IsOfficiallyRemoved;
            }
            catch (HttpRequestException e) when (e.StatusCode.HasValue)
            {
                if (e.StatusCode.Value == HttpStatusCode.NotFound)
                {
                    artwork.IsOfficiallyRemoved = true;
                }
                else if (e.StatusCode.Value == HttpStatusCode.BadRequest)
                {
                    await networkClient.ReconnectAsync(e, pipe, token).ConfigureAwait(false);
                    goto RETRY;
                }
                else if (!pipe)
                {
                    logger.LogInformation(e, $"Other failure. {artwork.Id}");
                }

                success = false;
            }

            if (!pipe)
            {
                logger.LogInformation($"Detail success: {success} Id: {artwork.Id,20}");
            }

            return success;
        }
    }
}
