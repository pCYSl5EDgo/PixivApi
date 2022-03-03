namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("ranking")]
    public async ValueTask DownloadRankingAsync
    (
        [Option(0, $"output {ArgumentDescriptions.DatabaseDescription}")] string output,
        [Option(1, ArgumentDescriptions.RankingDescription)] RankingKind ranking = RankingKind.day,
        DateOnly? date = null,
        bool pipe = false
    )
    {
        var token = Context.CancellationToken;
        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        var database = (await IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token).ConfigureAwait(false)) ?? new();
        var add = 0UL;
        var rankingList = new List<ulong>();
        try
        {
            var url = GetRankingUrl(date, ranking);
            await foreach (var artworkCollection in new Core.Network.DownloadArtworkAsyncEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                foreach (var item in artworkCollection)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    rankingList.Add(item.Id);
                    _ = database.ArtworkDictionary.AddOrUpdate(
                        item.Id,
                        _ =>
                        {
                            ++add;
                            if (pipe)
                            {
                                logger.LogInformation($"{item.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{add,4}: {item.Id,20}");
                            }

                            return LocalNetworkConverter.Convert(item, database.TagSet, database.ToolSet, database.UserDictionary);
                        },
                        (_, v) =>
                        {
                            LocalNetworkConverter.Overwrite(v, item, database.TagSet, database.ToolSet, database.UserDictionary);
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

    private static string GetRankingUrl(DateOnly? date, RankingKind ranking)
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
