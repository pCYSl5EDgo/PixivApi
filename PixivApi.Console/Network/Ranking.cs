using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("ranking")]
    public async ValueTask<int> DownloadRankingAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option(1, ArgumentDescriptions.RankingDescription)] Core.Local.RankingKind ranking = Core.Local.RankingKind.day,
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
        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = configSettings.MaxParallel,
        };

        var add = 0UL;
        var rankingList = new List<ulong>();
        try
        {
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(GetRankingUrl(date, ranking), RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                foreach (var item in artworkCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
                    }

                    var converted = Core.Local.Artwork.ConvertFromNetwrok(item, database.TagSet, database.ToolSet, database.UserDictionary);
                    rankingList.Add(converted.Id);
                    database.ArtworkDictionary.AddOrUpdate(
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
                            v.Overwrite(converted);
                            return v;
                        }
                    );
                }
            }
        }
        finally
        {
            if (rankingList.Count != 0)
            {
                var rankingArray = rankingList.ToArray();
                database.RankingSet.AddOrUpdate(new(date ?? DateOnly.FromDateTime(DateTime.Now), ranking), rankingArray, (_, _) => rankingArray);
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {database.ArtworkDictionary.Count} Add: {add} Update: {(ulong)rankingList.Count - add}");
            }
        }

        return 0;
    }

    private static string GetRankingUrl(DateOnly? date, Core.Local.RankingKind ranking)
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
