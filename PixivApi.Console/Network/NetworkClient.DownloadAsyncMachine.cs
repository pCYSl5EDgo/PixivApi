using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class NetworkClient
{
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
        private bool noDetailDownload = true;

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

        public async ValueTask<bool> DownloadAsync<T>(FileInfo file, Artwork artwork, T argument, Func<T, string> calcUrl)
        {
            ulong byteCount = 0;
        RETRY:
            var url = calcUrl(argument);
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
            catch (HttpRequestException e) when (noDetailDownload && e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (await DownloadFilePrepareDetailAsync(artwork).ConfigureAwait(false))
                {
                    goto RETRY;
                }
                else
                {
                    return false;
                }
            }
            catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await networkClient.ReconnectAsync(e, pipe, token).ConfigureAwait(false);
                goto RETRY;
            }
            catch (Exception e)
            {
                if (!pipe)
                {
                    logger.LogError(e, $"{VirtualCodes.BrightRedColor}Download failed. Url: {url}{VirtualCodes.NormalizeColor}");
                }

                return false;
            }

            DownloadByteCount += byteCount;
            ++DownloadFileCount;
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightBlueColor}Download success. Index: {DownloadFileCount,6} Transfer: {byteCount,20} Url: {url}{VirtualCodes.NormalizeColor}");
            }

            return true;
        }

        public async ValueTask<bool> DownloadAsync(FileInfo file, Artwork artwork, Func<string> calcUrl)
        {
            ulong byteCount = 0;
        RETRY:
            var url = calcUrl();
            try
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var headers = request.Headers;
                headers.Referrer = referer;
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var stream = new FileStream(file.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 8192, true);
                // Write to the file should not be cancelled.
                await response.Content.CopyToAsync(stream, CancellationToken.None).ConfigureAwait(false);
                byteCount = (ulong)stream.Length;
            }
            catch (HttpRequestException e) when (noDetailDownload && e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (!pipe)
                {
                    logger.LogError($"{VirtualCodes.BrightRedColor}Not Found: {url}{VirtualCodes.NormalizeColor}");
                }

                if (await DownloadFilePrepareDetailAsync(artwork).ConfigureAwait(false))
                {
                    goto RETRY;
                }
                else
                {
                    return false;
                }
            }
            catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await networkClient.ReconnectAsync(e, pipe, token).ConfigureAwait(false);
                goto RETRY;
            }
            catch (Exception e)
            {
                if (!pipe)
                {
                    logger.LogError(e, $"{VirtualCodes.BrightRedColor}Download failed. Url: {url}{VirtualCodes.NormalizeColor}");
                }

                return false;
            }

            DownloadByteCount += byteCount;
            ++DownloadFileCount;
            if (!pipe)
            {
                logger.LogInformation($"{VirtualCodes.BrightBlueColor}Download success. Index: {DownloadFileCount,6} Transfer: {byteCount,20} Url: {url}{VirtualCodes.NormalizeColor}");
            }

            return true;
        }

        private async ValueTask<bool> DownloadFilePrepareDetailAsync(Artwork artwork)
        {
            if (artwork is null)
            {
                return false;
            }

            noDetailDownload = false;
            bool success;
        RETRY:
            try
            {
                var detailArtwork = await networkClient.GetArtworkDetailAsync(artwork.Id, pipe, token).ConfigureAwait(false);
                var converted = Artwork.ConvertFromNetwrok(detailArtwork, database.TagSet, database.ToolSet, database.UserDictionary);
                artwork.Overwrite(converted);
                if (artwork.Type == ArtworkType.Ugoira && artwork.UgoiraFrames is null)
                {
                    artwork.UgoiraFrames = await networkClient.GetArtworkUgoiraMetadataAsync(artwork.Id, pipe, token).ConfigureAwait(false);
                }

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

        public void Initialize() => noDetailDownload = true;
    }
}
