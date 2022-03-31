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
        var database = await databaseFactory.RentAsync(token).ConfigureAwait(false);
        var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
        var transactional = database as ITransactionalDatabase;
        if (transactional is not null)
        {
            await transactional.BeginTransactionAsync(token).ConfigureAwait(false);
        }

        ulong add = 0UL, update = 0UL;
        try
        {
            await foreach (var artworkCollection in new SearchArtworkAsyncNewToOldEnumerable(url, requestSender.GetAsync, logger).WithCancellation(token))
            {
                token.ThrowIfCancellationRequested();
                var oldAdd = add;
                if (database is IExtenededDatabase exteneded)
                {
                    var (_add, _update) = await exteneded.ArtworkAddOrUpdateAsync(artworkCollection, token).ConfigureAwait(false);
                    add += _add;
                    update += _update;
                }
                else
                {
                    foreach (var item in artworkCollection)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

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
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (!addBehaviour && add == oldAdd)
                {
                    break;
                }
            }
        }
        catch (Exception e) when (e is not TaskCanceledException)
        {
            transactional?.RollbackTransaction();
            transactional = null;
            throw;
        }
        finally
        {
            if (!System.Console.IsOutputRedirected)
            {
                var artworkCount = await database.CountArtworkAsync(token).ConfigureAwait(false);
                logger.LogInformation($"Total: {artworkCount} Add: {add} Update: {update}");
            }

            transactional?.EndTransaction();
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
