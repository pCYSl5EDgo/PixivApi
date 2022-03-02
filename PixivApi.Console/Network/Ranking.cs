using PixivApi.Core;
using PixivApi.Core.Network;

namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("ranking")]
    public async ValueTask DownloadRankingAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option(1, ArgumentDescriptions.RankingDescription)] Core.Local.RankingKind ranking = Core.Local.RankingKind.day,
        DateOnly? date = null,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        var database = (await IOUtility.MessagePackDeserializeAsync<Core.Local.DatabaseFile>(output, token).ConfigureAwait(false)) ?? new();
        var add = 0UL;
        var rankingList = new List<ulong>();
        try
        {
            var url = GetRankingUrl(date, ranking);
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                foreach (var item in artworkCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
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
