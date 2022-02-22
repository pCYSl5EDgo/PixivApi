namespace PixivApi.Console;

public partial class LocalClient
{
    [Command("clear-0-bytes", "")]
    public ValueTask ClearZeroBytes(
        [Option("mask")] int maskPowerOf2 = 10
    ) => ClearAsync(logger, configSettings, maskPowerOf2, Context.CancellationToken);

    internal static async ValueTask ClearAsync(ILogger logger, ConfigSettings configSettings, int maskPowerOf2, CancellationToken token)
    {
        logger.LogInformation("Start clearing.");
        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };

        var mask = (1UL << maskPowerOf2) - 1UL;
        async ValueTask<(ulong, ulong)> DeleteAsync(string[] files)
        {
            System.Console.Write($"Remove: {0,6} {0,3}%({0,8} items of total {files.LongLength,8}) processed");
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
                    var percentage = (int)(myCount * 100d / files.LongLength);
                    System.Console.Write($"{ConsoleUtility.DeleteLine1}Remove: {removed,6} {percentage,3}%({myCount,8} items of total {files.LongLength,8}) processed");
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
            System.Console.Write(ConsoleUtility.DeleteLine1);
            return (count, removed);
        }

        var originalFiles = Directory.GetFiles(configSettings.OriginalFolder, "*", SearchOption.AllDirectories);
        var (count, removed) = await DeleteAsync(originalFiles).ConfigureAwait(false);
        logger.LogInformation($"Original: {removed} of {count} files removed.");
        var thumbnailFiles = Directory.GetFiles(configSettings.ThumbnailFolder, "*", SearchOption.AllDirectories);
        (count, removed) = await DeleteAsync(thumbnailFiles).ConfigureAwait(false);
        logger.LogInformation($"Thumbnail: {removed} of {count} files removed.");
        var ugoiraFiles = Directory.GetFiles(configSettings.UgoiraFolder, "*", SearchOption.AllDirectories);
        (count, removed) = await DeleteAsync(ugoiraFiles).ConfigureAwait(false);
        logger.LogInformation($"Ugoira: {removed} of {count} files removed.");
    }
}
