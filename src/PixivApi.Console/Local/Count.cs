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
        var artworkItemFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(configSettings.DatabaseFilePath, token).ConfigureAwait(false);
        if (database is not { ArtworkDictionary.IsEmpty: false })
        {
            logger.LogInformation("0");
            return;
        }

        if (artworkItemFilter is null)
        {
            logger.LogInformation($"{database.ArtworkDictionary.Count}");
            return;
        }

        await artworkItemFilter.InitializeAsync(finder, database.UserDictionary, database.TagSet, token);
        var artworks = FilterExtensions.FilterBy(database.ArtworkDictionary, artworkItemFilter.IdFilter);
        if (System.Console.IsOutputRedirected)
        {
            logger.LogInformation($"{await CountPipeAsync(artworkItemFilter, artworks, token).ConfigureAwait(false)}");
        }
        else if (artworkItemFilter.FileExistanceFilter is null)
        {
            logger.LogInformation($"{await CountWithoutFileFilterAsync(artworkItemFilter, artworks, token).ConfigureAwait(false)}");
        }
        else
        {
            logger.LogInformation($"{await CountWithFileFilterAsync(maskPowerOf2, artworkItemFilter, artworks, artworkItemFilter.FileExistanceFilter, token).ConfigureAwait(false)}");
        }
    }

    private static async Task<ulong> CountWithFileFilterAsync(byte maskPowerOf2, ArtworkFilter artworkItemFilter, IEnumerable<Artwork> artworks, FileExistanceFilter fileFilter, CancellationToken token)
    {
        ConcurrentBag<Artwork> bag = new();
        var count = 0UL;
        await Parallel.ForEachAsync(artworks, token, (artwork, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (artworkItemFilter.FilterWithoutFileExistance(artwork))
            {
                Interlocked.Increment(ref count);
                bag.Add(artwork);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        var maxCount = count;
        System.Console.Write($"{VirtualCodes.BrightYellowColor}Current: {count}    0% processed(0 items of total {count} items) {VirtualCodes.NormalizeColor}");
        var processed = 0UL;
        var mask = (1UL << maskPowerOf2) - 1UL;
        await Parallel.ForEachAsync(bag, token, (artwork, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (!fileFilter.Filter(artwork))
            {
                Interlocked.Decrement(ref count);
            }

            var currentProcessed = Interlocked.Increment(ref processed);
            if ((currentProcessed & mask) == 0UL)
            {
                var percentage = (int)(processed * 100d / maxCount);
                System.Console.Write($"{VirtualCodes.DeleteLine1}{VirtualCodes.BrightYellowColor}Current: {count} {percentage,3}% processed({processed} items of total {maxCount} items){VirtualCodes.NormalizeColor}");
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        System.Console.Write(VirtualCodes.DeleteLine1);
        return count;
    }

    private static async Task<ulong> CountWithoutFileFilterAsync(ArtworkFilter artworkItemFilter, IEnumerable<Artwork> artworks, CancellationToken token)
    {
        var count = 0UL;
        await Parallel.ForEachAsync(artworks, token, (artwork, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(token);
            }

            if (artworkItemFilter.FilterWithoutFileExistance(artwork))
            {
                Interlocked.Increment(ref count);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        return count;
    }

    private static async Task<ulong> CountPipeAsync(ArtworkFilter artworkItemFilter, IEnumerable<Artwork> artworks, CancellationToken token)
    {
        var count = 0UL;
        await Parallel.ForEachAsync(artworks, token, (artwork, token) =>
        {
            if (artworkItemFilter.Filter(artwork))
            {
                Interlocked.Increment(ref count);
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        return count;
    }
}
