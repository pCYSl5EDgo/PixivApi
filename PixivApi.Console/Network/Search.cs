using PixivApi.Core;
using PixivApi.Core.Network;
using System.Runtime.ExceptionServices;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("search")]
    public async ValueTask<int> SearchAsync(
        [Option(0, ArgumentDescriptions.DatabaseDescription)] string output,
        [Option(1, "search text")] string text,
        [Option("e", "end_date")] string? end_date = null,
        [Option("o", ArgumentDescriptions.OverwriteKindDescription)] OverwriteKind overwrite = OverwriteKind.add,
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

        var searchArray = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (CalcUrl(searchArray, end_date) is not string url)
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
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };
        var add = 0UL;
        var update = 0UL;
        var enumerator = new SearchArtworkAsyncNewToOldEnumerable(RetryGetAsync, url, async (e, token) =>
        {
            logger.LogInformation(e, $"{ArgumentDescriptions.WarningColor}Wait for {configSettings.RetryTimeSpan.TotalSeconds} seconds to reconnect.{ArgumentDescriptions.NormalizeColor}");
            await Task.Delay(configSettings.RetryTimeSpan, token).ConfigureAwait(false);
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

                if (overwrite == OverwriteKind.add && add == oldAdd)
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

        static string CalcUrl(string[] array, string? end_date)
        {
            DefaultInterpolatedStringHandler handler = $"https://{ApiHost}/v1/search/illust?word=";
            handler.AppendFormatted(new PercentEncoding(array[0]));
            for (var i = 1; i < array.Length; i++)
            {
                handler.AppendLiteral("%20");
                handler.AppendFormatted(new PercentEncoding(array[i]));
            }

            handler.AppendLiteral("&search_target=partial_match_for_tags&sort=date_desc");
            if (!string.IsNullOrWhiteSpace(end_date))
            {
                handler.AppendLiteral("&end_date=");
                handler.AppendLiteral(end_date);
            }

            return handler.ToStringAndClear();
        }
    }
}
