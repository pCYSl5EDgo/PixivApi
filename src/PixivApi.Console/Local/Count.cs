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

            if (errorNotRedirected)
            {
                System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load database.");
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

            var count = 0UL;
            var mask = (1UL << maskPowerOf2) - 1UL;

            if (artworkFilter.ShouldHandleFileExistanceFilter)
            {
                await foreach (var artwork in database.FilterAsync(artworkFilter, token))
                {
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
}
