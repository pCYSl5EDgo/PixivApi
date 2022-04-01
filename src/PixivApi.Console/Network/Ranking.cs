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
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        var transactional = database as ITransactionalDatabase;
        if (transactional is not null)
        {
            await transactional.BeginTransactionAsync(token).ConfigureAwait(false);
        }

        var add = 0UL;
        var rankingList = new List<ArtworkResponseContent>(300);
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        var url = GetRankingUrl(date, ranking);
        try
        {
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync, Context.Logger).WithCancellation(token))
            {
                foreach (var item in artworkCollection)
                {
                    rankingList.Add(item);
                }
            }

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
            }
        }
        catch (Exception e) when (transactional is not null && e is not TaskCanceledException && e is not OperationCanceledException)
        {
            transactional.RollbackTransaction();
            transactional = null;
            throw;
        }
        finally
        {
            if (!System.Console.IsOutputRedirected)
            {
                var databaseCount = await database.CountArtworkAsync(CancellationToken.None).ConfigureAwait(false);
                Context.Logger.LogInformation($"Total: {databaseCount} Add: {add} Update: {(ulong)rankingList.Count - add} Time: {DateTime.Now}");
            }

            transactional?.EndTransaction();
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
