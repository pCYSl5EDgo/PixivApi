namespace PixivApi;

partial class LocalClient
{
    [Command("merge", "")]
    public async ValueTask<int> MergeAsync(
        [Option(0, $"output *{IOUtility.ArtworkDatabaseFileExtension} file path")] string output,
        [Option("i", "search folder path")] string input = ".",
        [Option("f", $"input *{IOUtility.ArtworkDatabaseFileExtension} file path search pattern")] string filter = "*.arts"
    )
    {
        if (!Directory.Exists(input))
        {
            logger.LogError($"{IOUtility.ErrorColor}directory not found. Path: {input}{IOUtility.NormalizeColor}");
            return -1;
        }

        var token = Context.CancellationToken;
        var bag = new System.Collections.Concurrent.ConcurrentBag<HashSet<ArtworkDatabaseInfo>>();
        var enumerable = Directory.EnumerateFiles(input, filter);
        await Parallel.ForEachAsync(enumerable, token, async (input, token) =>
        {
            if (!File.Exists(input))
            {
                return;
            }

            using var segment = await IOUtility.ReadFromFileAsync(input, token).ConfigureAwait(false);
            var database = MessagePackSerializer.Deserialize<HashSet<ArtworkDatabaseInfo>>(segment.AsMemory(), null, token);
            bag.Add(database);
        }).ConfigureAwait(false);

        if (!bag.TryTake(out var set))
        {
            logger.LogError(IOUtility.ErrorColor + "inputs are empty." + IOUtility.NormalizeColor);
            return 0;
        }

        while (bag.TryTake(out var itr))
        {
            foreach (var item in itr)
            {
                if (item.User.Id == 0)
                {
                    continue;
                }

                if (!set.TryGetValue(item, out var actual))
                {
                    set.Add(item);
                    continue;
                }

                if (actual.TotalView >= item.TotalView)
                {
                    continue;
                }

                actual.Overwrite(item);
            }
        }

        if (!output.EndsWith(IOUtility.ArtworkDatabaseFileExtension))
        {
            output += IOUtility.ArtworkDatabaseFileExtension;
        }

        using var stream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.Read, 8192, true);
        await MessagePackSerializer.SerializeAsync(stream, set, null, token).ConfigureAwait(false);
        logger.LogInformation($"{IOUtility.SuccessColor}output success. Count: {set.Count}{IOUtility.NormalizeColor}");
        return 0;
    }
}
