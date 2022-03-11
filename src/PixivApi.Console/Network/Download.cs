namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("download")]
    public async ValueTask DownloadFileFromDatabaseAsync(
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool encode = true
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath) || string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var token = Context.CancellationToken;
        var finder = Context.ServiceProvider.GetRequiredService<FinderFacade>();
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var artworkFilter = await filterFactory.CreateAsync(database, new(configSettings.ArtworkFilterFilePath), token).ConfigureAwait(false);
            if (artworkFilter is not { FileExistanceFilter: { } fileFilter })
            {
                return;
            }

            var artworks = PrepareDownloadFileAsync(database, artworkFilter, configSettings.OriginalFolder, gigaByteCount);
            if (artworks is null)
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

            var converter = encode ? Context.ServiceProvider.GetRequiredService<ConverterFacade>() : null;
            var downloadItemCount = 0;
            var alreadyCount = 0;
            var machine = new DownloadAsyncMachine(this, database, token);
            var logger = Context.Logger;
            logger.LogInformation("Start downloading.");
            try
            {
                await foreach (var artwork in artworks)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if ((machine.DownloadByteCount >> 30) >= gigaByteCount)
                    {
                        return;
                    }

                    var downloadResult = artwork.Type == ArtworkType.Ugoira ?
                        await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, finder, converter, token).ConfigureAwait(false) :
                        await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, finder, converter, token).ConfigureAwait(false);
                    if ((downloadResult & DownloadResult.Success) != 0)
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
                if (!System.Console.IsOutputRedirected)
                {
                    logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ByteAmountUtility.ToDisplayable(machine.DownloadByteCount)}");
                }

                if (alreadyCount != 0)
                {
                    await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            databaseFactory.Return(ref database);
        }
    }

    [Flags]
    private enum DownloadResult
    {
        None = 0,
        Success = 1,
        Update = 2,
    }

    private static async ValueTask<DownloadResult> ProcessDownloadNotUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, FinderFacade finder, ConverterFacade? converter, CancellationToken token)
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
                return DownloadResult.None;
        }

        var downloadAny = false;
        var noDetailDownload = true;
        foreach (var pageIndex in artwork)
        {
            if (token.IsCancellationRequested)
            {
                goto END;
            }

            if (shouldDownloadOriginal)
            {
                if (token.IsCancellationRequested)
                {
                    goto END;
                }

                if (finderWithIndexOriginal.Exists(artwork, pageIndex))
                {
                    continue;
                }

                var dest = finderWithIndexOriginalDefault.Find(artwork, pageIndex);
                var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.OriginalConverter, artwork.GetNotUgoiraOriginalUrl, pageIndex).ConfigureAwait(false);
                noDetailDownload = NoDetailDownload;
                downloadAny = true;
                if (!Success)
                {
                    goto END;
                }
            }

            if (shouldDownloadThumbnail)
            {
                if (token.IsCancellationRequested)
                {
                    goto END;
                }

                if (finderWithIndexThumbnail.Exists(artwork, pageIndex))
                {
                    continue;
                }

                var dest = finderWithIndexThumbnailDefault.Find(artwork, pageIndex);
                var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.ThumbnailConverter, artwork.GetNotUgoiraThumbnailUrl, pageIndex).ConfigureAwait(false);
                noDetailDownload = NoDetailDownload;
                downloadAny = true;
                if (!Success)
                {
                    goto END;
                }
            }
        }

    END:
        return CalculateDownloadResult(downloadAny, noDetailDownload);
    }

    private static async ValueTask<DownloadResult> ProcessDownloadUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, bool shouldDownloadUgoira, FinderFacade finder, ConverterFacade? converter, CancellationToken token)
    {
        var downloadAny = false;
        var noDetailDownload = true;
        if (shouldDownloadUgoira && !finder.UgoiraZipFinder.Exists(artwork))
        {
            if (token.IsCancellationRequested)
            {
                goto END;
            }

            var dest = finder.DefaultUgoiraZipFinder.Find(artwork);
            var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.UgoiraZipConverter, artwork.GetUgoiraZipUrl).ConfigureAwait(false);
            noDetailDownload = NoDetailDownload;
            downloadAny = true;
            if (!Success)
            {
                goto END;
            }
        }

        if (shouldDownloadOriginal && !finder.UgoiraOriginalFinder.Exists(artwork))
        {
            if (token.IsCancellationRequested)
            {
                goto END;
            }

            var dest = finder.DefaultUgoiraOriginalFinder.Find(artwork);
            var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.OriginalConverter, artwork.GetUgoiraOriginalUrl).ConfigureAwait(false);
            noDetailDownload = NoDetailDownload;
            downloadAny = true;
            if (!Success)
            {
                goto END;
            }
        }

        if (shouldDownloadThumbnail && !finder.UgoiraThumbnailFinder.Exists(artwork))
        {
            if (token.IsCancellationRequested)
            {
                goto END;
            }

            var dest = finder.DefaultUgoiraThumbnailFinder.Find(artwork);
            var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, converter?.ThumbnailConverter, artwork.GetUgoiraThumbnailUrl).ConfigureAwait(false);
            noDetailDownload = NoDetailDownload;
            downloadAny = true;
            if (!Success)
            {
                goto END;
            }
        }

    END:
        return CalculateDownloadResult(downloadAny, noDetailDownload);
    }

    private static DownloadResult CalculateDownloadResult(bool downloadAny, bool noDetailDownload) => (DownloadResult)((downloadAny ? 1 : 0) | (noDetailDownload ? 2 : 0));

    private IAsyncEnumerable<Artwork>? PrepareDownloadFileAsync(
        IDatabase database,
        ArtworkFilter? filter,
        string destinationDirectory,
        ulong gigaByteCount
    )
    {
        if (filter is null || gigaByteCount == 0)
        {
            return null;
        }

        var logger = Context.Logger;
        if (!Directory.Exists(destinationDirectory))
        {
            logger.LogError($"{VirtualCodes.BrightRedColor}directory does not exist. Path: {destinationDirectory}{VirtualCodes.NormalizeColor}");
            return null;
        }

        var token = Context.CancellationToken;
        filter.PageCount ??= new();
        filter.PageCount.Min ??= 1;
        return database.FilterAsync(filter, token);
    }

    private static readonly Uri referer = new("https://app-api.pixiv.net/");
}
