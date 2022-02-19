using PixivApi.Core.Network;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("illusts")]
    public async ValueTask<int> DownloadIllustsOfUserAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option(null, ArgumentDescriptions.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return -1;
        }

        var overwriteKind = OverwriteKindExtensions.Parse(overwrite);
        var token = Context.CancellationToken;
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
        
        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = config.MaxParallel,
        };
        var add = 0UL;
        var update = 0UL;
        try
        {
            for (var line = System.Console.ReadLine(); !string.IsNullOrWhiteSpace(line); line = System.Console.ReadLine())
            {
                if (!ulong.TryParse(line.AsSpan().Trim(), out var id))
                {
                    continue;
                }

                var enumerator = new DownloadArtworkAsyncEnumerable(RetryGetAsync, $"https://{ApiHost}/v1/user/illusts?user_id={id}").GetAsyncEnumerator(token);
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
                }
            }
        }
        finally
        {
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
