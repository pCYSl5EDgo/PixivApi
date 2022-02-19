using PixivApi.Core.Network;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("ranking")]
    public async ValueTask<int> DownloadRankingAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option(1, "")] RankingKind ranking = RankingKind.day,
        [Option(2, "")] DateOnly? date = null,
        bool pipe = false
    )
    {
        if (!await Connect().ConfigureAwait(false))
        {
            return -3;
        }

        var token = Context.CancellationToken;
        var database = (await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false)) ?? new();
        var dictionary = new ConcurrentDictionary<ulong, Core.Local.Artwork>();
        foreach (var item in database.Artworks)
        {
            dictionary.TryAdd(item.Id, item);
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = config.MaxParallel,
        };

        var add = 0UL;
        var update = 0UL;
        var enumerator = new DownloadArtworkAsyncEnumerable(RetryGetAsync, GetUrl(ranking, date)).GetAsyncEnumerator(token);
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

        static string GetUrl(RankingKind ranking, DateOnly? date)
        {
            DefaultInterpolatedStringHandler url = $"https://{ApiHost}/v1/illust/ranking?mode={ranking}";
            if (date.HasValue)
            {
                url.AppendLiteral("&date=");
                var d = date.Value;
                url.AppendFormatted(d.Year);
                url.AppendLiteral("-");
                url.AppendFormatted(d.Month);
                url.AppendLiteral("-");
                url.AppendFormatted(d.Day);
            }

            return url.ToString();
        }
    }
}
