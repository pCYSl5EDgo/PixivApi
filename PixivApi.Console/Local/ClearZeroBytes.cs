namespace PixivApi;

partial class LocalClient
{
    [Command("clear-0-bytes", "")]
    public async ValueTask ClearZeroBytes(
        bool pipe = false
    )
    {
        System.Collections.Concurrent.ConcurrentBag<FileInfo> files = new();
        var token = Context.CancellationToken;

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

        var job0 = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.OriginalFolder), token, Collect);
        var job1 = Parallel.ForEachAsync(Directory.EnumerateFiles(configSettings.ThumbnailFolder), token, Collect);
        await job0.ConfigureAwait(false);
        await job1.ConfigureAwait(false);

        logger.LogWarning($"{IOUtility.WarningColor}Are you sure to delete {files.Count} files? Input 'yes'.{IOUtility.NormalizeColor}");
        var input = Console.ReadLine();
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
