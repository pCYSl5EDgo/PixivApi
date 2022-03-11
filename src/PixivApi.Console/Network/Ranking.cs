namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("ranking")]
    public async ValueTask DownloadRankingAsync
    (
        [Option(0, ArgumentDescriptions.RankingDescription)] RankingKind ranking = RankingKind.day,
        DateOnly? date = null
    )
    {
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        var token = Context.CancellationToken;
        System.Console.Error.WriteLine($"Start loading database. Time: {DateTime.Now}");
        var databaseTask = databaseFactory.RentAsync(token);
        var add = 0UL;
        var rankingList = new List<ArtworkResponseContent>(300);
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        var url = GetRankingUrl(date, ranking);
        try
        {
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync).WithCancellation(token))
            {
                foreach (var item in artworkCollection)
                {
                    rankingList.Add(item);
                }
            }
        }
        catch (TaskCanceledException)
        {
            Context.Logger.LogError("Accept cancel. Please wait for writing to the database file.");
        }
        finally
        {
            var database = await databaseTask.ConfigureAwait(false);
            if (rankingList.Count != 0)
            {
                var rankingArray = new ulong[rankingList.Count];
                for (var i = 0; i < rankingArray.Length; i++)
                {
                    var item = rankingList[i];
                    if (await database.AddOrUpdateAsync(
                        item.Id,
                        token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                        (v, token) => LocalNetworkConverter.OverwriteAsync(v, item, database, database, database, token),
                        token
                    ).ConfigureAwait(false))
                    {
                        ++add;
                        if (System.Console.IsOutputRedirected)
                        {
                            Context.Logger.LogInformation($"{item.Id}");
                        }
                        else
                        {
                            Context.Logger.LogInformation($"{add,4}: {item.Id,20}");
                        }
                    }

                    rankingArray[i] = item.Id;
                }

                await database.AddOrUpdateRankingAsync(date ?? DateOnly.FromDateTime(DateTime.Now), ranking, rankingArray, token).ConfigureAwait(false);

                if (!System.Console.IsOutputRedirected)
                {
                    var databaseCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
                    Context.Logger.LogInformation($"Total: {databaseCount} Add: {add} Update: {(ulong)rankingList.Count - add} Time: {DateTime.Now}");
                }
            }

            databaseFactory.Return(ref database);
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
