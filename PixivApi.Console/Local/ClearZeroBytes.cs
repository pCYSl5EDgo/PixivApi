namespace PixivApi.Console;

partial class LocalClient
{
    [Command("clear-0-bytes", "")]
    public ValueTask ClearZeroBytes(
        bool pipe = false,
        bool force = false
    ) => ClearAsync(logger, configSettings, pipe, force, Context.CancellationToken);

    internal static async ValueTask ClearAsync(ILogger logger, ConfigSettings configSettings, bool pipe, bool forceClear, CancellationToken token)
    {
        if (!pipe)
        {
            logger.LogInformation("Start clearing.");
        }

        if (forceClear)
        {
            await ForceClearAsync(logger, configSettings, pipe, token).ConfigureAwait(false);
            return;
        }

        ConcurrentBag<FileInfo> files = new();
        ValueTask Collect(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var info = new FileInfo(path);
            if (info.Exists && info.Length == 0)
            {
                if (!pipe)
                {
                    logger.LogInformation(info.Name);
                }

                files.Add(info);
            }

            return ValueTask.CompletedTask;
        }

        var tasks = new Task[3];
        tasks[0] = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.OriginalFolder, "*", SearchOption.AllDirectories), token, Collect);
        tasks[1] = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.ThumbnailFolder, "*", SearchOption.AllDirectories), token, Collect);
        tasks[2] = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.UgoiraFolder, "*", SearchOption.AllDirectories), token, Collect);
        await Task.WhenAll(tasks).ConfigureAwait(false);

        if (!files.IsEmpty)
        {
            logger.LogWarning($"{IOUtility.WarningColor}Are you sure to delete {files.Count} files? Input 'yes'.{IOUtility.NormalizeColor}");
            var input = System.Console.ReadLine();
            if (input == "yes")
            {
                await Parallel.ForEachAsync(files, token, (file, token) =>
                {
                    file.Delete();
                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);

                logger.LogWarning($"{IOUtility.WarningColor}Done.{IOUtility.NormalizeColor}");
            }
        }
    }

    private static async ValueTask ForceClearAsync(ILogger logger, ConfigSettings configSettings, bool pipe, CancellationToken token)
    {
        ulong clear = 0UL;
        ValueTask Delete(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var info = new FileInfo(path);
            if (info.Exists && info.Length == 0)
            {
                if (!pipe)
                {
                    logger.LogInformation(info.Name);
                }

                Interlocked.Increment(ref clear);
                info.Delete();
            }

            return ValueTask.CompletedTask;
        }

        var tasks = new Task[3];
        tasks[0] = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.OriginalFolder, "*", SearchOption.AllDirectories), token, Delete);
        tasks[1] = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.ThumbnailFolder, "*", SearchOption.AllDirectories), token, Delete);
        tasks[2] = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.UgoiraFolder, "*", SearchOption.AllDirectories), token, Delete);
        await Task.WhenAll(tasks).ConfigureAwait(false);
        logger.LogInformation($"{IOUtility.WarningColor}{clear} files are deleted.{IOUtility.NormalizeColor}");
    }
}
