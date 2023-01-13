namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask CountAsync(
        [Option(0, ArgumentDescriptions.FilterDescription)] string? filter = null,
        [Option("mask")] byte maskPowerOf2 = 10
    )
    {
        var errorNotRedirected = !System.Console.IsErrorRedirected;
        var token = Context.CancellationToken;
        filter ??= configSettings.ArtworkFilterFilePath;
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        if (errorNotRedirected)
        {
            System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load database.");
        }

        try
        {
            var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await filterFactory.CreateAsync(database, new FileInfo(filter), token).ConfigureAwait(false);
            if (errorNotRedirected)
            {
                System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load filter.");
            }

            if (artworkFilter?.Count == 0)
            {
                logger.LogInformation("0");
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            var allCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
            logger.LogTrace($"All Count: {allCount}");
            if (artworkFilter is null)
            {
                logger.LogInformation($"{allCount}");
                return;
            }

            if (allCount == 0 || (ulong)artworkFilter.Offset >= allCount)
            {
                logger.LogInformation("0");
                return;
            }

            artworkFilter.Order = ArtworkOrderKind.None;
            if (errorNotRedirected)
            {
                System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Start collecting.");
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            var count = 0UL;
            var mask = (1UL << maskPowerOf2) - 1UL;
            if (artworkFilter.ShouldHandleFileExistanceFilter)
            {
                await foreach (var artwork in database.FilterAsync(artworkFilter, token))
                {
                    token.ThrowIfCancellationRequested();
                    var c = ++count;
                    if (errorNotRedirected && (c & mask) == 0)
                    {
                        System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Collecting Count: {c}");
                    }
                }
            }
            else
            {
                count = await database.CountArtworkAsync(artworkFilter, token).ConfigureAwait(false);
            }

            if (errorNotRedirected)
            {
                System.Console.Error.Write(VirtualCodes.DeleteLine1);
            }

            logger.LogInformation($"{count}");
        }
        finally
        {
            databaseFactory.Return(ref database);
        }
    }

    [Command("count-size", "")]
    public async ValueTask CountSizeAsync(
        [Option(0, ArgumentDescriptions.FilterDescription)] string? filter = null,
        [Option("mask")] byte maskPowerOf2 = 10
    )
    {
        var errorNotRedirected = !System.Console.IsErrorRedirected;
        var token = Context.CancellationToken;
        filter ??= configSettings.ArtworkFilterFilePath;
        if (errorNotRedirected)
        {
            System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load database.");
        }

        var count = 0UL;
        var fileCount = 0UL;

        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        try
        {
            var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await filterFactory.CreateAsync(database, new FileInfo(filter), token).ConfigureAwait(false);
            if (artworkFilter is null)
            {
                return;
            }

            artworkFilter.FileExistanceFilter = default;
            artworkFilter.Count = default;
            artworkFilter.Offset = default;
            artworkFilter.Order = ArtworkOrderKind.None;
            if (errorNotRedirected)
            {
                System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load filter.");
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (errorNotRedirected)
            {
                System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Start collecting.");
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            var facade = Context.ServiceProvider.GetRequiredService<FinderFacade>();
            await foreach (var artwork in database.FilterAsync(artworkFilter, token))
            {
                token.ThrowIfCancellationRequested();
                var add = false;
                switch (artwork.Type)
                {
                    case ArtworkType.Illust:
                        for (uint i = 0; i < artwork.PageCount; i++)
                        {
                            var file = facade.IllustOriginalFinder.Find(artwork.Id, artwork.Extension, i);
                            if (file.Exists)
                            {
                                add = true;
                                count += (ulong)file.Length;
                            }
                        }
                        for (uint i = 0; i < artwork.PageCount; i++)
                        {
                            var file = facade.IllustThumbnailFinder.Find(artwork.Id, artwork.Extension, i);
                            if (file.Exists)
                            {
                                add = true;
                                count += (ulong)file.Length;
                            }
                        }
                        break;
                    case ArtworkType.Manga:
                        for (uint i = 0; i < artwork.PageCount; i++)
                        {
                            var file = facade.MangaOriginalFinder.Find(artwork.Id, artwork.Extension, i);
                            if (file.Exists)
                            {
                                add = true;
                                count += (ulong)file.Length;
                            }
                        }
                        for (uint i = 0; i < artwork.PageCount; i++)
                        {
                            var file = facade.MangaThumbnailFinder.Find(artwork.Id, artwork.Extension, i);
                            if (file.Exists)
                            {
                                add = true;
                                count += (ulong)file.Length;
                            }
                        }
                        break;
                    case ArtworkType.Ugoira:
                        {
                            var zip = facade.UgoiraZipFinder.Find(artwork.Id, artwork.Extension);
                            if (zip.Exists)
                            {
                                add = true;
                                count += (ulong)zip.Length;
                            }

                            var original = facade.UgoiraOriginalFinder.Find(artwork.Id, artwork.Extension);
                            if (original.Exists)
                            {
                                add = true;
                                count += (ulong)original.Length;
                            }

                            var thumbnail = facade.UgoiraThumbnailFinder.Find(artwork.Id, artwork.Extension);
                            if (thumbnail.Exists)
                            {
                                add = true;
                                count += (ulong)thumbnail.Length;
                            }
                        }
                        break;
                    default:
                        continue;
                }

                if (add)
                {
                    fileCount++;
                }
            }

            if (errorNotRedirected)
            {
                System.Console.Error.Write(VirtualCodes.DeleteLine1);
            }
        }
        finally
        {
            logger.LogInformation($"File Size: {ByteAmountUtility.ToDisplayable(count)}\nFile Count: {fileCount}");
            databaseFactory.Return(ref database);
        }
    }
}
