using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("download")]
    public async ValueTask<int> DownloadFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var artworkFilter = await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (artworkFilter is not { FileExistanceFilter: { } fileFilter })
        {
            return -1;
        }

        var shouldDownloadOriginal = fileFilter.Original != FileExistanceType.None;
        var shouldDownloadThumbnail = fileFilter.Thumbnail != FileExistanceType.None;
        if (!shouldDownloadOriginal && !shouldDownloadThumbnail)
        {
            return -1;
        }

        var (database, artworks) = await PrepareDownloadFileAsync(path, artworkFilter, configSettings.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var downloadItemCount = 0;
        var alreadyCount = 0;
        var machine = new DownloadAsyncMachine(this, database, pipe, token);
        try
        {
            await foreach (var artwork in artworks)
            {
                if (token.IsCancellationRequested || (machine.DownloadByteCount >> 30) >= gigaByteCount)
                {
                    break;
                }

                machine.Initialize();
                if (artwork.Type == ArtworkType.Ugoira)
                {
                    if (shouldDownloadOriginal)
                    {
                        var ugoiraZipFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.UgoiraFolder, artwork.Id, artwork.GetZipFileName());
                        if (!ugoiraZipFile.Exists && !await machine.DownloadAsync(ugoiraZipFile, artwork, artwork.GetZipUrl).ConfigureAwait(false))
                        {
                            goto FAIL;
                        }
                    }

                    if (shouldDownloadThumbnail)
                    {
                        var thumbnailFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetThumbnailFileName(0));
                        if (!thumbnailFile.Exists && !await machine.DownloadAsync(thumbnailFile, artwork, artwork.GetUgoiraThumbnailUrl).ConfigureAwait(false))
                        {
                            goto FAIL;
                        }
                    }
                }

                for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
                {
                    if (shouldDownloadOriginal)
                    {
                        var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetOriginalFileName(pageIndex));
                        if (pageFile.Exists)
                        {
                            continue;
                        }

                        if (!await machine.DownloadAsync(pageFile, artwork, pageIndex, artwork.GetOriginalUrl).ConfigureAwait(false))
                        {
                            goto FAIL;
                        }
                    }

                    if (shouldDownloadThumbnail)
                    {
                        var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetThumbnailFileName(pageIndex));
                        if (pageFile.Exists)
                        {
                            continue;
                        }

                        if (!await machine.DownloadAsync(pageFile, artwork, pageIndex, artwork.GetThumbnailUrl).ConfigureAwait(false))
                        {
                            goto FAIL;
                        }
                    }
                }

                ++downloadItemCount;
                continue;

            FAIL:
                ++alreadyCount;
            }
        }
        finally
        {
            if (!pipe)
            {
                logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ToDisplayableByteAmount(machine.DownloadByteCount)}");
            }

            await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            if (alreadyCount != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    [Command("download-original")]
    public async ValueTask<int> DownloadOriginalFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), configSettings.OriginalFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var downloadItemCount = 0;
        var alreadyCount = 0;
        var machine = new DownloadAsyncMachine(this, database, pipe, token);
        try
        {
            await foreach (var artwork in artworks)
            {
                if (token.IsCancellationRequested || (machine.DownloadByteCount >> 30) >= gigaByteCount)
                {
                    break;
                }

                machine.Initialize();
                if (artwork.Type == ArtworkType.Ugoira)
                {
                    var ugoiraZipFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.UgoiraFolder, artwork.Id, artwork.GetZipFileName());
                    if (!ugoiraZipFile.Exists && !await machine.DownloadAsync(ugoiraZipFile, artwork, artwork.GetZipUrl).ConfigureAwait(false))
                    {
                        goto FAIL;
                    }
                }

                for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
                {
                    var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetOriginalFileName(pageIndex));
                    if (pageFile.Exists)
                    {
                        continue;
                    }

                    if (!await machine.DownloadAsync(pageFile, artwork, pageIndex, artwork.GetOriginalUrl).ConfigureAwait(false))
                    {
                        goto FAIL;
                    }
                }

                ++downloadItemCount;
                continue;

            FAIL:
                ++alreadyCount;
            }
        }
        finally
        {
            if (!pipe)
            {
                logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ToDisplayableByteAmount(machine.DownloadByteCount)}");
            }

            await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            if (alreadyCount != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    [Command("download-thumbnail")]
    public async ValueTask<int> DownloadThumbnailFileFromDatabaseAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string path,
        [Option(1, ArgumentDescriptions.FilterDescription)] string filter,
        [Option("g")] ulong gigaByteCount = 2UL,
        [Option("mask")] int maskPowerOf2 = 10,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var (database, artworks) = await PrepareDownloadFileAsync(path, await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false), configSettings.ThumbnailFolder, gigaByteCount).ConfigureAwait(false);
        if (artworks is null)
        {
            return -1;
        }

        var downloadItemCount = 0;
        var alreadyCount = 0;
        var machine = new DownloadAsyncMachine(this, database, pipe, token);
        try
        {
            await foreach (var artwork in artworks)
            {
                if (token.IsCancellationRequested || (machine.DownloadByteCount >> 30) >= gigaByteCount)
                {
                    break;
                }

                machine.Initialize();
                if (artwork.Type == ArtworkType.Ugoira)
                {
                    var thumbnailFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetThumbnailFileName(0));
                    if (!thumbnailFile.Exists && !await machine.DownloadAsync(thumbnailFile, artwork, artwork.GetUgoiraThumbnailUrl).ConfigureAwait(false))
                    {
                        goto FAIL;
                    }
                }
                else
                {
                    for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
                    {
                        var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetThumbnailFileName(pageIndex));
                        if (pageFile.Exists)
                        {
                            continue;
                        }

                        if (!await machine.DownloadAsync(pageFile, artwork, pageIndex, artwork.GetThumbnailUrl).ConfigureAwait(false))
                        {
                            goto FAIL;
                        }
                    }
                }

                ++downloadItemCount;
                continue;

            FAIL:
                ++alreadyCount;
            }
        }
        finally
        {
            if (!pipe)
            {
                logger.LogInformation($"Item: {downloadItemCount}, File: {machine.DownloadFileCount}, Already: {alreadyCount}, Transfer: {ToDisplayableByteAmount(machine.DownloadByteCount)}");
            }

            await IOUtility.MessagePackSerializeAsync(path, database, FileMode.Create).ConfigureAwait(false);
            if (alreadyCount != 0)
            {
                await LocalClient.ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
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
            logger.LogError($"{ConsoleUtility.ErrorColor}file does not exist. Path: {path}{ConsoleUtility.NormalizeColor}");
            return default;
        }

        if (!Directory.Exists(destinationDirectory))
        {
            logger.LogError($"{ConsoleUtility.ErrorColor}directory does not exist. Path: {destinationDirectory}{ConsoleUtility.NormalizeColor}");
            return default;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(path, token).ConfigureAwait(false);
        if (database is not { ArtworkDictionary.IsEmpty: false })
        {
            logger.LogError($"{ConsoleUtility.ErrorColor}database is empty. Path: {path}{ConsoleUtility.NormalizeColor}");
            return default;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return default;
        }

        filter.PageCount ??= new();
        filter.PageCount.Min ??= 1;

        var artworkCollection = FilterExtensions.CreateAsyncEnumerable(configSettings, database, filter, token);
        return (database, artworkCollection);
    }

    private static string ToDisplayableByteAmount(ulong byteCount)
    {
        if (byteCount < (1 << 10))
        {
            return $"{byteCount} B";
        }
        else if (byteCount < (1 << 20))
        {
            return $"{byteCount >> 10} KB + {byteCount & 1023} B";
        }
        else if (byteCount < (1 << 30))
        {
            return $"{byteCount >> 20} MB + {(byteCount >> 10) & 1023} KB + {byteCount & 1023} B";
        }
        else
        {
            return $"{byteCount >> 30} GB + {(byteCount >> 20) & 1023} MB + {(byteCount >> 10) & 1023} KB + {byteCount & 1023} B";
        }
    }

    private static readonly Uri referer = new("https://app-api.pixiv.net/");
}
