﻿namespace PixivApi.Console;

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
        var artworkFilter = string.IsNullOrWhiteSpace(filter) ? null : await filterFactory.CreateAsync(new FileInfo(filter), token).ConfigureAwait(false);
        if (errorNotRedirected)
        {
            System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load filter.");
        }
        
        if (artworkFilter?.Count == 0)
        {
            logger.LogInformation("0");
            return;
        }

        var database = await databaseFactory.CreateAsync(token).ConfigureAwait(false);
        if (errorNotRedirected)
        {
            System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Load database.");
        }

        var allCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
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
        await foreach (var artwork in database.FastArtworkFilterAsync(artworkFilter, token))
        {
            var c = ++count;
            if (errorNotRedirected && (c & mask) == 0)
            {
                System.Console.Error.Write($"{VirtualCodes.DeleteLine1}Collecting Count: {c}");
            }
        }

        if (errorNotRedirected)
        {
            System.Console.Error.Write(VirtualCodes.DeleteLine1);
        }

        logger.LogInformation($"{count}");
    }
}
