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
        bool encode = true,
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
        var shouldDownloadUgoira = fileFilter.Ugoira is not null;
        if (!shouldDownloadOriginal && !shouldDownloadThumbnail && !shouldDownloadUgoira)
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
                var success = artwork.Type == ArtworkType.Ugoira ?
                    await ProcessDownloadUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, shouldDownloadUgoira, encode ? converter : null).ConfigureAwait(false) :
                    await ProcessDownloadNotUgoiraAsync(machine, artwork, shouldDownloadOriginal, shouldDownloadThumbnail, encode ? converter : null).ConfigureAwait(false);
                if (success)
                {
                    ++downloadItemCount;
                }
                else
                {
                    ++alreadyCount;
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

        return 0;
    }

    private async ValueTask<bool> ProcessDownloadNotUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, ConverterFacade? converter)
    {
        for (uint pageIndex = 0; pageIndex < artwork.PageCount; pageIndex++)
        {
            if (shouldDownloadOriginal)
            {
                var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetNotUgoiraOriginalFileName(pageIndex));
                if (pageFile.Exists)
                {
                    continue;
                }

                if (!await machine.DownloadAsync(pageFile, artwork, converter?.OriginalConverter, artwork.GetNotUgoiraOriginalUrl, pageIndex).ConfigureAwait(false))
                {
                    return false;
                }
            }

            if (shouldDownloadThumbnail)
            {
                var pageFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetNotUgoiraThumbnailFileName(pageIndex));
                if (pageFile.Exists)
                {
                    continue;
                }

                if (!await machine.DownloadAsync(pageFile, artwork, converter?.ThumbnailConverter, artwork.GetNotUgoiraThumbnailUrl, pageIndex).ConfigureAwait(false))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private async ValueTask<bool> ProcessDownloadUgoiraAsync(DownloadAsyncMachine machine, Artwork artwork, bool shouldDownloadOriginal, bool shouldDownloadThumbnail, bool shouldDownloadUgoira, ConverterFacade? converter)
    {
        if (shouldDownloadUgoira)
        {
            var ugoiraZipFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.UgoiraFolder, artwork.Id, artwork.GetUgoiraZipFileName());
            if (!ugoiraZipFile.Exists && !await machine.DownloadAsync(ugoiraZipFile, artwork, converter?.UgoiraZipConverter, artwork.GetUgoiraZipUrl).ConfigureAwait(false))
            {
                return false;
            }
        }

        if (shouldDownloadOriginal)
        {
            var originalFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.OriginalFolder, artwork.Id, artwork.GetUgoiraOriginalFileName());
            if (!originalFile.Exists && !await machine.DownloadAsync(originalFile, artwork, converter?.OriginalConverter, artwork.GetUgoiraOriginalUrl).ConfigureAwait(false))
            {
                return false;
            }
        }

        if (shouldDownloadThumbnail)
        {
            var thumbnailFile = DownloadAsyncMachine.PrepareFileInfo(configSettings.ThumbnailFolder, artwork.Id, artwork.GetUgoiraThumbnailFileName());
            if (!thumbnailFile.Exists && !await machine.DownloadAsync(thumbnailFile, artwork, converter?.ThumbnailConverter, artwork.GetUgoiraThumbnailUrl).ConfigureAwait(false))
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

        if (!await Connect().ConfigureAwait(false))
        {
            return default;
        }

        filter.PageCount ??= new();
        filter.PageCount.Min ??= 1;

        var artworkCollection = FilterExtensions.CreateAsyncEnumerable(finder, database, filter, token);
        return (database, artworkCollection);
    }

    private static readonly Uri referer = new("https://app-api.pixiv.net/");
}
