namespace PixivApi.Console;

public partial class NetworkClient
{
    [Command("search")]
    public async ValueTask SearchAsync(
        [Option(0, "search text")] string text,
        [Option("e")] string? end_date = null,
        [Option("o")] ushort offset = 0,
        [Option("a", ArgumentDescriptions.AddKindDescription)] bool addBehaviour = false
    )
    {
        var logger = Context.Logger;
        if (string.IsNullOrWhiteSpace(configSettings.DatabaseFilePath))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            if (!System.Console.IsOutputRedirected)
            {
                logger.LogError("empty.");
            }

            return;
        }

        var searchArray = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (CalcSearchUrl(searchArray, end_date, offset) is not string url)
        {
            if (!System.Console.IsOutputRedirected)
            {
                logger.LogError("invalid url.");
            }

            return;
        }

        var token = Context.CancellationToken;
        var databaseTask = databaseFactory.RentAsync(token);
        var database = default(IDatabase);
        var responseList = default(List<ArtworkResponseContent>);
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        ulong add = 0UL, update = 0UL;
        try
        {
            await foreach (var artworkCollection in new SearchArtworkAsyncNewToOldEnumerable(url, requestSender.GetAsync).WithCancellation(token))
            {
                token.ThrowIfCancellationRequested();
                if (database is null)
                {
                    if (!databaseTask.IsCompleted)
                    {
                        responseList ??= new List<ArtworkResponseContent>(5010);
                        foreach (var item in artworkCollection)
                        {
                            responseList.Add(item);
                            if (System.Console.IsOutputRedirected)
                            {
                                logger.LogInformation($"{item.Id}");
                            }
                            else
                            {
                                logger.LogInformation($"{responseList.Count,4}: {item.Id}");
                            }
                        }

                        token.ThrowIfCancellationRequested();
                        continue;
                    }

                    database = await databaseTask.ConfigureAwait(false);
                    if (responseList is { Count: > 0 })
                    {
                        foreach (var item in responseList)
                        {
                            if (await database.AddOrUpdateAsync(
                                item.Id,
                                token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                                (artwork, token) => LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token),
                                token
                            ).ConfigureAwait(false))
                            {
                                ++add;
                            }
                            else
                            {
                                ++update;
                            }
                        }

                        responseList = null;
                    }
                }

                var oldAdd = add;
                foreach (var item in artworkCollection)
                {
                    if (await database.AddOrUpdateAsync(
                        item.Id,
                        token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                        (artwork, token) => LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token),
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
                    else
                    {
                        ++update;
                    }
                }

                token.ThrowIfCancellationRequested();
                if (!addBehaviour && add == oldAdd)
                {
                    break;
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Accept cancel. Please wait for writing to the database file.");
        }
        finally
        {
            if (database is null)
            {
                database = await databaseTask.ConfigureAwait(false);
                if (responseList is { Count: > 0 })
                {
                    foreach (var item in responseList)
                    {
                        if (await database.AddOrUpdateAsync(
                            item.Id,
                            token => LocalNetworkConverter.ConvertAsync(item, database, database, database, token),
                            (artwork, token) => LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token),
                            token
                        ).ConfigureAwait(false))
                        {
                            ++add;
                        }
                        else
                        {
                            ++update;
                        }
                    }

                    responseList = null;
                }
            }

            if (!System.Console.IsOutputRedirected)
            {
                var artworkCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
                logger.LogInformation($"Total: {artworkCount} Add: {add} Update: {update}");
            }

            databaseFactory.Return(ref database);
        }
    }

    private static string CalcSearchUrl(string[] array, string? end_date, ushort offset)
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

        if (offset != 0)
        {
            handler.AppendLiteral("&offset=");
            handler.AppendFormatted(offset);
        }

        return handler.ToStringAndClear();
    }
}
