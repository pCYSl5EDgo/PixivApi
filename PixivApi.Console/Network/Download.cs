using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("download")]
    public async ValueTask DownloadFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool encode = true,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var artworkFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (artworkFilter is not { FileExistanceFilter: { } fileFilter })
        {
            return;
        }

        var shouldDownloadOriginal = fileFilter.Original != FileExistanceType.None;
        var shouldDownloadThumbnail = fileFilter.Thumbnail != FileExistanceType.None;
        var shouldDownloadUgoira = fileFilter.Ugoira is not null;
        if (!shouldDownloadOriginal && !shouldDownloadThumbnail && !shouldDownloadUgoira)
        {
            return;
        }

        var (database, artworks) = await PrepareDownloadFileAsync(path, artworkFilter, configSettings.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return;
        }

        var downloadItemCount = 0;
        var alreadyCount = 0;
        var machine = new DownloadAsyncMachine(this, database, holder, pipe, token);
        try
        {
            await foreach (var artwork in artworks)
            {
                if (token.IsCancellationRequested || (machine.DownloadByteCount >> 30) >= gigaByteCount)
                {
                    return;
                }

                var success = artwork.Type == ArtworkType.Ugoira ?
                    await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, encode ? converter : null).ConfigureAwait(false) :
                    await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, encode ? converter : null).ConfigureAwait(false);
                if (success)
                {
                    Interlocked.Increment(ref downloadItemCount);
                }
                else
                {
                    Interlocked.Increment(ref alreadyCount);
                }
            }
        }
        finally
        {
            if (!pipe)
            {
                logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ByteAmountUtility.ToDisplayable(machine.DownloadByteCount)}");
            }

            await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            if (alreadyCount != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask<bool> ProcessDownloadNotUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, ConverterFacade? converter)
    {
        var downloadAny = false;
        var noDetailDownload = true;
        for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
        {
            if ((artwork.ExtraHideLast && pageIndex == artwork.PageCount - 1) || (artwork.ExtraPageHideReasonDictionary is { Count: > 0 } dictionary && dictionary.TryGetValue(pageIndex, out var reason) && reason != HideReason.NotHidden))
            {
                continue;
            }

            if (shouldDownloadOriginal)
            {
                var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetNotUgoiraOriginalFileName(pageIndex));
                if (pageFile.Exists)
                {
                    continue;
                }

                var (Success, NoDetailDownload) = await machine.DownloadAsync(pageFile, artwork, converter?.OriginalConverter, noDetailDownload, artwork.GetNotUgoiraOriginalUrl, pageIndex).ConfigureAwait(false);
                if (!Success)
                {
                    return false;
                }

                noDetailDownload = NoDetailDownload;
                downloadAny = true;
            }

            if (shouldDownloadThumbnail)
            {
                var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetNotUgoiraThumbnailFileName(pageIndex));
                if (pageFile.Exists)
                {
                    continue;
                }

                var (Success, NoDetailDownload) = await machine.DownloadAsync(pageFile, artwork, converter?.ThumbnailConverter, noDetailDownload, artwork.GetNotUgoiraThumbnailUrl, pageIndex).ConfigureAwait(false);
                if (!Success)
                {
                    return false;
                }

                noDetailDownload = NoDetailDownload;
                downloadAny = true;
            }
        }

        return downloadAny;
    }

    private async ValueTask<bool> ProcessDownloadUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, bool shouldDownloadUgoira, ConverterFacade? converter)
    {
        var noDetailDownload = true;
        if (shouldDownloadUgoira)
        {
            var ugoiraZipFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.UgoiraFolder, artwork.Id, artwork.GetUgoiraZipFileName());
            if (!ugoiraZipFile.Exists)
            {
                var (Success, NoDetailDownload) = await machine.DownloadAsync(ugoiraZipFile, artwork, converter?.UgoiraZipConverter, noDetailDownload, calcUrl: artwork.GetUgoiraZipUrl).ConfigureAwait(false);
                if (!Success)
                {
                    return false;
                }

                noDetailDownload = NoDetailDownload;
            }
        }

        if (shouldDownloadOriginal)
        {
            var originalFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetUgoiraOriginalFileName());
            if (!originalFile.Exists)
            {
                var (Success, NoDetailDownload) = await machine.DownloadAsync(originalFile, artwork, converter?.OriginalConverter, noDetailDownload, calcUrl: artwork.GetUgoiraOriginalUrl).ConfigureAwait(false);
                if (!Success)
                {
                    return false;
                }

                noDetailDownload = NoDetailDownload;
            }
        }

        if (shouldDownloadThumbnail)
        {
            var thumbnailFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetUgoiraThumbnailFileName());
            if (!thumbnailFile.Exists)
            {
                var (Success, NoDetailDownload) = await machine.DownloadAsync(thumbnailFile, artwork, converter?.ThumbnailConverter, noDetailDownload, calcUrl: artwork.GetUgoiraThumbnailUrl).ConfigureAwait(false);
                if (!Success)
                {
                    return false;
                }
            }
        }

        return true;
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
            logger.LogError($"{VirtualCodes.BrightRedColor}file does not exist. Path: {path}{VirtualCodes.NormalizeColor}");
            return default;
        }

        if (!Directory.Exists(destinationDirectory))
        {
            logger.LogError($"{VirtualCodes.BrightRedColor}directory does not exist. Path: {destinationDirectory}{VirtualCodes.NormalizeColor}");
            return default;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is not { ArtworkDictionary.IsEmpty: false })
        {
            logger.LogError($"{VirtualCodes.BrightRedColor}database is empty. Path: {path}{VirtualCodes.NormalizeColor}");
            return default;
        }

        _ = await ConnectAsync(token).ConfigureAwait(false);

        filter.PageCount ??= new();
        filter.PageCount.Min ??= 1;

        var artworkCollection = FilterExtensions.CreateAsyncEnumerable(finder, database, filter, token);
        return (database, artworkCollection);
    }

    private static readonly Uri referer = new("https://app-api.pixiv.net/");
}
