namespace PixivApi.Console;

[Command("net")]
public sealed partial class NetworkClient : ConsoleAppBase, IDisposable
{
    private const string ApiHost = "app-api.pixiv.net";
    private readonly ConfigSettings configSettings;
    private readonly HttpClient client;
    private readonly AuthenticationHeaderValueHolder holder;
    private readonly IDatabaseFactory databaseFactory;
    private readonly IArtworkFilterFactory<FileInfo> filterFactory;

    public NetworkClient(ConfigSettings config, HttpClient client, AuthenticationHeaderValueHolder holder, IDatabaseFactory databaseFactory, IArtworkFilterFactory<FileInfo> filterFactory)
    {
        configSettings = config;
        this.client = client;
        this.holder = holder;
        this.databaseFactory = databaseFactory;
        this.filterFactory = filterFactory;
    }

    public void Dispose() => holder.Dispose();

    private async ValueTask DownloadArtworkResponses(bool addBehaviour, string url, CancellationToken token)
    {
        var logger = Context.Logger;
        var databaseTask = databaseFactory.RentAsync(token);
        // When addBehaviour is false, we should wait for the database initialization in order not to query unneccessarily.
        var database = addBehaviour ? null : await databaseTask.ConfigureAwait(false);
        var responseList = default(List<ArtworkResponseContent>);
        ulong add = 0UL, update = 0UL;
        async ValueTask<bool> RegisterNotShow(IDatabase database, ArtworkResponseContent item, CancellationToken token)
        {
            var isAdd = true;
            await database.AddOrUpdateAsync(
                item.Id,
                async token => await LocalNetworkConverter.ConvertAsync(item, database, database, database, token).ConfigureAwait(false),
                async (artwork, token) =>
                {
                    isAdd = false;
                    await LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token).ConfigureAwait(false);
                },
                token
            ).ConfigureAwait(false);
            return isAdd;
        }

        async ValueTask RegisterShow(IDatabase database, ArtworkResponseContent item, CancellationToken token)
        {
            await database.AddOrUpdateAsync(
                item.Id,
                async token =>
                {
                    ++add;
                    if (System.Console.IsOutputRedirected)
                    {
                        logger.LogInformation($"{item.Id}");
                    }
                    else
                    {
                        logger.LogInformation($"{add,4}: {item.Id,20}");
                    }

                    return await LocalNetworkConverter.ConvertAsync(item, database, database, database, token).ConfigureAwait(false);
                },
                async (artwork, token) =>
                {
                    ++update;
                    await LocalNetworkConverter.OverwriteAsync(artwork, item, database, database, database, token);
                },
                token
            ).ConfigureAwait(false);
        }

        try
        {
            var requestSender = Context.ServiceProvider.GetRequiredService<RequestSender>();
            await foreach (var artworkCollection in new DownloadArtworkAsyncEnumerable(url, requestSender.GetAsync).WithCancellation(token))
            {
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
                            (await RegisterNotShow(database, item, token).ConfigureAwait(false) ? ref add : ref update)++;
                        }

                        responseList = null;
                    }
                }

                var oldAdd = add;
                foreach (var item in artworkCollection)
                {
                    await RegisterShow(database, item, token).ConfigureAwait(false);
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
                        (await RegisterNotShow(database, item, token).ConfigureAwait(false) ? ref add : ref update)++;
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
}
