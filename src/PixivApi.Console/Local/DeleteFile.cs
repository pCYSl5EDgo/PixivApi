namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("delete-file", "")]
    public async ValueTask DeleteFileAsync(
        [Option(0, ArgumentDescriptions.FilterDescription)] string? filter = null
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

        var sizeInBytes = 0UL;
        try
        {
            var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await filterFactory.CreateAsync(database, new FileInfo(filter), token).ConfigureAwait(false);
            if (errorNotRedirected)
            {
                System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load filter.");
            }

            if (artworkFilter is null or { Count: <= 0 })
            {
                logger.LogInformation("0");
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            var finder = Context.ServiceProvider.GetRequiredService<FinderFacade>();
            if (finder is null)
            {
                return;
            }

            var count = 0UL;
            static ulong Delete(FileInfo file)
            {
                if (!file.Exists)
                {
                    return 0;
                }

                var answer = (ulong)file.Length;
                file.Delete();
                return answer;
            }

            await foreach (var artwork in database.FilterAsync(artworkFilter, token).ConfigureAwait(false))
            {
                if (errorNotRedirected)
                {
                    System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Delete Count: {++count}");
                }

                switch (artwork.Type)
                {
                    case ArtworkType.Illust:
                        for (var index = 0U; index < artwork.PageCount; index++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }

                            sizeInBytes += Delete(finder.IllustOriginalFinder.Find(artwork.Id, artwork.Extension, index));
                            sizeInBytes += Delete(finder.IllustThumbnailFinder.Find(artwork.Id, artwork.Extension, index));
                        }
                        break;
                    case ArtworkType.Manga:
                        for (var index = 0U; index < artwork.PageCount; index++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }

                            sizeInBytes += Delete(finder.IllustOriginalFinder.Find(artwork.Id, artwork.Extension, index));
                            sizeInBytes += Delete(finder.IllustThumbnailFinder.Find(artwork.Id, artwork.Extension, index));
                        }
                        break;
                    case ArtworkType.Ugoira:
                        sizeInBytes += Delete(finder.UgoiraOriginalFinder.Find(artwork.Id, artwork.Extension));
                        sizeInBytes += Delete(finder.UgoiraThumbnailFinder.Find(artwork.Id, artwork.Extension));
                        sizeInBytes += Delete(finder.UgoiraZipFinder.Find(artwork.Id, artwork.Extension));
                        break;
                    default:
                        continue;
                }
            }
        }
        finally
        {
            System.Console.WriteLine($"\nDelete Byte Amount: {ByteAmountUtility.ToDisplayable(sizeInBytes)}");
            databaseFactory.Return(ref database);
        }
    }
}
