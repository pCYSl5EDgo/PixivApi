namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("clear-0-bytes", "")]
    public ValueTask ClearZeroBytes(
        [Option("mask")] int maskPowerOf2 = 10
    ) => ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken);

    internal static async ValueTask ClearAsync(ILogger logger, ConfigSettings configSettings, int maskPowerOf2, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("Start clearing.");
        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };

        var mask = (1UL << maskPowerOf2) - 1UL;
        async ValueTask<(ulong, ulong)> DeleteAsync(string root)
        {
            var files = new List<string>(1 << 14);
            System.Console.Write($"Start collecting files under {root}");
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                files.Add(file);
                if (((ulong)files.Count & mask) == 0)
                {
                    System.Console.Write($"{ConsoleUtility.DeleteLine1}{files.Count} files collected");
                }
            }

            System.Console.Write($"{ConsoleUtility.DeleteLine1}Remove: {0,6} {0,3}%({0,8} items of total {files.Count,8}) processed");
            ulong count = 0UL, removed = 0UL;
            await Parallel.ForEachAsync(files, parallelOptions, (file, token) =>
            {
                if (token.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled(token);
                }

                var myCount = Interlocked.Increment(ref count);
                var info = new FileInfo(file);
                if (info.Length == 0)
                {
                    Interlocked.Increment(ref removed);
                    info.Delete();
                }

                if ((myCount & mask) == 0UL)
                {
                    var percentage = (int)(myCount * 100d / files.Count);
                    System.Console.Write($"{ConsoleUtility.DeleteLine1}Remove: {removed,6} {percentage,3}%({myCount,8} items of total {files.Count,8}) processed");
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
            System.Console.Write(ConsoleUtility.DeleteLine1);
            return (count, removed);
        }

        var (count, removed) = await DeleteAsync(configSettings.OriginalFolder).ConfigureAwait(false);
        logger.LogInformation($"Original: {removed} of {count} files removed.");
        (count, removed) = await DeleteAsync(configSettings.ThumbnailFolder).ConfigureAwait(false);
        logger.LogInformation($"Thumbnail: {removed} of {count} files removed.");
        (count, removed) = await DeleteAsync(configSettings.UgoiraFolder).ConfigureAwait(false);
        logger.LogInformation($"Ugoira: {removed} of {count} files removed.");
    }
}
