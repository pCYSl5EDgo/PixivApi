using PixivApi.Core;
using PixivApi.Core.Local;

namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("count", "")]
    public async ValueTask<int> CountAsync(
        [Option(0, $"input {ArgumentDescriptions.DatabaseDescription}")] string input,
        [Option(1, ArgumentDescriptions.FilterDescription)] string? filter = null,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var artworkItemFilter = string.IsNullOrWhiteSpace(filter) ? null : await IOUtility.JsonDeserializeAsync<ArtworkFilter>(filter, token).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input))
        {
            return -1;
        }

        var count = 0;
        var database = await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(input, token).ConfigureAwait(false);
        if (database is not { Artworks.Length: > 0 })
        {
            goto END;
        }

        if (artworkItemFilter is null)
        {
            count = database.Artworks.Length;
        }
        else
        {
            ParallelOptions parallelOptions = new()
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = configSettings.MaxParallel,
            };
            await artworkItemFilter.InitializeAsync(configSettings, database.UserDictionary, database.TagSet, parallelOptions);
            if (pipe)
            {
                await Parallel.ForEachAsync(database.Artworks, parallelOptions, (artwork, token) =>
                {
                    if (artworkItemFilter.Filter(artwork))
                    {
                        Interlocked.Increment(ref count);
                    }

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
                goto END;
            }

            if (artworkItemFilter.FileExistanceFilter is not { } fileFilter)
            {
                await Parallel.ForEachAsync(database.Artworks, parallelOptions, (artwork, token) =>
                {
                    if (artworkItemFilter.FilterWithoutFileExistance(artwork))
                    {
                        Interlocked.Increment(ref count);
                    }

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
                goto END;
            }

            ConcurrentBag<Artwork> bag = new();
            await Parallel.ForEachAsync(database.Artworks, parallelOptions, (artwork, token) =>
            {
                if (artworkItemFilter.FilterWithoutFileExistance(artwork))
                {
                    Interlocked.Increment(ref count);
                    bag.Add(artwork);
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
            var maxCount = count;
            System.Console.Write($"{ConsoleUtility.WarningColor}Current: {count}    0% processed(0 items of total {count} items) {ConsoleUtility.NormalizeColor}");
            var processed = 0;
            await Parallel.ForEachAsync(bag, parallelOptions, (artwork, token) =>
            {
                if (!fileFilter.Filter(artwork))
                {
                    Interlocked.Decrement(ref count);
                }

                var currentProcessed = Interlocked.Increment(ref processed);
                if ((currentProcessed & 1023) == 0)
                {
                    var percentage = (int)(processed * 100d / maxCount);
                    System.Console.Write($"{ConsoleUtility.DeleteLine1}{ConsoleUtility.WarningColor}Current: {count} {percentage,3}% processed({processed} items of total {maxCount} items){ConsoleUtility.NormalizeColor}");
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
            System.Console.Write(ConsoleUtility.DeleteLine1);
        }

    END:
        logger.LogInformation($"{count}");
        return 0;
    }
}
