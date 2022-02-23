using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("ranking")]
    public async ValueTask<int> DownloadRankingAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option(1, ArgumentDescriptions.RankingDescription)] RankingKind ranking = RankingKind.day,
        DateOnly? date = null,
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
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };

        ulong add = 0UL, update = 0UL;
        try
        {
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(GetRankingUrl(ranking, date), RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                foreach (var item in artworkCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
                    }

                    var converted = Core.Local.Artwork.ConvertFromNetwrok(item, database.TagSet, database.ToolSet, database.UserDictionary);
                    dictionary.AddOrUpdate(
                        item.Id,
                        _ =>
                        {
                            ++add;
                            if (pipe)
                            {
                                logger.LogInformation($"{converted.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{add,4}: {converted.Id,20}");
                            }
                            return converted;
                        },
                        (_, v) =>
                        {
                            ++update;
                            v.Overwrite(converted);
                            return v;
                        }
                    );
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

    private static string GetRankingUrl(RankingKind ranking, DateOnly? date)
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
