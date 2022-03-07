namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("download")]
    public async ValueTask DownloadFileFromDatabaseAsync(
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool encode = true,
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.ArtworkFilterFilePath) || string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var token = Context.CancellationToken;
        var artworkFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(configSettings.ArtworkFilterFilePath, token).ConfigureAwait(false);
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

        var (database, artworks) = await PrepareDownloadFileAsync(configSettings.DatabaseFilePath, artworkFilter, configSettings.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return;
        }

        var downloadItemCount = 0;
        var alreadyCount = 0;
        var machine = new DownloadAsyncMachine(this, database, holder, pipe, token);
        var logger = Context.Logger;
        logger.LogInformation("Start downloading.");
        var detailUpdate = false;
        try
        {
            await foreach (var artwork in artworks)
            {
                token.ThrowIfCancellationRequested();
                if ((machine.DownloadByteCount >> 30) >= gigaByteCount)
                {
                    return;
                }

                var downloadResult = artwork.Type == ArtworkType.Ugoira ?
                    await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, encode, token).ConfigureAwait(false) :
                    await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, encode, token).ConfigureAwait(false);
                if ((downloadResult & DownloadResult.Success) != 0)
                {
                    Interlocked.Increment(ref downloadItemCount);
                }
                else
                {
                    Interlocked.Increment(ref alreadyCount);
                }

                if ((downloadResult & DownloadResult.Update) != 0)
                {
                    detailUpdate = true;
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Accept cancel.");
        }
        finally
        {
            if (!pipe)
            {
                logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ByteAmountUtility.ToDisplayable(machine.DownloadByteCount)}");
            }

            if (detailUpdate)
            {
                if (!pipe)
                {
                    logger.LogInformation($"Save to the database file.");
                }

                await IOUtility.MessagePackSerializeAsync(configSettings.DatabaseFilePath, database, FileMode.Create).ConfigureAwait(false);
            }

            if (alreadyCount != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    [Flags]
    private enum DownloadResult
    {
        None = 0,
        Success = 1,
        Update = 2,
    }

    private async ValueTask<DownloadResult> ProcessDownloadNotUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, bool encode, CancellationToken token)
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
                var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, encode ? converter.OriginalConverter : null, artwork.GetNotUgoiraOriginalUrl, pageIndex).ConfigureAwait(false);
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
                var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, encode ? converter.ThumbnailConverter : null, artwork.GetNotUgoiraThumbnailUrl, pageIndex).ConfigureAwait(false);
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

    private async ValueTask<DownloadResult> ProcessDownloadUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, bool shouldDownloadUgoira, bool encode, CancellationToken token)
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
            var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, encode ? converter.UgoiraZipConverter : null, artwork.GetUgoiraZipUrl).ConfigureAwait(false);
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
            var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, encode ? converter.OriginalConverter : null, artwork.GetUgoiraOriginalUrl).ConfigureAwait(false);
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
            var (Success, NoDetailDownload) = await machine.DownloadAsync(dest, artwork, noDetailDownload, encode ? converter.ThumbnailConverter : null, artwork.GetUgoiraThumbnailUrl).ConfigureAwait(false);
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

        var logger = Context.Logger;
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
