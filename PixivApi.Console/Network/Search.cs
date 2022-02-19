using PixivApi.Core.Network;
using System.Runtime.ExceptionServices;

namespace PixivApi.Console;

partial class NetworkClient
{
    [Command("search")]
    public async ValueTask<int> SearchAsync(
        [Option(0, "search text")] string text,
        [Option(1, ArgumentDescriptions.DatabaseDescription)] string output,
        [Option(null, ArgumentDescriptions.OverwriteKindDescription)] string overwrite = "add",
        bool pipe = false
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (!pipe)
            {
                logger.LogError("empty.");
            }

            return -1;
        }

        return await InternalSearchAsync(text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), output, OverwriteKindExtensions.Parse(overwrite), pipe);
    }

    private async ValueTask<int> InternalSearchAsync(string[] searchArray, string output, OverwriteKind overwriteKind, bool pipe)
    {
        if (CalcUrl(searchArray) is not string url)
        {
            if (!pipe)
            {
                logger.LogError("invalid url.");
            }

            return -1;
        }

        var token = Context.CancellationToken;
        var database = await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false) ?? new();
        var dictionary = new ConcurrentDictionary<ulong, Core.Local.Artwork>();
        foreach (var item in database.Artworks)
        {
            dictionary.TryAdd(item.Id, item);
        }

        if (!await Connect().ConfigureAwait(false))
        {
            return -1;
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = config.MaxParallel,
        };
        var add = 0UL;
        var update = 0UL;
        var enumerator = new SearchArtworkAsyncEnumerable(RetryGetAsync, url, SearchUrlUtility.CalculateNextUrl, array =>
        {
            var index = SearchUrlUtility.GetIndexOfOldestDay(array);
            var date = DateOnly.FromDateTime(array[index].CreateDate);
            return (date, index == 0 ? Array.Empty<Artwork>() : array[..index]);
        }, async (e, token) =>
        {
            logger.LogInformation(e, $"{ArgumentDescriptions.WarningColor}Wait for {config.RetryTimeSpan.TotalSeconds} seconds to reconnect.{ArgumentDescriptions.NormalizeColor}");
            await Task.Delay(config.RetryTimeSpan, token).ConfigureAwait(false);
            if (!await Reconnect().ConfigureAwait(false))
            {
                ExceptionDispatchInfo.Throw(e);
            }

            logger.LogInformation($"{ArgumentDescriptions.WarningColor}Reconnect.{ArgumentDescriptions.NormalizeColor}");
        }).GetAsyncEnumerator(token);
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

        static string CalcUrl(string[] array)
        {
            DefaultInterpolatedStringHandler handler = $"https://{ApiHost}/v1/search/illust?word=";
            handler.AppendFormatted(new PercentEncoding(array[0]));
            for (int i = 1; i < array.Length; i++)
            {
                handler.AppendLiteral("%20");
                handler.AppendFormatted(new PercentEncoding(array[i]));
            }

            handler.AppendLiteral("&search_target=");
            handler.AppendLiteral("partial_match_for_tags&sort=date_desc");
            return handler.ToStringAndClear();
        }
    }
}
