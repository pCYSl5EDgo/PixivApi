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

        var shouldDownloadOriginal = fileFilter.Original is not null;
        var shouldDownloadThumbnail = fileFilter.Thumbnail is not null;
        var shouldDownloadUgoira = fileFilter.Ugoira.HasValue;
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
        IFinderWithIndex finderWithIndexOriginal, finderWithIndexOriginalDefault, finderWithIndexThumbnail, finderWithIndexThumbnailDefault;
        switch (artwork.Type)
        {
            case ArtworkType.Illust:
                finderWithIndexOriginal = finder.IllustOriginalFinder;
                finderWithIndexOriginalDefault = finder.DefaultIllustOriginalFinder;
                finderWithIndexThumbnail = finder.IllustThumbnailFinder;
                finderWithIndexThumbnailDefault = finder.DefaultIllustThumbnailFinder;
                break;
            case ArtworkType.Manga:
                finderWithIndexOriginal = finder.MangaOriginalFinder;
                finderWithIndexOriginalDefault = finder.DefaultMangaOriginalFinder;
                finderWithIndexThumbnail = finder.MangaThumbnailFinder;
                finderWithIndexThumbnailDefault = finder.DefaultMangaThumbnailFinder;
                break;
            default:
                return false;
        }

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
                if (finderWithIndexOriginal.Exists(artwork, pageIndex))
                {
                    continue;
                }

                var (Success, NoDetailDownload) = await machine.DownloadAsync(finderWithIndexOriginalDefault.Find(artwork, pageIndex), artwork, converter?.OriginalConverter, noDetailDownload, artwork.GetNotUgoiraOriginalUrl, pageIndex).ConfigureAwait(false);
                if (!Success)
                {
                    return false;
                }

                noDetailDownload = NoDetailDownload;
                downloadAny = true;
            }

            if (shouldDownloadThumbnail)
            {
                if (finderWithIndexThumbnail.Exists(artwork, pageIndex))
                {
                    continue;
                }

                var (Success, NoDetailDownload) = await machine.DownloadAsync(finderWithIndexThumbnailDefault.Find(artwork, pageIndex), artwork, converter?.ThumbnailConverter, noDetailDownload, artwork.GetNotUgoiraThumbnailUrl, pageIndex).ConfigureAwait(false);
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
        if (shouldDownloadUgoira && !finder.UgoiraZipFinder.Exists(artwork))
        {
            var (Success, NoDetailDownload) = await machine.DownloadAsync(finder.DefaultUgoiraZipFinder.Find(artwork), artwork, converter?.UgoiraZipConverter, noDetailDownload, calcUrl: artwork.GetUgoiraZipUrl).ConfigureAwait(false);
            if (!Success)
            {
                return false;
            }

            noDetailDownload = NoDetailDownload;
        }

        if (shouldDownloadOriginal && !finder.UgoiraOriginalFinder.Exists(artwork))
        {
            var (Success, NoDetailDownload) = await machine.DownloadAsync(finder.DefaultUgoiraOriginalFinder.Find(artwork), artwork, converter?.OriginalConverter, noDetailDownload, calcUrl: artwork.GetUgoiraOriginalUrl).ConfigureAwait(false);
            if (!Success)
            {
                return false;
            }

            noDetailDownload = NoDetailDownload;
        }

        if (shouldDownloadThumbnail && !finder.UgoiraThumbnailFinder.Exists(artwork))
        {
            var (Success, NoDetailDownload) = await machine.DownloadAsync(finder.DefaultUgoiraThumbnailFinder.Find(artwork), artwork, converter?.ThumbnailConverter, noDetailDownload, calcUrl: artwork.GetUgoiraThumbnailUrl).ConfigureAwait(false);
            if (!Success)
            {
                return false;
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
