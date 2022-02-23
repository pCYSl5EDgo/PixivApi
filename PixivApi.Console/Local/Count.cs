using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask<int> CountAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null,
        bool pipe = false,
        [Option("mask")] byte maskPowerOf2 = 10
    )
    {
        var token = Context.CancellationToken;
        var artworkItemFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input))
        {
            return -1;
        }

        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
        if (database is not { Artworks.Length: > 0 })
        {
            logger.LogInformation("0");
            return 0;
        }

        if (artworkItemFilter is null)
        {
            logger.LogInformation($"{database.Artworks.Length}");
            return 0;
        }

        await artworkItemFilter.InitializeAsync(configSettings, database.UserDictionary, database.TagSet, token);
        if (pipe)
        {
            logger.LogInformation($"{await CountPipeAsync(artworkItemFilter, database.Artworks, token).ConfigureAwait(false)}");
        }
        else if (artworkItemFilter.FileExistanceFilter is null)
        {
            logger.LogInformation($"{await CountWithoutFileFilterAsync(artworkItemFilter, database.Artworks, token).ConfigureAwait(false)}");
        }
        else
        {
            logger.LogInformation($"{await CountWithFileFilterAsync(maskPowerOf2, artworkItemFilter, database.Artworks, artworkItemFilter.FileExistanceFilter, token).ConfigureAwait(false)}");
        }

        return 0;
    }

    private static async Task<ulong> CountWithFileFilterAsync(byte maskPowerOf2, ArtworkFilter artworkItemFilter, Artwork[] artworks, FileExistanceFilter fileFilter, CancellationToken token)
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
        System.Console.Write($"{ConsoleUtility.WarningColor}Current: {count}    0% processed(0 items of total {count} items) {ConsoleUtility.NormalizeColor}");
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
                System.Console.Write($"{ConsoleUtility.DeleteLine1}{ConsoleUtility.WarningColor}Current: {count} {percentage,3}% processed({processed} items of total {maxCount} items){ConsoleUtility.NormalizeColor}");
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
        System.Console.Write(ConsoleUtility.DeleteLine1);
        return count;
    }

    private static async Task<ulong> CountWithoutFileFilterAsync(ArtworkFilter artworkItemFilter, Artwork[] artworks, CancellationToken token)
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

    private static async Task<ulong> CountPipeAsync(ArtworkFilter artworkItemFilter, Artwork[] artworks, CancellationToken token)
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
