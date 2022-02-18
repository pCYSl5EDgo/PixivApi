using PixivApi.Core.Network;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("follows-new-work")]
    public async ValueTask<int> DownloadNewIllustsOfFollowersAsync
    (
        [Option(0, $"output {IOUtility.DatabaseDescription}")] string output,
        [Option(null, IOUtility.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return -1;
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var overwriteKind = OverwriteKindExtensions.Parse(overwrite);
        var database = overwriteKind == OverwriteKind.ClearAndAdd ?
            null :
            await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false);
        if (database is null)
        {
            overwriteKind = OverwriteKind.ClearAndAdd;
            database = new();
        }

        var dictionary = new ConcurrentDictionary<ulong, Core.Local.Artwork>();
        if (overwriteKind == OverwriteKind.SearchAndAdd || overwriteKind == OverwriteKind.Add)
        {
            foreach (var item in database.Artworks)
            {
                dictionary.TryAdd(item.Id, item);
            }
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = config.MaxParallel,
        };
        var add = 0UL;
        var update = 0UL;
        var enumerator = new DownloadArtworkAsyncEnumerable(RetryGetAsync, $"https://{ApiHost}/v2/illust/follow?restrict=public").GetAsyncEnumerator(token);
        try
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var c = enumerator.Current;
                var oldAdd = add;
                var oldUpdate = update;
                await Parallel.ForEachAsync(c, parallelOptions, (item, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var converted = Core.Local.Artwork.ConvertFromNetwrok(item, database.TagSet, database.ToolSet, database.UserDictionary);
                    dictionary.AddOrUpdate(
                        item.Id,
                        _ =>
                        {
                            var added = Interlocked.Increment(ref add);
                            if (pipe)
                            {
                                logger.LogInformation($"{converted.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{added,4}: {converted.Id,20}");
                            }
                            return converted;
                        },
                        (_, v) =>
                        {
                            Interlocked.Increment(ref update);
                            v.Overwrite(converted);
                            return v;
                        }
                    );

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);

                if (overwriteKind == OverwriteKind.Add && add == oldAdd)
                {
                    break;
                }
            }
        }
        finally
        {
            if (enumerator is not null)
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }

            if (add != 0 || update != 0)
            {
                database.Artworks = dictionary.Values.ToArray();
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.Artworks.Length} Add: {add} Update: {update}");
            }
        }

        return 0;
    }
}
