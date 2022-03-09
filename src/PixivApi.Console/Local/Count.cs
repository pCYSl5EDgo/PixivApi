namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask CountAsync(
        [Option(0, ArgumentDescriptions.FilterDescription)] string? filter = null,
        [Option("mask")] byte maskPowerOf2 = 10
    )
    {
        var token = Context.CancellationToken;
        filter ??= configSettings.ArtworkFilterFilePath;
        var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await filterFactory.CreateAsync(new FileInfo(filter), token).ConfigureAwait(false);
        var database = await databaseFactory.CreateAsync(token).ConfigureAwait(false);
        var allCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
        if (artworkFilter is null)
        {
            logger.LogInformation($"{allCount}");
            return;
        }

        if (allCount == 0 || (ulong)artworkFilter.Offset >= allCount || artworkFilter.Count == 0)
        {
            logger.LogInformation("0");
            return;
        }

        artworkFilter.Order = ArtworkOrderKind.None;
        var errorNotRedirected = !System.Console.IsErrorRedirected;
        if (errorNotRedirected)
        {
            System.Console.Error.Write("Start collecting.");
        }

        long count = -artworkFilter.Offset;
        var mask = (1L << maskPowerOf2) - 1;
        if (artworkFilter.Count is { } maxCount)
        {
            await foreach (var artwork in database.FastArtworkFilterAsync(artworkFilter, token))
            {
                var c = ++count;
                if (c >= maxCount)
                {
                    break;
                }

                if (errorNotRedirected && (c & mask) == 0)
                {
                    System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Collecting Count: {c}");
                }
            }
        }
        else
        {
            await foreach (var artwork in database.FastArtworkFilterAsync(artworkFilter, token))
            {
                var c = ++count;
                if (errorNotRedirected && (c & mask) == 0)
                {
                    System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Collecting Count: {c}");
                }
            }
        }

        if (count < 0)
        {
            count = 0;
        }

        if (errorNotRedirected)
        {
            System.Console.Error.Write(VirtualCodes.DeleteLine1);
        }

        logger.LogInformation($"{count}");
    }
}
