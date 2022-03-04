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
        System.Console.Error.WriteLine($"Start loading database. Time: {DateTime.Now}");
        var databaseTask = IOUtility.MessagePackDeserializeAsync<DatabaseFile>(output, token);
        var authentication = await ConnectAsync(token).ConfigureAwait(false);
        var add = 0UL;
        var rankingList = new List<Core.Network.ArtworkResponseContent>(300);
        var url = GetRankingUrl(date, ranking);
        try
        {
            await foreach (var artworkCollection in new Core.Network.DownloadArtworkAsyncEnumerable(url, authentication, RetryGetAsync, ReconnectAsync, pipe).WithCancellation(token))
            {
                foreach (var item in artworkCollection)
                {
                    rankingList.Add(item);
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Accept cancel. Please wait for writing to the database file.");
        }
        finally
        {
            var databaseCount = 0;
            if (rankingList.Count != 0)
            {
                var database = (await databaseTask.ConfigureAwait(false)) ?? new();
                var rankingArray = new ulong[rankingList.Count];
                for (var i = 0; i < rankingArray.Length; i++)
                {
                    var item = rankingList[i];
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

                    rankingArray[i] = item.Id;
                }

                database.RankingSet.AddOrUpdate(new(date ?? DateOnly.FromDateTime(DateTime.Now), ranking), rankingArray, (_, _) => rankingArray);
                await IOUtility.MessagePackSerializeAsync(output, database, FileMode.Create).ConfigureAwait(false);
                databaseCount = database.ArtworkDictionary.Count;
            }

            if (!pipe)
            {
                logger.LogInformation($"Total: {databaseCount} Add: {add} Update: {(ulong)rankingList.Count - add} Time: {DateTime.Now}");
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
